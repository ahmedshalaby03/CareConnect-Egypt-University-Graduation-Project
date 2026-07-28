using System.Security.Cryptography;
using CareConnect.Application.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Accounts;
using CareConnect.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Local managed storage for development and single-server deployments. Untrusted input is
/// decoded and re-encoded as WebP; the original bytes and client file name are never saved.
/// </summary>
public sealed class LocalProfileImageStorageService : IProfileImageStorageService
{
    private const long MaximumDecodedPixels = 40_000_000;
    private const int MaximumSourceDimension = 12_000;
    private const string OutputContentType = "image/webp";

    private static readonly IReadOnlyDictionary<string, string> ContentTypeByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    private readonly string _managedDirectory;
    private readonly ILogger<LocalProfileImageStorageService> _logger;

    public LocalProfileImageStorageService(
        IHostEnvironment environment,
        ILogger<LocalProfileImageStorageService> logger)
    {
        _managedDirectory = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, ProfileImageStorageConstants.RelativeDirectory));
        _logger = logger;

        Directory.CreateDirectory(_managedDirectory);
    }

    public async Task<Result<StoredProfileImage>> SaveAsync(
        ProfileImageUpload upload,
        CancellationToken ct = default)
    {
        var basicValidation = ValidateUploadMetadata(upload);
        if (basicValidation is not null)
        {
            return Result<StoredProfileImage>.Invalid("Profile image upload failed.", [basicValidation]);
        }

        await using var input = new MemoryStream(capacity: checked((int)upload.Length));
        try
        {
            await CopyWithLimitAsync(upload.Content, input, ct);
        }
        catch (InvalidDataException)
        {
            return Result<StoredProfileImage>.Invalid(
                "Profile image upload failed.",
                ["The image must not exceed 5 MB."]);
        }

        input.Position = 0;

        ImageInfo? imageInfo;
        try
        {
            imageInfo = await Image.IdentifyAsync(input, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return InvalidImageResult();
        }

        if (imageInfo is null
            || imageInfo.Width <= 0
            || imageInfo.Height <= 0
            || imageInfo.Width > MaximumSourceDimension
            || imageInfo.Height > MaximumSourceDimension
            || (long)imageInfo.Width * imageInfo.Height > MaximumDecodedPixels)
        {
            return Result<StoredProfileImage>.Invalid(
                "Profile image upload failed.",
                ["The image dimensions are invalid or too large."]);
        }

        var extension = Path.GetExtension(upload.ClientFileName);
        var detectedFormat = imageInfo.Metadata.DecodedImageFormat;
        if (!MatchesAllowedFormat(extension, upload.ContentType, detectedFormat))
        {
            return InvalidImageResult();
        }

        input.Position = 0;
        using var image = await LoadImageSafelyAsync(input, ct);
        if (image is null)
        {
            return InvalidImageResult();
        }

        image.Mutate(context => context.AutoOrient());
        if (image.Width > ProfileImageStorageConstants.MaximumOutputDimension
            || image.Height > ProfileImageStorageConstants.MaximumOutputDimension)
        {
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(
                    ProfileImageStorageConstants.MaximumOutputDimension,
                    ProfileImageStorageConstants.MaximumOutputDimension)
            }));
        }

        // Re-encoding strips the untrusted container. Explicitly clear metadata profiles so
        // EXIF location, comments and colour/profile payloads cannot survive normalization.
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        // GetHexString accepts the resulting string length, not a byte count.
        // Sixty-four hexadecimal characters provide a 256-bit random managed name
        // and match the strict validation performed by IsManagedFileName.
        var fileName = $"{RandomNumberGenerator.GetHexString(64).ToLowerInvariant()}.webp";
        var destinationPath = ManagedPath(fileName);

        try
        {
            await using var output = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true);

            await image.SaveAsWebpAsync(
                output,
                new WebpEncoder { Quality = 82 },
                ct);
        }
        catch (OperationCanceledException)
        {
            TryDeletePhysicalFile(destinationPath);
            throw;
        }
        catch (Exception exception)
        {
            TryDeletePhysicalFile(destinationPath);
            _logger.LogWarning(
                "Profile image normalization failed during managed storage ({ErrorType}).",
                exception.GetType().Name);
            return Result<StoredProfileImage>.ServiceUnavailable(
                "Profile image storage is temporarily unavailable. Please try again.");
        }

        var size = new FileInfo(destinationPath).Length;
        _logger.LogInformation(
            "Normalized a profile image to {Format} ({SizeBytes} bytes).",
            OutputContentType,
            size);

        return Result<StoredProfileImage>.Success(
            new StoredProfileImage(
                fileName,
                GetPublicUrl(fileName)!,
                size,
                OutputContentType),
            "Profile image validated and stored successfully.");
    }

    public Task DeleteAsync(string? managedFileName, CancellationToken ct = default)
    {
        if (!TryGetManagedPath(managedFileName, out var path))
        {
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "A managed profile image could not be removed during cleanup ({ErrorType}).",
                exception.GetType().Name);
        }

        return Task.CompletedTask;
    }

    public string? GetPublicUrl(string? managedFileName)
    {
        if (!IsManagedFileName(managedFileName))
        {
            return null;
        }

        return $"{ProfileImageStorageConstants.RequestPath}/{Uri.EscapeDataString(managedFileName!)}";
    }

    private static string? ValidateUploadMetadata(ProfileImageUpload upload)
    {
        if (upload.Content is null || upload.Length <= 0)
        {
            return "Select a non-empty image file.";
        }

        if (upload.Length > ProfileImageStorageConstants.MaximumUploadBytes)
        {
            return "The image must not exceed 5 MB.";
        }

        var clientName = Path.GetFileName(upload.ClientFileName);
        if (string.IsNullOrWhiteSpace(clientName)
            || !string.Equals(clientName, upload.ClientFileName, StringComparison.Ordinal)
            || clientName.Count(character => character == '.') != 1)
        {
            return "The supplied image file name is not valid.";
        }

        var extension = Path.GetExtension(clientName);
        if (!ContentTypeByExtension.TryGetValue(extension, out var expectedContentType)
            || !string.Equals(upload.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return "Only JPEG, PNG and WebP images are accepted, and the file type must match its extension.";
        }

        return null;
    }

    private static bool MatchesAllowedFormat(
        string extension,
        string contentType,
        IImageFormat? detectedFormat)
    {
        if (detectedFormat is null
            || !ContentTypeByExtension.TryGetValue(extension, out var expectedContentType)
            || !string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var detected = detectedFormat.Name.ToUpperInvariant();
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => detected is "JPEG" or "JPG",
            ".png" => detected == "PNG",
            ".webp" => detected == "WEBP",
            _ => false
        };
    }

    private static async Task<Image<Rgba32>?> LoadImageSafelyAsync(
        Stream input,
        CancellationToken ct)
    {
        try
        {
            return await Image.LoadAsync<Rgba32>(input, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static Result<StoredProfileImage> InvalidImageResult() =>
        Result<StoredProfileImage>.Invalid(
            "Profile image upload failed.",
            ["The file is not a valid JPEG, PNG or WebP image, or its contents do not match its extension."]);

    private async Task CopyWithLimitAsync(
        Stream source,
        Stream destination,
        CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        long total = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, ct);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > ProfileImageStorageConstants.MaximumUploadBytes)
            {
                throw new InvalidDataException("Profile image exceeds the size limit.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }

    private string ManagedPath(string managedFileName)
    {
        if (!IsManagedFileName(managedFileName))
        {
            throw new InvalidOperationException("The generated profile image name is invalid.");
        }

        return Path.Combine(_managedDirectory, managedFileName);
    }

    private bool TryGetManagedPath(string? managedFileName, out string path)
    {
        path = string.Empty;
        if (!IsManagedFileName(managedFileName))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(_managedDirectory, managedFileName!));
        var prefix = _managedDirectory.TrimEnd(Path.DirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    private static bool IsManagedFileName(string? managedFileName) =>
        !string.IsNullOrWhiteSpace(managedFileName)
        && managedFileName.Length == 69
        && managedFileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
        && managedFileName.AsSpan(0, 64).ToString().All(Uri.IsHexDigit)
        && string.Equals(Path.GetFileName(managedFileName), managedFileName, StringComparison.Ordinal);

    private static void TryDeletePhysicalFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // The caller's primary exception is more useful; cleanup is best-effort here.
        }
    }
}
