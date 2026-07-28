using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Dashboards;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Constants;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Read-only, role-scoped dashboard aggregates. Each query projects only the fields needed
/// by the dashboard; no profile id is accepted from the client.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private const int RecentItemLimit = 5;
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db) => _db = db;

    public async Task<Result<PatientDashboardDto>> GetPatientAsync(
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var patientId = await _db.PatientProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == currentUserId)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!patientId.HasValue)
        {
            return Result<PatientDashboardDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var activeAppointments = _db.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.PatientProfileId == patientId.Value &&
                (appointment.Status == AppointmentStatus.Pending ||
                 appointment.Status == AppointmentStatus.Confirmed) &&
                (appointment.AppointmentDate > today ||
                 (appointment.AppointmentDate == today && appointment.StartTime >= nowTime)));

        var nextRaw = await activeAppointments
            .OrderBy(appointment => appointment.AppointmentDate)
            .ThenBy(appointment => appointment.StartTime)
            .Select(appointment => new
            {
                appointment.Id,
                appointment.AppointmentDate,
                appointment.StartTime,
                DoctorName = appointment.DoctorProfile!.User!.FullName,
                HospitalName = appointment.HospitalProfile!.HospitalName ?? string.Empty,
                appointment.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        var upcomingCount = await activeAppointments.CountAsync(cancellationToken);
        var pendingAppointments = await activeAppointments
            .CountAsync(appointment => appointment.Status == AppointmentStatus.Pending, cancellationToken);
        var pendingInsurance = await _db.InsuranceRequests.AsNoTracking()
            .CountAsync(request =>
                request.PatientProfileId == patientId.Value &&
                request.Status == InsuranceRequestStatus.Pending,
                cancellationToken);
        var pendingBlood = await _db.BloodRequests.AsNoTracking()
            .CountAsync(request =>
                request.PatientProfileId == patientId.Value &&
                request.Status == BloodRequestStatus.Pending,
                cancellationToken);
        var activeServices = await _db.MedicalServiceRequests.AsNoTracking()
            .CountAsync(request =>
                request.PatientProfileId == patientId.Value &&
                (request.Status == MedicalServiceRequestStatus.Pending ||
                 request.Status == MedicalServiceRequestStatus.Accepted),
                cancellationToken);
        var unread = await GetUnreadCountAsync(currentUserId, cancellationToken);

        var eligibleDoctorReviews = await _db.Appointments.AsNoTracking()
            .CountAsync(appointment =>
                appointment.PatientProfileId == patientId.Value &&
                appointment.Status == AppointmentStatus.Completed &&
                appointment.DoctorReview == null,
                cancellationToken);
        var eligibleHospitalReviews = await _db.Appointments.AsNoTracking()
            .CountAsync(appointment =>
                appointment.PatientProfileId == patientId.Value &&
                appointment.Status == AppointmentStatus.Completed &&
                appointment.HospitalReview == null,
                cancellationToken);
        var eligibleProviderReviews = await _db.MedicalServiceRequests.AsNoTracking()
            .CountAsync(request =>
                request.PatientProfileId == patientId.Value &&
                request.Status == MedicalServiceRequestStatus.Completed &&
                request.Review == null,
                cancellationToken);

        var recentRequests = await GetRecentPatientRequestsAsync(
            patientId.Value,
            cancellationToken);

        var nextAppointment = nextRaw is null
            ? null
            : new DashboardAppointmentItemDto
            {
                Id = nextRaw.Id,
                AppointmentDate = nextRaw.AppointmentDate,
                StartTime = nextRaw.StartTime,
                PrimaryName = nextRaw.DoctorName,
                SecondaryName = nextRaw.HospitalName,
                Status = nextRaw.Status.ToString()
            };

        return Result<PatientDashboardDto>.Success(
            new PatientDashboardDto
            {
                NextAppointment = nextAppointment,
                UpcomingAppointmentsCount = upcomingCount,
                PendingAppointmentsCount = pendingAppointments,
                PendingInsuranceRequestsCount = pendingInsurance,
                PendingBloodRequestsCount = pendingBlood,
                ActiveMedicalServiceRequestsCount = activeServices,
                UnreadNotificationsCount = unread,
                EligibleReviewsCount =
                    eligibleDoctorReviews + eligibleHospitalReviews + eligibleProviderReviews,
                RecentRequests = recentRequests
            },
            "Patient dashboard retrieved successfully.");
    }

    public async Task<Result<DoctorDashboardDto>> GetDoctorAsync(
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var doctorId = await _db.DoctorProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == currentUserId)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!doctorId.HasValue)
        {
            return Result<DoctorDashboardDto>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);

        var todayCount = await _db.Appointments.AsNoTracking()
            .CountAsync(appointment =>
                appointment.DoctorProfileId == doctorId.Value &&
                appointment.AppointmentDate == today &&
                appointment.Status != AppointmentStatus.Rejected &&
                appointment.Status != AppointmentStatus.Cancelled,
                cancellationToken);
        var upcomingConfirmed = await _db.Appointments.AsNoTracking()
            .CountAsync(appointment =>
                appointment.DoctorProfileId == doctorId.Value &&
                appointment.Status == AppointmentStatus.Confirmed &&
                (appointment.AppointmentDate > today ||
                 (appointment.AppointmentDate == today && appointment.StartTime >= nowTime)),
                cancellationToken);
        var pendingAppointments = await _db.Appointments.AsNoTracking()
            .CountAsync(appointment =>
                appointment.DoctorProfileId == doctorId.Value &&
                appointment.Status == AppointmentStatus.Pending,
                cancellationToken);
        var completedAppointments = await _db.Appointments.AsNoTracking()
            .CountAsync(appointment =>
                appointment.DoctorProfileId == doctorId.Value &&
                appointment.Status == AppointmentStatus.Completed,
                cancellationToken);
        var approvedAffiliations = await _db.DoctorHospitalAffiliations.AsNoTracking()
            .CountAsync(affiliation =>
                affiliation.DoctorProfileId == doctorId.Value &&
                affiliation.Status == AffiliationStatus.Approved,
                cancellationToken);
        var pendingAffiliations = await _db.DoctorHospitalAffiliations.AsNoTracking()
            .CountAsync(affiliation =>
                affiliation.DoctorProfileId == doctorId.Value &&
                affiliation.Status == AffiliationStatus.Pending,
                cancellationToken);

        var rating = await _db.AppointmentDoctorReviews.AsNoTracking()
            .Where(review =>
                review.DoctorProfileId == doctorId.Value &&
                review.ModerationStatus == ReviewModerationStatus.Visible)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Average = (decimal?)group.Average(review => review.Rating)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var recentRaw = await _db.Appointments.AsNoTracking()
            .Where(appointment => appointment.DoctorProfileId == doctorId.Value)
            .OrderByDescending(appointment => appointment.AppointmentDate)
            .ThenByDescending(appointment => appointment.StartTime)
            .Take(RecentItemLimit)
            .Select(appointment => new
            {
                appointment.Id,
                appointment.AppointmentDate,
                appointment.StartTime,
                PatientName = appointment.PatientProfile!.User!.FullName,
                HospitalName = appointment.HospitalProfile!.HospitalName ?? string.Empty,
                appointment.Status
            })
            .ToListAsync(cancellationToken);

        return Result<DoctorDashboardDto>.Success(
            new DoctorDashboardDto
            {
                TodayAppointmentsCount = todayCount,
                UpcomingConfirmedAppointmentsCount = upcomingConfirmed,
                PendingAppointmentRequestsCount = pendingAppointments,
                CompletedAppointmentsCount = completedAppointments,
                CurrentHospitalAffiliationsCount = approvedAffiliations,
                PendingHospitalAffiliationRequestsCount = pendingAffiliations,
                AverageVisibleRating = rating?.Average is null
                    ? null
                    : Math.Round(rating.Average.Value, 1),
                VisibleReviewsCount = rating?.Count ?? 0,
                UnreadNotificationsCount = await GetUnreadCountAsync(
                    currentUserId,
                    cancellationToken),
                RecentAppointments = recentRaw.Select(item => new DashboardAppointmentItemDto
                {
                    Id = item.Id,
                    AppointmentDate = item.AppointmentDate,
                    StartTime = item.StartTime,
                    PrimaryName = item.PatientName,
                    SecondaryName = item.HospitalName,
                    Status = item.Status.ToString()
                }).ToList()
            },
            "Doctor dashboard retrieved successfully.");
    }

    public async Task<Result<HospitalDashboardDto>> GetHospitalAsync(
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = await _db.HospitalProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == currentUserId)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!hospitalId.HasValue)
        {
            return Result<HospitalDashboardDto>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var approvedDoctors = await _db.DoctorHospitalAffiliations.AsNoTracking()
            .CountAsync(affiliation =>
                affiliation.HospitalProfileId == hospitalId.Value &&
                affiliation.Status == AffiliationStatus.Approved,
                cancellationToken);
        var pendingAffiliations = await _db.DoctorHospitalAffiliations.AsNoTracking()
            .CountAsync(affiliation =>
                affiliation.HospitalProfileId == hospitalId.Value &&
                affiliation.Status == AffiliationStatus.Pending,
                cancellationToken);
        var todayAppointments = await _db.Appointments.AsNoTracking()
            .CountAsync(appointment =>
                appointment.HospitalProfileId == hospitalId.Value &&
                appointment.AppointmentDate == today &&
                appointment.Status != AppointmentStatus.Rejected &&
                appointment.Status != AppointmentStatus.Cancelled,
                cancellationToken);
        var pendingInsurance = await _db.InsuranceRequests.AsNoTracking()
            .CountAsync(request =>
                request.HospitalProfileId == hospitalId.Value &&
                request.Status == InsuranceRequestStatus.Pending,
                cancellationToken);
        var pendingBlood = await _db.BloodRequests.AsNoTracking()
            .CountAsync(request =>
                request.HospitalProfileId == hospitalId.Value &&
                request.Status == BloodRequestStatus.Pending,
                cancellationToken);
        var lowStock = await _db.BloodStocks.AsNoTracking()
            .CountAsync(stock =>
                stock.HospitalProfileId == hospitalId.Value &&
                stock.AvailableUnits < stock.MinimumRequiredUnits,
                cancellationToken);
        var rating = await _db.AppointmentHospitalReviews.AsNoTracking()
            .Where(review =>
                review.HospitalProfileId == hospitalId.Value &&
                review.ModerationStatus == ReviewModerationStatus.Visible)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Average = (decimal?)group.Average(review => review.Rating)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return Result<HospitalDashboardDto>.Success(
            new HospitalDashboardDto
            {
                ActiveAffiliatedDoctorsCount = approvedDoctors,
                PendingDoctorAffiliationRequestsCount = pendingAffiliations,
                TodayAppointmentsCount = todayAppointments,
                PendingInsuranceRequestsCount = pendingInsurance,
                PendingBloodRequestsCount = pendingBlood,
                LowBloodStockGroupsCount = lowStock,
                AverageVisibleRating = rating?.Average is null
                    ? null
                    : Math.Round(rating.Average.Value, 1),
                VisibleReviewsCount = rating?.Count ?? 0,
                UnreadNotificationsCount = await GetUnreadCountAsync(
                    currentUserId,
                    cancellationToken)
            },
            "Hospital dashboard retrieved successfully.");
    }

    public async Task<Result<MedicalServiceProviderDashboardDto>> GetMedicalServiceProviderAsync(
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.MedicalServiceProviderProfiles
            .AsNoTracking()
            .Where(item => item.UserId == currentUserId)
            .Select(item => new
            {
                item.Id,
                BusinessName = item.BusinessName ?? string.Empty,
                item.IsPublished
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Result<MedicalServiceProviderDashboardDto>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        var now = DateTime.UtcNow;
        var activeServices = await _db.MedicalServiceOfferings.AsNoTracking()
            .CountAsync(service =>
                service.MedicalServiceProviderProfileId == profile.Id && service.IsActive,
                cancellationToken);
        var inactiveServices = await _db.MedicalServiceOfferings.AsNoTracking()
            .CountAsync(service =>
                service.MedicalServiceProviderProfileId == profile.Id && !service.IsActive,
                cancellationToken);
        var pendingRequests = await _db.MedicalServiceRequests.AsNoTracking()
            .CountAsync(request =>
                request.MedicalServiceProviderProfileId == profile.Id &&
                request.Status == MedicalServiceRequestStatus.Pending,
                cancellationToken);
        var acceptedUpcoming = await _db.MedicalServiceRequests.AsNoTracking()
            .CountAsync(request =>
                request.MedicalServiceProviderProfileId == profile.Id &&
                request.Status == MedicalServiceRequestStatus.Accepted &&
                request.ScheduledAt.HasValue &&
                request.ScheduledAt.Value >= now,
                cancellationToken);
        var completed = await _db.MedicalServiceRequests.AsNoTracking()
            .CountAsync(request =>
                request.MedicalServiceProviderProfileId == profile.Id &&
                request.Status == MedicalServiceRequestStatus.Completed,
                cancellationToken);
        var rating = await _db.MedicalServiceProviderReviews.AsNoTracking()
            .Where(review =>
                review.MedicalServiceProviderProfileId == profile.Id &&
                review.ModerationStatus == ReviewModerationStatus.Visible)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Average = (decimal?)group.Average(review => review.Rating)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var upcomingRaw = await _db.MedicalServiceRequests.AsNoTracking()
            .Where(request =>
                request.MedicalServiceProviderProfileId == profile.Id &&
                request.Status == MedicalServiceRequestStatus.Accepted &&
                request.ScheduledAt.HasValue &&
                request.ScheduledAt.Value >= now)
            .OrderBy(request => request.ScheduledAt)
            .Take(RecentItemLimit)
            .Select(request => new
            {
                request.Id,
                request.RequestNumber,
                request.ServiceNameSnapshot,
                request.Status,
                request.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<MedicalServiceProviderDashboardDto>.Success(
            new MedicalServiceProviderDashboardDto
            {
                BusinessName = profile.BusinessName,
                IsPublished = profile.IsPublished,
                ActiveServicesCount = activeServices,
                InactiveServicesCount = inactiveServices,
                PendingRequestsCount = pendingRequests,
                AcceptedUpcomingRequestsCount = acceptedUpcoming,
                CompletedRequestsCount = completed,
                AverageVisibleRating = rating?.Average is null
                    ? null
                    : Math.Round(rating.Average.Value, 1),
                VisibleReviewsCount = rating?.Count ?? 0,
                UnreadNotificationsCount = await GetUnreadCountAsync(
                    currentUserId,
                    cancellationToken),
                UpcomingRequests = upcomingRaw.Select(request => new DashboardRequestItemDto
                {
                    Id = request.Id,
                    Category = "Medical service",
                    Title = $"{request.RequestNumber} · {request.ServiceNameSnapshot}",
                    Status = request.Status.ToString(),
                    SubmittedAt = request.CreatedAt,
                    ActionRoute = $"/dashboard/service-provider/requests/{request.Id}"
                }).ToList()
            },
            "Medical service provider dashboard retrieved successfully.");
    }

    public async Task<Result<SuperAdminDashboardDto>> GetSuperAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
        var activeUsers = await _db.Users.AsNoTracking()
            .CountAsync(user => user.IsActive, cancellationToken);

        var roleCounts = await (
                from userRole in _db.UserRoles.AsNoTracking()
                join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                group userRole by role.Name into grouped
                select new { Role = grouped.Key!, Count = grouped.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);

        var specialtyCount = await _db.Specialties.AsNoTracking()
            .CountAsync(specialty => specialty.IsActive, cancellationToken);
        var insuranceCompanyCount = await _db.InsuranceCompanies.AsNoTracking()
            .CountAsync(company => company.IsActive, cancellationToken);
        var serviceCategoryCount = await _db.MedicalServiceCategories.AsNoTracking()
            .CountAsync(category => category.IsActive, cancellationToken);

        var visibleReviews =
            await _db.AppointmentDoctorReviews.AsNoTracking()
                .CountAsync(review => review.ModerationStatus == ReviewModerationStatus.Visible, cancellationToken) +
            await _db.AppointmentHospitalReviews.AsNoTracking()
                .CountAsync(review => review.ModerationStatus == ReviewModerationStatus.Visible, cancellationToken) +
            await _db.MedicalServiceProviderReviews.AsNoTracking()
                .CountAsync(review => review.ModerationStatus == ReviewModerationStatus.Visible, cancellationToken);
        var hiddenReviews =
            await _db.AppointmentDoctorReviews.AsNoTracking()
                .CountAsync(review => review.ModerationStatus == ReviewModerationStatus.Hidden, cancellationToken) +
            await _db.AppointmentHospitalReviews.AsNoTracking()
                .CountAsync(review => review.ModerationStatus == ReviewModerationStatus.Hidden, cancellationToken) +
            await _db.MedicalServiceProviderReviews.AsNoTracking()
                .CountAsync(review => review.ModerationStatus == ReviewModerationStatus.Hidden, cancellationToken);

        var recentRegistrations = await (
                from user in _db.Users.AsNoTracking()
                join userRole in _db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                orderby user.CreatedAt descending
                select new RecentRegistrationDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = role.Name ?? string.Empty,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                })
            .Take(RecentItemLimit)
            .ToListAsync(cancellationToken);

        int RoleCount(string role) => roleCounts.GetValueOrDefault(role);

        return Result<SuperAdminDashboardDto>.Success(
            new SuperAdminDashboardDto
            {
                TotalUsersCount = totalUsers,
                ActiveUsersCount = activeUsers,
                InactiveUsersCount = totalUsers - activeUsers,
                PatientsCount = RoleCount(AppRoles.Patient),
                DoctorsCount = RoleCount(AppRoles.Doctor),
                HospitalsCount = RoleCount(AppRoles.Hospital),
                MedicalServiceProvidersCount = RoleCount(AppRoles.MedicalServiceProvider),
                MedicalSpecialtiesCount = specialtyCount,
                InsuranceCompaniesCount = insuranceCompanyCount,
                MedicalServiceCategoriesCount = serviceCategoryCount,
                VisibleReviewsCount = visibleReviews,
                HiddenReviewsCount = hiddenReviews,
                RecentRegistrations = recentRegistrations
            },
            "SuperAdmin dashboard retrieved successfully.");
    }

    private Task<int> GetUnreadCountAsync(
        string currentUserId,
        CancellationToken cancellationToken) =>
        _db.Notifications.AsNoTracking().CountAsync(notification =>
            notification.RecipientApplicationUserId == currentUserId &&
            !notification.IsRead &&
            notification.DismissedAt == null,
            cancellationToken);

    private async Task<IReadOnlyList<DashboardRequestItemDto>> GetRecentPatientRequestsAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        var insuranceRaw = await _db.InsuranceRequests.AsNoTracking()
            .Where(request => request.PatientProfileId == patientProfileId)
            .OrderByDescending(request => request.SubmittedAt)
            .Take(RecentItemLimit)
            .Select(request => new
            {
                request.Id,
                request.ServiceDescription,
                request.Status,
                request.SubmittedAt
            })
            .ToListAsync(cancellationToken);

        var bloodRaw = await _db.BloodRequests.AsNoTracking()
            .Where(request => request.PatientProfileId == patientProfileId)
            .OrderByDescending(request => request.SubmittedAt)
            .Take(RecentItemLimit)
            .Select(request => new
            {
                request.Id,
                request.BeneficiaryName,
                request.BloodGroup,
                request.Status,
                request.SubmittedAt
            })
            .ToListAsync(cancellationToken);

        var serviceRaw = await _db.MedicalServiceRequests.AsNoTracking()
            .Where(request => request.PatientProfileId == patientProfileId)
            .OrderByDescending(request => request.CreatedAt)
            .Take(RecentItemLimit)
            .Select(request => new
            {
                request.Id,
                request.RequestNumber,
                request.ServiceNameSnapshot,
                request.Status,
                request.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return insuranceRaw.Select(request => new DashboardRequestItemDto
            {
                Id = request.Id,
                Category = "Insurance",
                Title = request.ServiceDescription,
                Status = request.Status.ToString(),
                SubmittedAt = request.SubmittedAt,
                ActionRoute = $"/dashboard/patient/insurance-requests/{request.Id}"
            })
            .Concat(bloodRaw.Select(request => new DashboardRequestItemDto
            {
                Id = request.Id,
                Category = "Blood",
                Title = $"{request.BloodGroup} for {request.BeneficiaryName}",
                Status = request.Status.ToString(),
                SubmittedAt = request.SubmittedAt,
                ActionRoute = $"/dashboard/patient/blood-requests/{request.Id}"
            }))
            .Concat(serviceRaw.Select(request => new DashboardRequestItemDto
            {
                Id = request.Id,
                Category = "Medical service",
                Title = $"{request.RequestNumber} · {request.ServiceNameSnapshot}",
                Status = request.Status.ToString(),
                SubmittedAt = request.CreatedAt,
                ActionRoute = $"/dashboard/patient/service-requests/{request.Id}"
            }))
            .OrderByDescending(request => request.SubmittedAt)
            .Take(RecentItemLimit)
            .ToList();
    }
}
