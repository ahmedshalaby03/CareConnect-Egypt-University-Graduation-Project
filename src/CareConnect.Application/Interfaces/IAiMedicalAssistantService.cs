using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.AiMedicalAssistant;

namespace CareConnect.Application.Interfaces;

public interface IAiMedicalAssistantService
{
    Task<Result<MedicalAssistantChatResponse>> ChatAsync(
        string userId,
        MedicalAssistantChatRequest request,
        CancellationToken ct = default);
}
