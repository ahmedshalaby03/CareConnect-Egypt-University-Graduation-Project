using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Bulk, in-memory appointment- and blood-availability lookups shared by
/// <see cref="HealthcareDirectoryService"/> and <see cref="HospitalDiscoveryService"/>, so
/// the two search endpoints never diverge on what "has available appointments/blood" means.
///
/// Everything here is computed for a whole candidate set in a handful of queries rather than
/// once per hospital, to avoid N+1 round trips against a page of search results.
/// </summary>
internal static class HospitalAvailabilityHelpers
{
    internal readonly record struct AppointmentAvailability(bool HasAvailableAppointments, DateTime? NextAvailableAt);

    /// <summary>
    /// Mirrors AvailableSlotService's per-doctor logic (active affiliation, active weekly
    /// availability, not blocked by an unavailable period or an existing Pending/Confirmed
    /// appointment) but evaluated in bulk across a limited 7-day window and summarised down
    /// to a single boolean plus the earliest free slot, per hospital.
    /// </summary>
    internal static async Task<Dictionary<Guid, AppointmentAvailability>> ComputeAppointmentAvailabilityAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<Guid> hospitalProfileIds,
        CancellationToken ct)
    {
        var result = new Dictionary<Guid, AppointmentAvailability>();

        if (hospitalProfileIds.Count == 0)
        {
            return result;
        }

        var affiliatedDoctorIds = await context.DoctorHospitalAffiliations
            .AsNoTracking()
            .Where(a => hospitalProfileIds.Contains(a.HospitalProfileId) && a.Status == AffiliationStatus.Approved)
            .Select(a => a.DoctorProfileId)
            .Distinct()
            .ToListAsync(ct);

        if (affiliatedDoctorIds.Count == 0)
        {
            return result;
        }

        var blocks = await context.DoctorAvailabilities
            .AsNoTracking()
            .Where(b => affiliatedDoctorIds.Contains(b.DoctorProfileId)
                        && hospitalProfileIds.Contains(b.HospitalProfileId)
                        && b.IsActive)
            .ToListAsync(ct);

        if (blocks.Count == 0)
        {
            return result;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var windowEndDate = today.AddDays(6);
        var windowEnd = windowEndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var unavailablePeriods = await context.DoctorUnavailablePeriods
            .AsNoTracking()
            .Where(p => affiliatedDoctorIds.Contains(p.DoctorProfileId)
                        && p.EndDateTime >= now
                        && p.StartDateTime <= windowEnd)
            .Select(p => new { p.DoctorProfileId, p.HospitalProfileId, p.StartDateTime, p.EndDateTime })
            .ToListAsync(ct);

        var busyAppointments = await context.Appointments
            .AsNoTracking()
            .Where(a => affiliatedDoctorIds.Contains(a.DoctorProfileId)
                        && a.AppointmentDate >= today
                        && a.AppointmentDate <= windowEndDate
                        && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
            .Select(a => new { a.DoctorProfileId, a.AppointmentDate, a.StartTime, a.EndTime })
            .ToListAsync(ct);

        foreach (var hospitalId in hospitalProfileIds)
        {
            var hospitalBlocks = blocks.Where(b => b.HospitalProfileId == hospitalId).ToList();
            if (hospitalBlocks.Count == 0)
            {
                continue;
            }

            DateTime? earliest = null;

            for (var dayOffset = 0; dayOffset <= 6 && earliest is null; dayOffset++)
            {
                var date = today.AddDays(dayOffset);
                var dayOfWeek = date.DayOfWeek;

                foreach (var block in hospitalBlocks.Where(b => b.DayOfWeek == dayOfWeek))
                {
                    foreach (var start in block.GenerateSlotStarts())
                    {
                        var end = start.AddMinutes(block.SlotDurationMinutes);
                        var slotStart = date.ToDateTime(start, DateTimeKind.Utc);
                        var slotEnd = date.ToDateTime(end, DateTimeKind.Utc);

                        if (slotStart <= now)
                        {
                            continue;
                        }

                        var blockedByUnavailability = unavailablePeriods.Any(p =>
                            p.DoctorProfileId == block.DoctorProfileId
                            && p.HospitalProfileId == hospitalId
                            && p.StartDateTime < slotEnd
                            && slotStart < p.EndDateTime);

                        if (blockedByUnavailability)
                        {
                            continue;
                        }

                        var blockedByAppointment = busyAppointments.Any(a =>
                            a.DoctorProfileId == block.DoctorProfileId
                            && a.AppointmentDate == date
                            && a.StartTime < end
                            && start < a.EndTime);

                        if (blockedByAppointment)
                        {
                            continue;
                        }

                        if (earliest is null || slotStart < earliest.Value)
                        {
                            earliest = slotStart;
                        }
                    }
                }
            }

            result[hospitalId] = new AppointmentAvailability(earliest is not null, earliest);
        }

        return result;
    }

    /// <summary>Every blood group currently in stock (AvailableUnits &gt; 0, IsAvailable) at each hospital.</summary>
    internal static async Task<Dictionary<Guid, IReadOnlyList<BloodGroup>>> ComputeAvailableBloodGroupsAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<Guid> hospitalProfileIds,
        CancellationToken ct)
    {
        if (hospitalProfileIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<BloodGroup>>();
        }

        var stocks = await context.BloodStocks
            .AsNoTracking()
            .Where(s => hospitalProfileIds.Contains(s.HospitalProfileId) && s.AvailableUnits > 0 && s.IsAvailable)
            .Select(s => new { s.HospitalProfileId, s.BloodGroup })
            .ToListAsync(ct);

        return stocks
            .GroupBy(s => s.HospitalProfileId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BloodGroup>)g.Select(x => x.BloodGroup).Distinct().OrderBy(bg => bg).ToList());
    }
}
