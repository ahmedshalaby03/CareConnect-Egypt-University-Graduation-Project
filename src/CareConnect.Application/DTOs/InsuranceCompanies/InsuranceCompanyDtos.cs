using CareConnect.Application.Common.Models;

namespace CareConnect.Application.DTOs.InsuranceCompanies;

/// <summary>Compact shape used by the patient's request form dropdown.</summary>
public class InsuranceCompanyOptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ArabicName { get; init; }
    public string? LogoUrl { get; init; }
}

/// <summary>Full shape for the SuperAdmin management screen.</summary>
public class InsuranceCompanyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ArabicName { get; init; }
    public string? Description { get; init; }
    public string? PhoneNumber { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? LogoUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>How many insurance requests reference this company, so an admin sees the impact of deactivating it.</summary>
    public int RequestCount { get; init; }
}

public class CreateInsuranceCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ArabicName { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
}

public class UpdateInsuranceCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ArabicName { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
}

public class InsuranceCompanyQueryParameters : PagedQueryParameters
{
    /// <summary>Matches the English or the Arabic name.</summary>
    public string? SearchTerm { get; set; }

    public bool? IsActive { get; set; }
}

public class ToggleInsuranceCompanyStatusResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
