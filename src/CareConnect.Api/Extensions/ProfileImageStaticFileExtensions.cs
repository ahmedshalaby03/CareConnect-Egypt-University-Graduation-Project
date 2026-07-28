using CareConnect.Application.Common;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace CareConnect.Api.Extensions;

public static class ProfileImageStaticFileExtensions
{
    public static WebApplication UseManagedProfileImages(this WebApplication app)
    {
        var directory = Path.GetFullPath(Path.Combine(
            app.Environment.ContentRootPath,
            ProfileImageStorageConstants.RelativeDirectory));
        Directory.CreateDirectory(directory);
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".webp"] = "image/webp";

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(directory),
            RequestPath = ProfileImageStorageConstants.RequestPath,
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = context =>
            {
                // Generated immutable names change on replacement, so one-year browser
                // caching cannot leave a user stuck with an old avatar.
                context.Context.Response.Headers.CacheControl =
                    "public,max-age=31536000,immutable";
                context.Context.Response.Headers.XContentTypeOptions = "nosniff";
                context.Context.Response.Headers.ContentDisposition = "inline";
            }
        });

        return app;
    }
}
