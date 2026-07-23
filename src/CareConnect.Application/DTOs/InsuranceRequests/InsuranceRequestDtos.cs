using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.InsuranceRequests;

// -------------------------------------------------------------------- Requests

public class CreateInsuranceRequestRequest
{
    public Guid AppointmentId { get; set; }
    public Guid InsuranceCompanyId { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public decimal? RequestedAmount { get; set; }
    public string? PatientNotes { get; set; }
    public string? InsuranceCardImageUrl { get; set; }
    public string? SupportingDocumentUrl { get; set; }
}

public class ApproveInsuranceRequestRequest
{
    public decimal? ApprovedAmount { get; set; }
    public string? ApprovalReferenceNumber { get; set; }
    public string? HospitalNotes { get; set; }
}

public class RejectInsuranceRequestRequest
{
    public string RejectionReason { get; set; } = string.Empty;
    public string? HospitalNotes { get; set; }
}

public class InsuranceHospitalNotesRequest
{
    public string? HospitalNotes { get; set; }
}

// -------------------------------------------------------------------- Queries

public class PatientInsuranceRequestQueryParameters : PagedQueryParameters
{
    public InsuranceRequestStatus? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? HospitalName { get; set; }
    public Guid? InsuranceCompanyId { get; set; }
}

public class HospitalInsuranceRequestQueryParameters : PagedQueryParameters
{
    public InsuranceRequestStatus? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public Guid? InsuranceCompanyId { get; set; }
}

// ----------------------------------------------------------------- Responses

/// <summary>Shape returned to the Patient - no HospitalNotes, matching the doctor-notes privacy pattern.</summary>
public class PatientInsuranceRequestDto
{
    public Guid InsuranceRequestId { get; init; }

    public Guid AppointmentId { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public string AppointmentStartTime { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public string? DoctorSpecialty { get; init; }
    public string HospitalName { get; init; } = string.Empty;

    public string InsuranceCompanyName { get; init; } = string.Empty;
    public string MemberNumber { get; init; } = string.Empty;
    public string ServiceDescription { get; init; } = string.Empty;
    public decimal? RequestedAmount { get; init; }
    public decimal? ApprovedAmount { get; init; }

    public InsuranceRequestStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public string? RejectionReason { get; init; }
    public string? ApprovalReferenceNumber { get; init; }

    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
}

/// <summary>
/// Shape returned to the Hospital - carries PatientNotes and HospitalNotes, plus just
/// enough patient identity to review the request. Never a password hash, security stamp
/// or refresh token.
/// </summary>
public class HospitalInsuranceRequestDto
{
    public Guid InsuranceRequestId { get; init; }

    public Guid PatientProfileId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? PatientPhoneNumber { get; init; }

    public Guid AppointmentId { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public string AppointmentStartTime { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public string? DoctorSpecialty { get; init; }

    public Guid InsuranceCompanyId { get; init; }
    public string InsuranceCompany { get; init; } = string.Empty;

    public string MemberNumber { get; init; } = string.Empty;
    public string? PolicyNumber { get; init; }
    public string ServiceDescription { get; init; } = string.Empty;
    public decimal? RequestedAmount { get; init; }
    public decimal? ApprovedAmount { get; init; }

    public string? PatientNotes { get; init; }
    public string? HospitalNotes { get; init; }

    public string? InsuranceCardImageUrl { get; init; }
    public string? SupportingDocumentUrl { get; init; }

    public InsuranceRequestStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public string? RejectionReason { get; init; }
    public string? ApprovalReferenceNumber { get; init; }

    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
}

// -------------------------------------------------------------- Dashboard stats

public class PatientInsuranceDashboardStatsDto
{
    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public string? LatestStatus { get; init; }
}

public class HospitalInsuranceDashboardStatsDto
{
    public int PendingCount { get; init; }
    public int UnderReviewCount { get; init; }
    public int ApprovedThisMonthCount { get; init; }
    public int RejectedThisMonthCount { get; init; }
}
