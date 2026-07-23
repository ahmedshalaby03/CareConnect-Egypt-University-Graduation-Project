using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

/// <summary>
/// One hospital's on-hand unit count for one blood group. At most one row per
/// (HospitalProfileId, BloodGroup) pair - see the unique index in
/// <c>BloodStockConfiguration</c>. Rows are never deleted, only adjusted, so history
/// survives a hospital deactivation.
/// </summary>
public class BloodStock
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public BloodGroup BloodGroup { get; set; }

    public int AvailableUnits { get; set; }
    public int MinimumRequiredUnits { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Derived server-side only, never trusted from the client - true when
    /// <see cref="AvailableUnits"/> is greater than zero.
    /// </summary>
    public bool IsAvailable { get; set; }

    public string? LastUpdatedByUserId { get; set; }
    public ApplicationUser? LastUpdatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();

    public bool IsBelowMinimum => AvailableUnits < MinimumRequiredUnits;
}
