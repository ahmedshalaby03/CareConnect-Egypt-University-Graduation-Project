namespace CareConnect.Domain.Enums;

/// <summary>
/// A request-priority indicator the patient sets and the hospital sees when triaging its
/// queue. This is not a medical decision-support signal - nothing in the system acts on it
/// automatically.
/// </summary>
public enum BloodRequestUrgency
{
    Normal = 1,
    Urgent = 2,
    Emergency = 3
}
