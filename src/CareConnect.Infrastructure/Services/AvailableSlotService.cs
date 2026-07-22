using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Computes bookable slots for one doctor, at one hospital, on one date. Nothing here is
/// persisted, and the same logic runs again inside <see cref="AppointmentService"/>
/// immediately before a booking is saved, so a stale client-side slot list can never
/// double-book anyone.
/// </summary>
public class AvailableSlotService : IAvailableSlotService
{
    private readonly ApplicationDbContext _context;

    public AvailableSlotService(ApplicationDbContext context) => _context = context;

    public async Task<Result<AvailableSlotsResponse>> GetAvailableSlotsAsync(
        Guid doctorProfileId,
        Guid hospitalProfileId,
        DateOnly date,
        CancellationToken ct = default)
    {
        var doctor = await _context.DoctorProfiles
            .AsNoTracking()
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == doctorProfileId, ct);

        if (doctor is null)
        {
            return Result<AvailableSlotsResponse>.NotFound("Doctor not found.");
        }

        var hospital = await _context.HospitalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == hospitalProfileId, ct);

        if (hospital is null)
        {
            return Result<AvailableSlotsResponse>.NotFound("Hospital not found.");
        }

        // Local so every early-exit and the final result build the response the same way,
        // without repeating the doctor/hospital fields at each return point.
        AvailableSlotsResponse BuildResponse(int slotDurationMinutes, IReadOnlyList<SlotDto> slots) => new()
        {
            DoctorProfileId = doctor.Id,
            DoctorName = doctor.User?.FullName ?? string.Empty,
            HospitalProfileId = hospital.Id,
            HospitalName = hospital.HospitalName ?? string.Empty,
            Date = date,
            SlotDurationMinutes = slotDurationMinutes,
            Slots = slots
        };

        var emptyResponse = BuildResponse(0, []);

        // Not bookable for any of these reasons - all of them come back as an empty slot
        // list rather than an error, per the "do not throw just because nothing is
        // available" rule.
        if (doctor.User?.IsActive != true)
        {
            return Result<AvailableSlotsResponse>.Success(emptyResponse, "Slots retrieved successfully.");
        }

        var isApproved = await _context.DoctorHospitalAffiliations.AnyAsync(
            a => a.DoctorProfileId == doctorProfileId
                 && a.HospitalProfileId == hospitalProfileId
                 && a.Status == AffiliationStatus.Approved,
            ct);

        if (!isApproved)
        {
            return Result<AvailableSlotsResponse>.Success(emptyResponse, "Slots retrieved successfully.");
        }

        var dayOfWeek = date.DayOfWeek;

        var blocks = await _context.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorProfileId
                        && a.HospitalProfileId == hospitalProfileId
                        && a.DayOfWeek == dayOfWeek
                        && a.IsActive)
            .ToListAsync(ct);

        if (blocks.Count == 0)
        {
            return Result<AvailableSlotsResponse>.Success(emptyResponse, "Slots retrieved successfully.");
        }

        var unavailablePeriods = await _context.DoctorUnavailablePeriods
            .AsNoTracking()
            .Where(p => p.DoctorProfileId == doctorProfileId && p.HospitalProfileId == hospitalProfileId)
            .Select(p => new { p.StartDateTime, p.EndDateTime })
            .ToListAsync(ct);

        // A doctor is one person: booked time blocks the slot regardless of which hospital
        // the other appointment is at.
        var busyRanges = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorProfileId
                        && a.AppointmentDate == date
                        && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var slots = new List<SlotDto>();

        foreach (var block in blocks)
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

                if (unavailablePeriods.Any(p => p.StartDateTime < slotEnd && slotStart < p.EndDateTime))
                {
                    continue;
                }

                if (busyRanges.Any(b => b.StartTime < end && start < b.EndTime))
                {
                    continue;
                }

                slots.Add(new SlotDto
                {
                    StartTime = start.ToString("HH:mm:ss"),
                    EndTime = end.ToString("HH:mm:ss")
                });
            }
        }

        var response = BuildResponse(
            blocks[0].SlotDurationMinutes,
            slots.OrderBy(s => s.StartTime, StringComparer.Ordinal).ToList());

        return Result<AvailableSlotsResponse>.Success(response, "Slots retrieved successfully.");
    }
}
