using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;

namespace CareConnect.Application.Interfaces;

public interface IMedicalServiceProviderDirectoryService
{
    Task<Result<PagedResult<MedicalServiceProviderSummaryDto>>> SearchAsync(
        MedicalServiceProviderFilter filter,
        CancellationToken ct = default);
    Task<Result<MedicalServiceProviderDetailsDto>> GetByIdAsync(
        Guid id,
        MedicalServiceProviderDetailsQuery query,
        CancellationToken ct = default);
}
