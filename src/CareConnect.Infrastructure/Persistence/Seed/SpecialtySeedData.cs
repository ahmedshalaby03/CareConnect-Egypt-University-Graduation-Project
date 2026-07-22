namespace CareConnect.Infrastructure.Persistence.Seed;

public record SpecialtySeed(string Name, string ArabicName, string Description);

/// <summary>
/// The starting specialty list. Seeding matches on Name, so adding entries here later tops
/// the table up without disturbing anything the SuperAdmin has edited.
/// </summary>
public static class SpecialtySeedData
{
    public static readonly IReadOnlyList<SpecialtySeed> Items =
    [
        new("General Medicine", "الطب العام", "General consultations, diagnosis and routine care."),
        new("Cardiology", "أمراض القلب", "Heart and cardiovascular system conditions."),
        new("Dermatology", "الأمراض الجلدية", "Skin, hair and nail conditions."),
        new("Pediatrics", "طب الأطفال", "Medical care for infants, children and adolescents."),
        new("Orthopedics", "جراحة العظام", "Bones, joints, ligaments and musculoskeletal injuries."),
        new("Obstetrics and Gynecology", "النساء والتوليد", "Pregnancy, childbirth and women's reproductive health."),
        new("Dentistry", "طب الأسنان", "Oral health, teeth and gum treatment."),
        new("Neurology", "المخ والأعصاب", "Brain, spinal cord and nervous system disorders."),
        new("Ophthalmology", "طب العيون", "Eye examinations, vision care and eye surgery."),
        new("ENT", "الأنف والأذن والحنجرة", "Ear, nose and throat conditions."),
        new("Psychiatry", "الطب النفسي", "Mental health assessment and treatment."),
        new("General Surgery", "الجراحة العامة", "General surgical procedures and operative care.")
    ];
}
