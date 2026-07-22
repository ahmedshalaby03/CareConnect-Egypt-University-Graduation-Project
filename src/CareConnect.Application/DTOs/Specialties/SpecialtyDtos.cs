using CareConnect.Application.Common.Models;

namespace CareConnect.Application.DTOs.Specialties;

/// <summary>Compact shape used by public dropdowns: id, English name, Arabic name.</summary>
public class SpecialtyOptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ArabicName { get; init; }
}

/// <summary>Full shape for the SuperAdmin management screen.</summary>
public class SpecialtyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ArabicName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>How many doctors point at this specialty, so an admin sees the impact of deactivating it.</summary>
    public int DoctorCount { get; init; }

    /// <summary>How many hospitals list this specialty.</summary>
    public int HospitalCount { get; init; }
}

public class CreateSpecialtyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ArabicName { get; set; }
    public string? Description { get; set; }
}

public class UpdateSpecialtyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ArabicName { get; set; }
    public string? Description { get; set; }
}

public class SpecialtyQueryParameters : PagedQueryParameters
{
    /// <summary>Matches the English or the Arabic name.</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}

public class ToggleSpecialtyStatusResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
