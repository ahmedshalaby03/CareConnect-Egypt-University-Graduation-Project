using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceCompanies;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public class InsuranceCompanyService : IInsuranceCompanyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InsuranceCompanyService> _logger;

    public InsuranceCompanyService(ApplicationDbContext context, ILogger<InsuranceCompanyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<InsuranceCompanyOptionDto>>> GetActiveAsync(
        CancellationToken ct = default)
    {
        var items = await _context.InsuranceCompanies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new InsuranceCompanyOptionDto
            {
                Id = c.Id,
                Name = c.Name,
                ArabicName = c.ArabicName,
                LogoUrl = c.LogoUrl
            })
            .ToListAsync(ct);

        return Result<IReadOnlyList<InsuranceCompanyOptionDto>>.Success(
            items, "Insurance companies retrieved successfully.");
    }

    public async Task<Result<PagedResult<InsuranceCompanyDto>>> GetAllAsync(
        InsuranceCompanyQueryParameters query,
        CancellationToken ct = default)
    {
        var companies = _context.InsuranceCompanies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            companies = companies.Where(c =>
                EF.Functions.Like(c.Name, $"%{term}%") ||
                (c.ArabicName != null && EF.Functions.Like(c.ArabicName, $"%{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            companies = companies.Where(c => c.IsActive == query.IsActive.Value);
        }

        var totalCount = await companies.CountAsync(ct);

        var items = await companies
            .OrderBy(c => c.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(c => new InsuranceCompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                ArabicName = c.ArabicName,
                Description = c.Description,
                PhoneNumber = c.PhoneNumber,
                WebsiteUrl = c.WebsiteUrl,
                LogoUrl = c.LogoUrl,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                RequestCount = c.InsuranceRequests.Count
            })
            .ToListAsync(ct);

        return Result<PagedResult<InsuranceCompanyDto>>.Success(
            PagedResult<InsuranceCompanyDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Insurance companies retrieved successfully.");
    }

    public async Task<Result<InsuranceCompanyDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _context.InsuranceCompanies
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new InsuranceCompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                ArabicName = c.ArabicName,
                Description = c.Description,
                PhoneNumber = c.PhoneNumber,
                WebsiteUrl = c.WebsiteUrl,
                LogoUrl = c.LogoUrl,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                RequestCount = c.InsuranceRequests.Count
            })
            .FirstOrDefaultAsync(ct);

        return company is null
            ? Result<InsuranceCompanyDto>.NotFound("Insurance company not found.")
            : Result<InsuranceCompanyDto>.Success(company, "Insurance company retrieved successfully.");
    }

    public async Task<Result<InsuranceCompanyDto>> CreateAsync(
        CreateInsuranceCompanyRequest request,
        CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var arabicName = Normalise(request.ArabicName);

        if (await NameExistsAsync(name, null, ct))
        {
            return Result<InsuranceCompanyDto>.Conflict($"An insurance company named '{name}' already exists.");
        }

        if (arabicName is not null && await ArabicNameExistsAsync(arabicName, null, ct))
        {
            return Result<InsuranceCompanyDto>.Conflict(
                $"An insurance company with the Arabic name '{arabicName}' already exists.");
        }

        var company = new InsuranceCompany
        {
            Name = name,
            ArabicName = arabicName,
            Description = Normalise(request.Description),
            PhoneNumber = Normalise(request.PhoneNumber),
            WebsiteUrl = Normalise(request.WebsiteUrl),
            LogoUrl = Normalise(request.LogoUrl),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.InsuranceCompanies.Add(company);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created insurance company {CompanyId} ({Name}).", company.Id, company.Name);

        return Result<InsuranceCompanyDto>.Success(ToDto(company, 0), "Insurance company created successfully.");
    }

    public async Task<Result<InsuranceCompanyDto>> UpdateAsync(
        Guid id,
        UpdateInsuranceCompanyRequest request,
        CancellationToken ct = default)
    {
        var company = await _context.InsuranceCompanies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null)
        {
            return Result<InsuranceCompanyDto>.NotFound("Insurance company not found.");
        }

        var name = request.Name.Trim();
        var arabicName = Normalise(request.ArabicName);

        // Excluding this row from the duplicate check, so saving without changing the name works.
        if (await NameExistsAsync(name, id, ct))
        {
            return Result<InsuranceCompanyDto>.Conflict($"An insurance company named '{name}' already exists.");
        }

        if (arabicName is not null && await ArabicNameExistsAsync(arabicName, id, ct))
        {
            return Result<InsuranceCompanyDto>.Conflict(
                $"An insurance company with the Arabic name '{arabicName}' already exists.");
        }

        company.Name = name;
        company.ArabicName = arabicName;
        company.Description = Normalise(request.Description);
        company.PhoneNumber = Normalise(request.PhoneNumber);
        company.WebsiteUrl = Normalise(request.WebsiteUrl);
        company.LogoUrl = Normalise(request.LogoUrl);
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var requestCount = await _context.InsuranceRequests.CountAsync(r => r.InsuranceCompanyId == id, ct);

        _logger.LogInformation("Updated insurance company {CompanyId}.", id);

        return Result<InsuranceCompanyDto>.Success(
            ToDto(company, requestCount), "Insurance company updated successfully.");
    }

    public async Task<Result<ToggleInsuranceCompanyStatusResponse>> ToggleStatusAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var company = await _context.InsuranceCompanies.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (company is null)
        {
            return Result<ToggleInsuranceCompanyStatusResponse>.NotFound("Insurance company not found.");
        }

        // Deactivating is the only "delete". Existing insurance requests keep referencing
        // it; the company simply stops appearing in the patient's request form.
        company.IsActive = !company.IsActive;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Set IsActive={IsActive} on insurance company {CompanyId}.", company.IsActive, id);

        return Result<ToggleInsuranceCompanyStatusResponse>.Success(
            new ToggleInsuranceCompanyStatusResponse
            {
                Id = company.Id,
                Name = company.Name,
                IsActive = company.IsActive
            },
            company.IsActive
                ? "Insurance company activated successfully."
                : "Insurance company deactivated successfully. Existing requests keep their reference.");
    }

    // ----------------------------------------------------------------- Helpers

    private Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken ct) =>
        _context.InsuranceCompanies.AnyAsync(
            c => c.Name.ToLower() == name.ToLower() && (excludingId == null || c.Id != excludingId),
            ct);

    private Task<bool> ArabicNameExistsAsync(string arabicName, Guid? excludingId, CancellationToken ct) =>
        _context.InsuranceCompanies.AnyAsync(
            c => c.ArabicName == arabicName && (excludingId == null || c.Id != excludingId),
            ct);

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static InsuranceCompanyDto ToDto(InsuranceCompany company, int requestCount) => new()
    {
        Id = company.Id,
        Name = company.Name,
        ArabicName = company.ArabicName,
        Description = company.Description,
        PhoneNumber = company.PhoneNumber,
        WebsiteUrl = company.WebsiteUrl,
        LogoUrl = company.LogoUrl,
        IsActive = company.IsActive,
        CreatedAt = company.CreatedAt,
        UpdatedAt = company.UpdatedAt,
        RequestCount = requestCount
    };
}
