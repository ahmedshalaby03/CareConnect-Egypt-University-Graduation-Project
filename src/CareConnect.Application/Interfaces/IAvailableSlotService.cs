using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Computes bookable slots on demand - nothing here is stored, and Angular never performs
/// this calculation itself.
/// </summary>
public interface IAvailableSlotService
{
    Task<Result<AvailableSlotsResponse>> GetAvailableSlotsAsync(
        Guid doctorProfileId,
        Guid hospitalProfileId,
        DateOnly date,
        CancellationToken ct = default);
}
