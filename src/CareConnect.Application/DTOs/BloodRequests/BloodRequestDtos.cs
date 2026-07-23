using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.BloodRequests;

// -------------------------------------------------------------------- Requests

public class CreateBloodRequestRequest
{
    public Guid HospitalProfileId { get; set; }
    public BloodGroup BloodGroup { get; set; }
    public int UnitsRequested { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public int? BeneficiaryAge { get; set; }
    public string ContactPhoneNumber { get; set; } = string.Empty;
    public string? MedicalCondition { get; set; }
    public string? HospitalOrFacilityName { get; set; }
    public string? RequestNotes { get; set; }
    public BloodRequestUrgency Urgency { get; set; } = BloodRequestUrgency.Normal;
}

public class ApproveBloodRequestRequest
{
    public string? HospitalNotes { get; set; }
}

public class RejectBloodRequestRequest
{
    public string RejectionReason { get; set; } = string.Empty;
    public string? HospitalNotes { get; set; }
}

public class BloodRequestHospitalNotesRequest
{
    public string? HospitalNotes { get; set; }
}

// -------------------------------------------------------------------- Queries

public class PatientBloodRequestQueryParameters : PagedQueryParameters
{
    public BloodRequestStatus? Status { get; set; }
    public BloodGroup? BloodGroup { get; set; }
    public BloodRequestUrgency? Urgency { get; set; }
    public string? HospitalName { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public class HospitalBloodRequestQueryParameters : PagedQueryParameters
{
    public BloodRequestStatus? Status { get; set; }
    public BloodGroup? BloodGroup { get; set; }
    public BloodRequestUrgency? Urgency { get; set; }
    public string? PatientName { get; set; }
    public string? BeneficiaryName { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

// ----------------------------------------------------------------- Responses

/// <summary>Shape returned to the Patient - no internal hospital review data beyond notes/reason.</summary>
public class PatientBloodRequestDto
{
    public Guid BloodRequestId { get; init; }

    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? HospitalAddress { get; init; }
    public string? HospitalPhoneNumber { get; init; }

    public BloodGroup BloodGroup { get; init; }
    public string BloodGroupDisplayName { get; init; } = string.Empty;
    public int UnitsRequested { get; init; }
    public string BeneficiaryName { get; init; } = string.Empty;
    public string ContactPhoneNumber { get; init; } = string.Empty;

    public BloodRequestUrgency Urgency { get; init; }
    public string UrgencyName { get; init; } = string.Empty;

    public BloodRequestStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public string? RejectionReason { get; init; }
    public string? HospitalNotes { get; init; }

    public DateTime SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? FulfilledAt { get; init; }
    public DateTime? CancelledAt { get; init; }
}

/// <summary>
/// Shape returned to the Hospital - carries the patient/beneficiary detail a reviewer needs.
/// Never a password hash, security stamp or refresh token.
/// </summary>
public class HospitalBloodRequestDto
{
    public Guid BloodRequestId { get; init; }

    public Guid PatientProfileId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? PatientPhoneNumber { get; init; }

    public string BeneficiaryName { get; init; } = string.Empty;
    public int? BeneficiaryAge { get; init; }
    public string ContactPhoneNumber { get; init; } = string.Empty;

    public BloodGroup BloodGroup { get; init; }
    public string BloodGroupDisplayName { get; init; } = string.Empty;
    public int UnitsRequested { get; init; }
    public int CurrentAvailableUnits { get; init; }

    public string? MedicalCondition { get; init; }
    public string? HospitalOrFacilityName { get; init; }
    public string? RequestNotes { get; init; }
    public string? HospitalNotes { get; init; }

    public BloodRequestUrgency Urgency { get; init; }
    public string UrgencyName { get; init; } = string.Empty;

    public BloodRequestStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public string? RejectionReason { get; init; }

    public DateTime SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? FulfilledAt { get; init; }
    public DateTime? CancelledAt { get; init; }
}

// -------------------------------------------------------------- Dashboard stats

public class PatientBloodDashboardStatsDto
{
    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public string? LatestStatus { get; init; }
}

public class HospitalBloodDashboardStatsDto
{
    public int TotalAvailableUnits { get; init; }
    public int BloodGroupsBelowMinimumCount { get; init; }
    public int PendingRequestsCount { get; init; }
    public int EmergencyRequestsCount { get; init; }
    public int ApprovedAwaitingFulfillmentCount { get; init; }
}

public class SuperAdminBloodDashboardStatsDto
{
    public int HospitalsWithStockCount { get; init; }
    public int ActiveBloodStockRecordsCount { get; init; }
}
