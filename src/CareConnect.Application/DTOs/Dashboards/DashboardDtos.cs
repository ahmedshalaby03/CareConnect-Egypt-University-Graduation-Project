namespace CareConnect.Application.DTOs.Dashboards;

public sealed class DashboardAppointmentItemDto
{
    public Guid Id { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public string PrimaryName { get; init; } = string.Empty;
    public string SecondaryName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class DashboardRequestItemDto
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
    public string ActionRoute { get; init; } = string.Empty;
}

public sealed class PatientDashboardDto
{
    public DashboardAppointmentItemDto? NextAppointment { get; init; }
    public int UpcomingAppointmentsCount { get; init; }
    public int PendingAppointmentsCount { get; init; }
    public int PendingInsuranceRequestsCount { get; init; }
    public int PendingBloodRequestsCount { get; init; }
    public int ActiveMedicalServiceRequestsCount { get; init; }
    public int UnreadNotificationsCount { get; init; }
    public int EligibleReviewsCount { get; init; }
    public IReadOnlyList<DashboardRequestItemDto> RecentRequests { get; init; } = [];
}

public sealed class DoctorDashboardDto
{
    public int TodayAppointmentsCount { get; init; }
    public int UpcomingConfirmedAppointmentsCount { get; init; }
    public int PendingAppointmentRequestsCount { get; init; }
    public int CompletedAppointmentsCount { get; init; }
    public int CurrentHospitalAffiliationsCount { get; init; }
    public int PendingHospitalAffiliationRequestsCount { get; init; }
    public decimal? AverageVisibleRating { get; init; }
    public int VisibleReviewsCount { get; init; }
    public int UnreadNotificationsCount { get; init; }
    public IReadOnlyList<DashboardAppointmentItemDto> RecentAppointments { get; init; } = [];
}

public sealed class HospitalDashboardDto
{
    public int ActiveAffiliatedDoctorsCount { get; init; }
    public int PendingDoctorAffiliationRequestsCount { get; init; }
    public int TodayAppointmentsCount { get; init; }
    public int PendingInsuranceRequestsCount { get; init; }
    public int PendingBloodRequestsCount { get; init; }
    public int LowBloodStockGroupsCount { get; init; }
    public decimal? AverageVisibleRating { get; init; }
    public int VisibleReviewsCount { get; init; }
    public int UnreadNotificationsCount { get; init; }
}

public sealed class MedicalServiceProviderDashboardDto
{
    public string BusinessName { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public int ActiveServicesCount { get; init; }
    public int InactiveServicesCount { get; init; }
    public int PendingRequestsCount { get; init; }
    public int AcceptedUpcomingRequestsCount { get; init; }
    public int CompletedRequestsCount { get; init; }
    public decimal? AverageVisibleRating { get; init; }
    public int VisibleReviewsCount { get; init; }
    public int UnreadNotificationsCount { get; init; }
    public IReadOnlyList<DashboardRequestItemDto> UpcomingRequests { get; init; } = [];
}

public sealed class RecentRegistrationDto
{
    public string UserId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class SuperAdminDashboardDto
{
    public int TotalUsersCount { get; init; }
    public int ActiveUsersCount { get; init; }
    public int InactiveUsersCount { get; init; }
    public int PatientsCount { get; init; }
    public int DoctorsCount { get; init; }
    public int HospitalsCount { get; init; }
    public int MedicalServiceProvidersCount { get; init; }
    public int MedicalSpecialtiesCount { get; init; }
    public int InsuranceCompaniesCount { get; init; }
    public int MedicalServiceCategoriesCount { get; init; }
    public int VisibleReviewsCount { get; init; }
    public int HiddenReviewsCount { get; init; }
    public IReadOnlyList<RecentRegistrationDto> RecentRegistrations { get; init; } = [];
}
