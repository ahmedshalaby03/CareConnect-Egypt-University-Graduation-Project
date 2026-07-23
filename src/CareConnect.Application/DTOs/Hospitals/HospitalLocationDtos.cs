namespace CareConnect.Application.DTOs.Hospitals;

/// <summary>The hospital's own view of its location - shown on the "manage my location" page.</summary>
public class HospitalLocationDto
{
    public Guid HospitalProfileId { get; init; }
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? LocationDescription { get; init; }
    public string? NearbyLandmark { get; init; }

    /// <summary>Address + Governorate + City + both coordinates. Distinct from the general profile-completion flag.</summary>
    public bool IsLocationCompleted { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public class UpdateHospitalLocationRequest
{
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public string? NearbyLandmark { get; set; }
}
