using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceCompanies;

namespace CareConnect.Application.Interfaces;

public interface IInsuranceCompanyService
{
    /// <summary>Active companies only, sorted by name. Backs the patient's request form.</summary>
    Task<Result<IReadOnlyList<InsuranceCompanyOptionDto>>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>SuperAdmin listing with search, active filter and paging.</summary>
    Task<Result<PagedResult<InsuranceCompanyDto>>> GetAllAsync(
        InsuranceCompanyQueryParameters query,
        CancellationToken ct = default);

    Task<Result<InsuranceCompanyDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<InsuranceCompanyDto>> CreateAsync(
        CreateInsuranceCompanyRequest request,
        CancellationToken ct = default);

    Task<Result<InsuranceCompanyDto>> UpdateAsync(
        Guid id,
        UpdateInsuranceCompanyRequest request,
        CancellationToken ct = default);

    /// <summary>Flips IsActive. Companies are never deleted, so this is the only removal path.</summary>
    Task<Result<ToggleInsuranceCompanyStatusResponse>> ToggleStatusAsync(
        Guid id,
        CancellationToken ct = default);
}
