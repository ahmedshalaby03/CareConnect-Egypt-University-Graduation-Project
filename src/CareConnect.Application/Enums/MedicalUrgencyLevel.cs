namespace CareConnect.Application.Enums;

/// <summary>
/// A navigation priority returned by the educational assistant. It is not a diagnosis
/// and must never be used as an automated clinical decision.
/// </summary>
public enum MedicalUrgencyLevel
{
    Routine = 1,
    Urgent = 2,
    Emergency = 3
}
