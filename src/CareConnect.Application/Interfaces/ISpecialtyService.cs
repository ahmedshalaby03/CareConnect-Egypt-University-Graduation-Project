using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Specialties;

namespace CareConnect.Application.Interfaces;

public interface ISpecialtyService
{
    /// <summary>Active specialties only, sorted by name. Backs every public dropdown.</summary>
    Task<Result<IReadOnlyList<SpecialtyOptionDto>>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>SuperAdmin listing with search, active filter and paging.</summary>
    Task<Result<PagedResult<SpecialtyDto>>> GetAllAsync(
        SpecialtyQueryParameters query,
        CancellationToken ct = default);

    Task<Result<SpecialtyDto>> CreateAsync(CreateSpecialtyRequest request, CancellationToken ct = default);

    Task<Result<SpecialtyDto>> UpdateAsync(
        Guid id,
        UpdateSpecialtyRequest request,
        CancellationToken ct = default);

    /// <summary>Flips IsActive. Specialties are never deleted, so this is the only removal path.</summary>
    Task<Result<ToggleSpecialtyStatusResponse>> ToggleStatusAsync(Guid id, CancellationToken ct = default);
}
