using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.MedicalServiceRequests;

public static class MedicalServiceRequestLimits
{
    public const int MaximumBookingDays = 90;
    public const int MaximumSearchLength = 150;
}

// -------------------------------------------------------------------- Requests

public class CreateMedicalServiceRequestRequest
{
    public Guid MedicalServiceOfferingId { get; set; }
    public DateOnly RequestedDate { get; set; }
    public string PreferredStartTime { get; set; } = string.Empty;
    public ServiceDeliveryMode DeliveryMode { get; set; }
    public string? PatientNotes { get; set; }
    public string? HomeVisitAddress { get; set; }
}

public class AcceptMedicalServiceRequestRequest
{
    public DateOnly ScheduledDate { get; set; }
    public string ScheduledStartTime { get; set; } = string.Empty;
    public string? ProviderResponseNote { get; set; }
}

public class RejectMedicalServiceRequestRequest
{
    public string RejectionReason { get; set; } = string.Empty;
    public string? ProviderResponseNote { get; set; }
}

public class CancelMedicalServiceRequestRequest
{
    public string CancellationReason { get; set; } = string.Empty;
}

// --------------------------------------------------------------------- Filters

public class PatientMedicalServiceRequestFilter : PagedQueryParameters
{
    public string? Search { get; set; }
    public MedicalServiceRequestStatus? Status { get; set; }
    public Guid? ProviderId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
}

public class ProviderMedicalServiceRequestFilter : PagedQueryParameters
{
    public string? Search { get; set; }
    public MedicalServiceRequestStatus? Status { get; set; }
    public Guid? ServiceId { get; set; }
    public ServiceDeliveryMode? DeliveryMode { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string SortBy { get; set; } = "priority";
    public string SortDirection { get; set; } = "asc";
}

// ------------------------------------------------------------------- Responses

public class MedicalServiceRequestStatusHistoryDto
{
    public MedicalServiceRequestStatus? PreviousStatus { get; init; }
    public MedicalServiceRequestStatus NewStatus { get; init; }
    public string NewStatusName { get; init; } = string.Empty;
    public string ActorLabel { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class MedicalServiceRequestSummaryDto
{
    public Guid Id { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public Guid ProviderId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public ServiceDeliveryMode DeliveryMode { get; init; }
    public string DeliveryModeName { get; init; } = string.Empty;
    public DateOnly RequestedDate { get; init; }
    public string PreferredStartTime { get; init; } = string.Empty;
    public DateTime? ScheduledAt { get; init; }
    public decimal PriceSnapshot { get; init; }
    public MedicalServiceRequestStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public class MedicalServiceRequestDetailsDto : MedicalServiceRequestSummaryDto
{
    public string? ProviderTypeName { get; init; }
    public string? ProviderPhoneNumber { get; init; }
    public string? ProviderAddress { get; init; }
    public string? PatientPhoneNumber { get; init; }
    public int? DurationMinutesSnapshot { get; init; }
    public string? PatientNotes { get; init; }
    public string? HomeVisitAddress { get; init; }
    public string? ProviderResponseNote { get; init; }
    public string? RejectionReason { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public IReadOnlyList<MedicalServiceRequestStatusHistoryDto> StatusHistory { get; init; } = [];
}

public class MedicalServiceRequestDashboardSummaryDto
{
    public int PendingCount { get; init; }
    public int AcceptedUpcomingCount { get; init; }
    public int CompletedCount { get; init; }
    public int CancelledOrRejectedCount { get; init; }
}
