using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.BloodStock;

// -------------------------------------------------------------------- Requests

public class CreateBloodStockRequest
{
    public BloodGroup BloodGroup { get; set; }
    public int AvailableUnits { get; set; }
    public int MinimumRequiredUnits { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBloodStockRequest
{
    public int AvailableUnits { get; set; }
    public int MinimumRequiredUnits { get; set; }
    public string? Notes { get; set; }
}

public class IncreaseBloodStockRequest
{
    public int Units { get; set; }
    public string? Notes { get; set; }
}

public class DecreaseBloodStockRequest
{
    public int Units { get; set; }
    public string? Notes { get; set; }
}

// -------------------------------------------------------------------- Queries

/// <summary>
/// Not paged - a hospital has at most eight BloodStock rows (one per BloodGroup), so a full
/// list is always cheap.
/// </summary>
public class BloodStockQueryParameters
{
    public BloodGroup? BloodGroup { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsBelowMinimum { get; set; }
}

// ----------------------------------------------------------------- Responses

public class BloodStockDto
{
    public Guid Id { get; init; }
    public Guid HospitalProfileId { get; init; }

    public BloodGroup BloodGroup { get; init; }
    public string BloodGroupDisplayName { get; init; } = string.Empty;

    public int AvailableUnits { get; init; }
    public int MinimumRequiredUnits { get; init; }
    public string? Notes { get; init; }

    public bool IsAvailable { get; init; }
    public bool IsBelowMinimum { get; init; }

    public string? LastUpdatedByName { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
