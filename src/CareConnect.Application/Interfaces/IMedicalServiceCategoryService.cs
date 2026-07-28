using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;

namespace CareConnect.Application.Interfaces;

public interface IMedicalServiceCategoryService
{
    Task<Result<IReadOnlyList<MedicalServiceCategoryOptionDto>>> GetActiveAsync(
        CancellationToken ct = default);
    Task<Result<PagedResult<MedicalServiceCategoryDto>>> GetAllAsync(
        MedicalServiceCategoryQueryParameters query,
        CancellationToken ct = default);
    Task<Result<MedicalServiceCategoryDto>> CreateAsync(
        CreateMedicalServiceCategoryRequest request,
        CancellationToken ct = default);
    Task<Result<MedicalServiceCategoryDto>> UpdateAsync(
        Guid id,
        UpdateMedicalServiceCategoryRequest request,
        CancellationToken ct = default);
    Task<Result<MedicalServiceCategoryDto>> SetStatusAsync(
        Guid id,
        SetMedicalServiceCategoryStatusRequest request,
        CancellationToken ct = default);
}
