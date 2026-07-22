namespace CareConnect.Application.DTOs.Scheduling;

public class UnavailablePeriodDto
{
    public Guid Id { get; init; }
    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;

    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public string? Reason { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class CreateUnavailablePeriodRequest
{
    public Guid HospitalProfileId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }
}

public class UnavailablePeriodQueryParameters
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public Guid? HospitalProfileId { get; set; }
}
