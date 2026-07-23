using CareConnect.Domain.Constants;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Persistence.Seed;

/// <summary>
/// Local-Development-only demo data: enough hospitals, doctors, schedules, appointments,
/// insurance requests and blood-bank records for the Angular app to have something real to
/// show. Every phase resolves existing rows by a stable natural key first and only inserts
/// what is missing, so running this on every startup never duplicates anything.
///
/// DEVELOPMENT ONLY: every demo account below uses a fixed, publicly-known password. Never
/// point this seeder at a non-Development database, and never reuse these credentials
/// anywhere real.
/// </summary>
public class DemoDataSeeder : IDemoDataSeeder
{
    /// <summary>Demo-only password for newly created Doctor/Hospital/MedicalServiceProvider accounts. Local Development only - never used in Production.</summary>
    private const string DemoPassword = "CareConnect@123";

    private const string OldSuperAdminEmail = "admin@careconnect.com";
    private const string NewSuperAdminEmail = "admin@gmail.com";
    /// <summary>Demo-only SuperAdmin password. Local Development only - never used in Production.</summary>
    private const string NewSuperAdminPassword = "Admin@123";

    private const string ExistingPatientEmail = "ahmed@gmail.com";
    /// <summary>Demo-only Patient password, applied only as a fallback if sign-in fails. Local Development only.</summary>
    private const string ExistingPatientPassword = "Ahmed@123";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Demo data seeding started.");

        await SeedRolesAsync();
        await UpdateExistingSuperAdminAsync();
        var patient = await ValidateExistingPatientAsync(ct);

        await SeedSpecialtiesAsync(ct);

        var hospitals = await SeedHospitalUsersAndProfilesAsync(ct);
        var doctors = await SeedDoctorUsersAndProfilesAsync(ct);
        await SeedMedicalServiceProviderAsync(ct);

        await SeedHospitalSpecialtiesAsync(hospitals, ct);
        await SeedDoctorAffiliationsAsync(doctors, hospitals, ct);
        await SeedDoctorAvailabilityAsync(doctors, hospitals, ct);

        await SeedInsuranceCompaniesAsync(ct);
        await SeedBloodStockAsync(hospitals, ct);

        if (patient is not null)
        {
            var appointments = await SeedAppointmentsAsync(patient, doctors, hospitals, ct);
            await SeedInsuranceRequestsAsync(patient, appointments, hospitals, ct);
            await SeedBloodRequestsAsync(patient, hospitals, ct);
        }
        else
        {
            _logger.LogWarning(
                "Skipping appointment, insurance-request and blood-request demo data: the " +
                "existing Patient account '{Email}' was not found.", ExistingPatientEmail);
        }

        _logger.LogInformation("Demo data seeding completed.");
    }

    // ================================================================= Identity

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in AppRoles.All)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            await _roleManager.CreateAsync(new ApplicationRole(roleName));
            _logger.LogInformation("Seeded role {Role}.", roleName);
        }

        _logger.LogInformation("Roles verified.");
    }

    /// <summary>
    /// Renames the existing SuperAdmin account from the bootstrap email to the demo email and
    /// resets its password - never deletes or recreates the account, so its user id and every
    /// record that references it survive untouched.
    /// </summary>
    private async Task UpdateExistingSuperAdminAsync()
    {
        var oldAccount = await _userManager.FindByEmailAsync(OldSuperAdminEmail);
        var newAccount = await _userManager.FindByEmailAsync(NewSuperAdminEmail);

        if (oldAccount is not null && newAccount is not null && oldAccount.Id != newAccount.Id)
        {
            // The bootstrap seeder (DatabaseSeeder) used to target the old email on every
            // startup, so once this seeder renamed the real account away, the next run's
            // bootstrap pass would recreate a brand-new, never-signed-in account at the old
            // email. appsettings.Development.json now points the bootstrap seeder at the new
            // email too, so this should only ever fire once, for that leftover duplicate -
            // safe to remove per the "last resort" rule: no dependent records, never used to
            // sign in, reason logged clearly. Any account that has issued a refresh token is
            // left completely untouched and reported instead.
            var everSignedIn = await _context.RefreshTokens.AnyAsync(t => t.UserId == oldAccount.Id);

            if (!everSignedIn && string.Equals(oldAccount.Email, OldSuperAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                var deleteResult = await _userManager.DeleteAsync(oldAccount);
                if (deleteResult.Succeeded)
                {
                    _logger.LogInformation(
                        "Removed a leftover duplicate SuperAdmin account at {OldEmail} that was never " +
                        "signed into and has no dependent records - {NewEmail} is the single SuperAdmin account.",
                        OldSuperAdminEmail, NewSuperAdminEmail);
                }
                else
                {
                    LogIdentityErrors($"remove the leftover duplicate account '{OldSuperAdminEmail}'", deleteResult);
                }

                await EnsureSuperAdminConfiguredAsync(newAccount);
                return;
            }

            _logger.LogWarning(
                "Both {OldEmail} and {NewEmail} exist as separate accounts, and {OldEmail} has signed " +
                "in before. Leaving it untouched and only ensuring {NewEmail} is a fully-configured " +
                "SuperAdmin. Resolve this conflict manually if it is unexpected.",
                OldSuperAdminEmail, NewSuperAdminEmail, OldSuperAdminEmail, NewSuperAdminEmail);

            await EnsureSuperAdminConfiguredAsync(newAccount);
            return;
        }

        var account = newAccount ?? oldAccount;

        if (account is null)
        {
            _logger.LogWarning(
                "Neither {OldEmail} nor {NewEmail} exist. The SuperAdmin account was not updated - " +
                "it will be created by the bootstrap seeder on the next run if configured.",
                OldSuperAdminEmail, NewSuperAdminEmail);
            return;
        }

        if (!string.Equals(account.Email, NewSuperAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(account, NewSuperAdminEmail);
            var userNameResult = await _userManager.SetUserNameAsync(account, NewSuperAdminEmail);

            if (!emailResult.Succeeded || !userNameResult.Succeeded)
            {
                LogIdentityErrors("update the SuperAdmin email/username", emailResult, userNameResult);
                return;
            }

            _logger.LogInformation(
                "SuperAdmin account renamed from {OldEmail} to {NewEmail}, user id preserved.",
                OldSuperAdminEmail, NewSuperAdminEmail);
        }

        await EnsureSuperAdminConfiguredAsync(account);
    }

    private async Task EnsureSuperAdminConfiguredAsync(ApplicationUser account)
    {
        var changed = false;

        if (!account.EmailConfirmed)
        {
            account.EmailConfirmed = true;
            changed = true;
        }

        if (!account.IsActive)
        {
            account.IsActive = true;
            changed = true;
        }

        if (changed)
        {
            await _userManager.UpdateAsync(account);
        }

        if (!await _userManager.IsInRoleAsync(account, AppRoles.SuperAdmin))
        {
            await _userManager.AddToRoleAsync(account, AppRoles.SuperAdmin);
        }

        // Reset unconditionally to the known demo password, per the local-Development
        // credential contract this seeder guarantees - never logged, never returned to a caller.
        // RemovePasswordAsync legitimately fails with no error to act on when the account has
        // no password set yet, so its result is intentionally not checked here.
        await _userManager.RemovePasswordAsync(account);

        var addResult = await _userManager.AddPasswordAsync(account, NewSuperAdminPassword);
        if (!addResult.Succeeded)
        {
            LogIdentityErrors("reset the SuperAdmin password", addResult);
        }

        _logger.LogInformation("SuperAdmin account {Email} verified and updated.", account.Email);
    }

    /// <summary>
    /// Never creates a second Patient. Finds the existing account, tops up role/active/email-
    /// confirmed flags if needed, and fills only genuinely empty, non-sensitive profile fields.
    /// </summary>
    private async Task<PatientProfile?> ValidateExistingPatientAsync(CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(ExistingPatientEmail);
        if (user is null)
        {
            _logger.LogWarning("Existing Patient account '{Email}' was not found.", ExistingPatientEmail);
            return null;
        }

        var changed = false;

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            changed = true;
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            changed = true;
        }

        if (changed)
        {
            await _userManager.UpdateAsync(user);
        }

        if (!await _userManager.IsInRoleAsync(user, AppRoles.Patient))
        {
            await _userManager.AddToRoleAsync(user, AppRoles.Patient);
        }

        // The existing login is left alone. Only if it has no password at all (so sign-in
        // could not possibly work) do we set the documented demo password.
        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (!hasPassword)
        {
            var addResult = await _userManager.AddPasswordAsync(user, ExistingPatientPassword);
            if (!addResult.Succeeded)
            {
                LogIdentityErrors("set a password for the existing Patient", addResult);
            }
            else
            {
                _logger.LogInformation("Existing Patient account had no password set; the demo password was applied.");
            }
        }

        var profile = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile is null)
        {
            profile = new PatientProfile { UserId = user.Id };
            _context.PatientProfiles.Add(profile);
            _logger.LogInformation("Created the missing PatientProfile for the existing Patient account.");
        }

        // Only fill genuinely empty fields - never overwrite anything already there.
        var profileChanged = false;

        if (profile.DateOfBirth is null)
        {
            profile.DateOfBirth = new DateOnly(1998, 5, 14);
            profileChanged = true;
        }

        if (string.IsNullOrWhiteSpace(profile.Gender))
        {
            profile.Gender = "Male";
            profileChanged = true;
        }

        if (string.IsNullOrWhiteSpace(profile.Address))
        {
            profile.Address = "Cairo, Egypt";
            profileChanged = true;
        }

        if (profileChanged || _context.Entry(profile).State == EntityState.Added)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Existing Patient account '{Email}' verified.", ExistingPatientEmail);

        return profile;
    }

    // ============================================================== Specialties

    /// <summary>Names required by this demo dataset that are not already covered by the bootstrap specialty seed.</summary>
    private static readonly (string Name, string ArabicName, string Description)[] RequiredSpecialties =
    [
        ("Cardiology", "أمراض القلب", "Heart and cardiovascular system conditions."),
        ("Internal Medicine", "الباطنة", "General internal medicine and adult primary care."),
        ("Pediatrics", "طب الأطفال", "Medical care for infants, children and adolescents."),
        ("Dermatology", "الأمراض الجلدية", "Skin, hair and nail conditions."),
        ("Orthopedics", "جراحة العظام", "Bones, joints, ligaments and musculoskeletal injuries."),
        ("Neurology", "المخ والأعصاب", "Brain, spinal cord and nervous system disorders."),
        ("Ophthalmology", "طب وجراحة العيون", "Eye examinations, vision care and eye surgery."),
        ("Dentistry", "طب الأسنان", "Oral health, teeth and gum treatment."),
        ("Obstetrics and Gynecology", "النساء والتوليد", "Pregnancy, childbirth and women's reproductive health."),
        // "Ear, Nose and Throat" is deliberately not seeded here: the bootstrap specialty
        // seed already has this exact specialty under the name "ENT" with the same Arabic
        // name, and the Arabic name has a unique index - "ENT" is reused below instead.
        ("General Surgery", "الجراحة العامة", "General surgical procedures and operative care."),
        ("Psychiatry", "الطب النفسي", "Mental health assessment and treatment.")
    ];

    private async Task SeedSpecialtiesAsync(CancellationToken ct)
    {
        var existingNames = await _context.Specialties.Select(s => s.Name).ToListAsync(ct);

        var missing = RequiredSpecialties
            .Where(s => !existingNames.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
            .Select(s => new Specialty
            {
                Name = s.Name,
                ArabicName = s.ArabicName,
                Description = s.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count > 0)
        {
            _context.Specialties.AddRange(missing);
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Specialties verified ({Existing} already present, {Created} created).",
            existingNames.Count, missing.Count);
    }

    private async Task<Guid?> GetSpecialtyIdAsync(string name, CancellationToken ct) =>
        await _context.Specialties
            .Where(s => s.Name == name)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

    // ================================================================= Hospitals

    private sealed record HospitalSeed(
        string Email, string Name, string PhoneNumber, string Address, string Governorate,
        string City, decimal Latitude, decimal Longitude, string NearbyLandmark, string LocationDescription);

    private static readonly HospitalSeed[] HospitalSeeds =
    [
        new("cairohospital@careconnect.local", "CareConnect Cairo Hospital", "0220000001",
            "Nasr City, Cairo", "Cairo", "Nasr City", 30.056100m, 31.330100m,
            "Near City Stars", "Main hospital building in Nasr City"),
        new("shubrahospital@careconnect.local", "Shubra Medical Center", "0220000002",
            "Shubra El-Kheima, Qalyubia", "Qalyubia", "Shubra El-Kheima", 30.128600m, 31.242200m,
            "Near Shubra El-Kheima Metro Station", "Multi-specialty medical center"),
        new("gizahospital@careconnect.local", "Giza Specialized Hospital", "0220000003",
            "Dokki, Giza", "Giza", "Dokki", 30.038400m, 31.212200m,
            "Near Dokki Metro Station", "Specialized healthcare facility in Dokki"),
        new("alexhospital@careconnect.local", "Alexandria Health Hospital", "0320000004",
            "Smouha, Alexandria", "Alexandria", "Smouha", 31.215600m, 29.955300m,
            "Near Smouha Sporting Club", "General Hospital serving Alexandria"),
        new("maadihospital@careconnect.local", "Maadi Family Hospital", "0220000005",
            "Maadi, Cairo", "Cairo", "Maadi", 29.960200m, 31.256900m,
            "Near Maadi Metro Station", "Family-focused Hospital in Maadi"),
        new("tantahospital@careconnect.local", "Tanta Medical Hospital", "0402000006",
            "Tanta, Gharbia", "Gharbia", "Tanta", 30.786500m, 31.000400m,
            "Near Tanta University", "Regional Hospital in Tanta")
    ];

    private async Task<Dictionary<string, HospitalProfile>> SeedHospitalUsersAndProfilesAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, HospitalProfile>(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var seed in HospitalSeeds)
        {
            var user = await CreateOrGetUserAsync(seed.Email, seed.Name, seed.PhoneNumber, AppRoles.Hospital, ct);
            if (user is null)
            {
                continue;
            }

            var profile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == user.Id, ct);
            if (profile is null)
            {
                profile = new HospitalProfile
                {
                    UserId = user.Id,
                    HospitalName = seed.Name,
                    Address = seed.Address,
                    Governorate = seed.Governorate,
                    City = seed.City,
                    Latitude = seed.Latitude,
                    Longitude = seed.Longitude,
                    LocationDescription = seed.LocationDescription,
                    NearbyLandmark = seed.NearbyLandmark,
                    PhoneNumber = seed.PhoneNumber,
                    Description = $"{seed.Name} is a full-service hospital located in {seed.City}, " +
                                  $"{seed.Governorate}, offering comprehensive medical care to the local community.",
                    IsProfileCompleted = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.HospitalProfiles.Add(profile);
                created++;
            }

            result[seed.Name] = profile;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Hospital accounts and profiles verified ({Total} total, {Created} created).",
            HospitalSeeds.Length, created);

        return result;
    }

    // =================================================================== Doctors

    private sealed record DoctorSeed(
        string Email, string FullName, string PhoneNumber, string SpecialtyName, string LicenseNumber,
        int YearsOfExperience, decimal ConsultationFee, string Biography, string Governorate, string City,
        string[] HospitalNames);

    private static readonly DoctorSeed[] DoctorSeeds =
    [
        new("doctor.cardiology@careconnect.local", "Mahmoud Ibrahim", "01050000001", "Cardiology",
            "CC-DOC-1001", 12, 500m, "Consultant cardiologist with experience in adult cardiac care.",
            "Cairo", "Nasr City", ["CareConnect Cairo Hospital", "Tanta Medical Hospital"]),
        new("doctor.internal@careconnect.local", "Youssef Ahmed", "01050000002", "Internal Medicine",
            "CC-DOC-1002", 9, 350m, "Internal medicine specialist.",
            "Cairo", "Nasr City", ["CareConnect Cairo Hospital", "Maadi Family Hospital"]),
        new("doctor.pediatrics@careconnect.local", "Nada Mostafa", "01050000003", "Pediatrics",
            "CC-DOC-1003", 8, 400m, "Pediatric specialist focused on child healthcare.",
            "Qalyubia", "Shubra El-Kheima", ["Shubra Medical Center", "Maadi Family Hospital", "Alexandria Health Hospital"]),
        new("doctor.orthopedics@careconnect.local", "Karim Samir", "01050000004", "Orthopedics",
            "CC-DOC-1004", 14, 550m, "Orthopedic and joint specialist.",
            "Giza", "Dokki", ["Giza Specialized Hospital", "Tanta Medical Hospital"]),
        new("doctor.dermatology@careconnect.local", "Salma Hany", "01050000005", "Dermatology",
            "CC-DOC-1005", 7, 450m, "Dermatology and skin-care specialist.",
            "Qalyubia", "Shubra El-Kheima", ["Shubra Medical Center", "Maadi Family Hospital"]),
        new("doctor.neurology@careconnect.local", "Amr Khaled", "01050000006", "Neurology",
            "CC-DOC-1006", 11, 600m, "Neurologist specializing in adult neurological conditions.",
            "Cairo", "Nasr City", ["CareConnect Cairo Hospital", "Giza Specialized Hospital"]),
        new("doctor.dentistry@careconnect.local", "Rana Adel", "01050000007", "Dentistry",
            "CC-DOC-1007", 6, 300m, "General dentist and oral-care specialist.",
            "Qalyubia", "Shubra El-Kheima", ["Shubra Medical Center"]),
        new("doctor.gynecology@careconnect.local", "Hala Hassan", "01050000008", "Obstetrics and Gynecology",
            "CC-DOC-1008", 13, 550m, "Obstetrics and gynecology consultant.",
            "Alexandria", "Smouha", ["Alexandria Health Hospital", "Tanta Medical Hospital"])
    ];

    private async Task<Dictionary<string, DoctorProfile>> SeedDoctorUsersAndProfilesAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, DoctorProfile>(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var seed in DoctorSeeds)
        {
            var user = await CreateOrGetUserAsync(seed.Email, seed.FullName, seed.PhoneNumber, AppRoles.Doctor, ct);
            if (user is null)
            {
                continue;
            }

            var profile = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
            if (profile is null)
            {
                var specialtyId = await GetSpecialtyIdAsync(seed.SpecialtyName, ct);

                profile = new DoctorProfile
                {
                    UserId = user.Id,
                    SpecialtyId = specialtyId,
                    LicenseNumber = seed.LicenseNumber,
                    YearsOfExperience = seed.YearsOfExperience,
                    ConsultationPrice = seed.ConsultationFee,
                    Biography = seed.Biography,
                    Governorate = seed.Governorate,
                    City = seed.City,
                    IsProfileCompleted = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DoctorProfiles.Add(profile);
                created++;
            }

            result[seed.FullName] = profile;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Doctor accounts and profiles verified ({Total} total, {Created} created).",
            DoctorSeeds.Length, created);

        return result;
    }

    // ==================================================== MedicalServiceProvider

    private async Task SeedMedicalServiceProviderAsync(CancellationToken ct)
    {
        const string email = "provider1@careconnect.local";
        const string name = "CareConnect Medical Services";
        const string phoneNumber = "01060000001";

        var user = await CreateOrGetUserAsync(email, name, phoneNumber, AppRoles.MedicalServiceProvider, ct);
        if (user is null)
        {
            return;
        }

        var profile = await _context.MedicalServiceProviderProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile is not null)
        {
            _logger.LogInformation("MedicalServiceProvider account already present.");
            return;
        }

        profile = new MedicalServiceProviderProfile
        {
            UserId = user.Id,
            ProviderName = name,
            ServiceType = "General medical services",
            Address = "Nasr City, Cairo",
            Governorate = "Cairo",
            City = "Nasr City",
            Description = "Demo medical services provider for local development."
        };

        _context.MedicalServiceProviderProfiles.Add(profile);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("MedicalServiceProvider account created.");
    }

    // ========================================================= Hospital <-> user helper

    private async Task<ApplicationUser?> CreateOrGetUserAsync(
        string email, string fullName, string phoneNumber, string role, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            var changed = false;

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                changed = true;
            }

            if (changed)
            {
                await _userManager.UpdateAsync(user);
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = true,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, DemoPassword);
        if (!createResult.Succeeded)
        {
            LogIdentityErrors($"create demo account '{email}'", createResult);
            return null;
        }

        await _userManager.AddToRoleAsync(user, role);
        return user;
    }

    // ========================================================= HospitalSpecialties

    private static readonly Dictionary<string, string[]> HospitalSpecialtyLinks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CareConnect Cairo Hospital"] = ["Cardiology", "Internal Medicine", "Neurology", "General Surgery", "Pediatrics"],
        ["Shubra Medical Center"] = ["Internal Medicine", "Pediatrics", "Dentistry", "Dermatology"],
        ["Giza Specialized Hospital"] = ["Orthopedics", "Neurology", "Ophthalmology", "General Surgery"],
        // "ENT" is the bootstrap seed's existing name for Ear, Nose and Throat - see the note in SeedSpecialtiesAsync.
        ["Alexandria Health Hospital"] = ["Cardiology", "Pediatrics", "Obstetrics and Gynecology", "ENT"],
        ["Maadi Family Hospital"] = ["Internal Medicine", "Pediatrics", "Dermatology", "Psychiatry"],
        ["Tanta Medical Hospital"] = ["General Surgery", "Orthopedics", "Cardiology", "Obstetrics and Gynecology"]
    };

    private async Task SeedHospitalSpecialtiesAsync(Dictionary<string, HospitalProfile> hospitals, CancellationToken ct)
    {
        var existingPairs = await _context.HospitalSpecialties
            .Select(hs => new { hs.HospitalProfileId, hs.SpecialtyId })
            .ToListAsync(ct);
        var existingSet = existingPairs.Select(p => (p.HospitalProfileId, p.SpecialtyId)).ToHashSet();

        var created = 0;

        foreach (var (hospitalName, specialtyNames) in HospitalSpecialtyLinks)
        {
            if (!hospitals.TryGetValue(hospitalName, out var hospital))
            {
                continue;
            }

            foreach (var specialtyName in specialtyNames)
            {
                var specialtyId = await GetSpecialtyIdAsync(specialtyName, ct);
                if (specialtyId is null || existingSet.Contains((hospital.Id, specialtyId.Value)))
                {
                    continue;
                }

                _context.HospitalSpecialties.Add(new HospitalSpecialty
                {
                    HospitalProfileId = hospital.Id,
                    SpecialtyId = specialtyId.Value,
                    CreatedAt = DateTime.UtcNow
                });
                existingSet.Add((hospital.Id, specialtyId.Value));
                created++;
            }
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Hospital specialties verified ({Created} created).", created);
    }

    // ==================================================== DoctorHospitalAffiliations

    private async Task SeedDoctorAffiliationsAsync(
        Dictionary<string, DoctorProfile> doctors, Dictionary<string, HospitalProfile> hospitals, CancellationToken ct)
    {
        var existingPairs = await _context.DoctorHospitalAffiliations
            .Select(a => new { a.DoctorProfileId, a.HospitalProfileId })
            .ToListAsync(ct);
        var existingSet = existingPairs.Select(p => (p.DoctorProfileId, p.HospitalProfileId)).ToHashSet();

        var created = 0;

        foreach (var seed in DoctorSeeds)
        {
            if (!doctors.TryGetValue(seed.FullName, out var doctor))
            {
                continue;
            }

            foreach (var hospitalName in seed.HospitalNames)
            {
                if (!hospitals.TryGetValue(hospitalName, out var hospital))
                {
                    continue;
                }

                if (existingSet.Contains((doctor.Id, hospital.Id)))
                {
                    continue;
                }

                _context.DoctorHospitalAffiliations.Add(new DoctorHospitalAffiliation
                {
                    DoctorProfileId = doctor.Id,
                    HospitalProfileId = hospital.Id,
                    Status = AffiliationStatus.Approved,
                    RequestedAt = DateTime.UtcNow,
                    ReviewedAt = DateTime.UtcNow,
                    IsPrimary = hospitalName == seed.HospitalNames[0],
                    CreatedAt = DateTime.UtcNow
                });
                existingSet.Add((doctor.Id, hospital.Id));
                created++;
            }
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Doctor-hospital affiliations verified ({Created} created).", created);
    }

    // ========================================================== DoctorAvailability

    private static readonly (DayOfWeek Day, TimeOnly Start, TimeOnly End)[] WeeklyPattern =
    [
        (DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
        (DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0)),
        (DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
        (DayOfWeek.Wednesday, new TimeOnly(14, 0), new TimeOnly(18, 0)),
        (DayOfWeek.Thursday, new TimeOnly(10, 0), new TimeOnly(14, 0))
    ];

    private async Task SeedDoctorAvailabilityAsync(
        Dictionary<string, DoctorProfile> doctors, Dictionary<string, HospitalProfile> hospitals, CancellationToken ct)
    {
        var existing = await _context.DoctorAvailabilities
            .Select(a => new { a.DoctorProfileId, a.HospitalProfileId, a.DayOfWeek, a.StartTime, a.EndTime })
            .ToListAsync(ct);
        var existingSet = existing
            .Select(a => (a.DoctorProfileId, a.HospitalProfileId, a.DayOfWeek, a.StartTime, a.EndTime))
            .ToHashSet();

        var created = 0;

        foreach (var seed in DoctorSeeds)
        {
            if (!doctors.TryGetValue(seed.FullName, out var doctor))
            {
                continue;
            }

            foreach (var hospitalName in seed.HospitalNames)
            {
                if (!hospitals.TryGetValue(hospitalName, out var hospital))
                {
                    continue;
                }

                foreach (var (day, start, end) in WeeklyPattern)
                {
                    if (existingSet.Contains((doctor.Id, hospital.Id, day, start, end)))
                    {
                        continue;
                    }

                    _context.DoctorAvailabilities.Add(new DoctorAvailability
                    {
                        DoctorProfileId = doctor.Id,
                        HospitalProfileId = hospital.Id,
                        DayOfWeek = day,
                        StartTime = start,
                        EndTime = end,
                        SlotDurationMinutes = 30,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    existingSet.Add((doctor.Id, hospital.Id, day, start, end));
                    created++;
                }
            }
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Doctor weekly availability verified ({Created} created).", created);
    }

    // ========================================================= InsuranceCompanies

    private async Task SeedInsuranceCompaniesAsync(CancellationToken ct)
    {
        // Already fully covered by DatabaseSeeder's own idempotent insurance-company seed
        // (same Name-based matching), which always runs before this seeder. Nothing to add
        // here - just confirm the expected companies are present.
        var requiredNames = new[]
        {
            "Misr Insurance", "AXA Egypt", "Allianz Egypt", "MetLife Egypt", "Med Right", "GlobeMed Egypt"
        };

        var existingNames = await _context.InsuranceCompanies.Select(c => c.Name).ToListAsync(ct);
        var missing = requiredNames.Where(n => !existingNames.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();

        if (missing.Count > 0)
        {
            _logger.LogWarning(
                "Expected insurance companies missing after bootstrap seeding: {Missing}.",
                string.Join(", ", missing));
        }
        else
        {
            _logger.LogInformation("Insurance companies verified (already present).");
        }
    }

    private async Task<Guid?> GetInsuranceCompanyIdAsync(string name, CancellationToken ct) =>
        await _context.InsuranceCompanies
            .Where(c => c.Name == name)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);

    // =============================================================== BloodStock

    private static readonly (BloodGroup Group, int AvailableUnits, int MinimumRequiredUnits)[] BaseBloodStock =
    [
        (BloodGroup.APositive, 18, 8),
        (BloodGroup.ANegative, 4, 5),
        (BloodGroup.BPositive, 12, 6),
        (BloodGroup.BNegative, 2, 4),
        (BloodGroup.ABPositive, 7, 4),
        (BloodGroup.ABNegative, 0, 3),
        (BloodGroup.OPositive, 20, 10),
        (BloodGroup.ONegative, 3, 6)
    ];

    private async Task SeedBloodStockAsync(Dictionary<string, HospitalProfile> hospitals, CancellationToken ct)
    {
        var existingPairs = await _context.BloodStocks
            .Select(s => new { s.HospitalProfileId, s.BloodGroup })
            .ToListAsync(ct);
        var existingSet = existingPairs.Select(p => (p.HospitalProfileId, p.BloodGroup)).ToHashSet();

        var created = 0;
        var hospitalIndex = 0;

        foreach (var hospital in hospitals.Values)
        {
            // A small, deterministic per-hospital offset so quantities vary slightly without
            // needing any randomness (which would break idempotent re-runs).
            var offset = hospitalIndex % 4;

            foreach (var (group, availableUnits, minimumUnits) in BaseBloodStock)
            {
                if (existingSet.Contains((hospital.Id, group)))
                {
                    continue;
                }

                var adjustedAvailable = Math.Max(0, availableUnits + offset - 1);

                _context.BloodStocks.Add(new BloodStock
                {
                    HospitalProfileId = hospital.Id,
                    BloodGroup = group,
                    AvailableUnits = adjustedAvailable,
                    MinimumRequiredUnits = minimumUnits,
                    IsAvailable = adjustedAvailable > 0,
                    LastUpdatedByUserId = hospital.UserId,
                    CreatedAt = DateTime.UtcNow
                });
                existingSet.Add((hospital.Id, group));
                created++;
            }

            hospitalIndex++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Blood stock verified ({Created} records created).", created);
    }

    // =============================================================== Appointments

    private sealed record AppointmentSeed(
        string Marker, string DoctorFullName, string HospitalName, int DayOffset, TimeOnly StartTime,
        AppointmentStatus Status);

    private static readonly AppointmentSeed[] AppointmentSeeds =
    [
        new("Demo: future pending cardiology follow-up", "Mahmoud Ibrahim", "CareConnect Cairo Hospital",
            10, new TimeOnly(9, 0), AppointmentStatus.Pending),
        new("Demo: future confirmed internal medicine check-up", "Youssef Ahmed", "CareConnect Cairo Hospital",
            3, new TimeOnly(14, 0), AppointmentStatus.Confirmed),
        new("Demo: pediatric visit scheduled for tomorrow", "Nada Mostafa", "Maadi Family Hospital",
            1, new TimeOnly(10, 0), AppointmentStatus.Confirmed),
        new("Demo: orthopedic consultation within the next week", "Karim Samir", "Giza Specialized Hospital",
            5, new TimeOnly(10, 0), AppointmentStatus.Pending),
        new("Demo: completed dermatology visit from last week", "Salma Hany", "Shubra Medical Center",
            -7, new TimeOnly(14, 0), AppointmentStatus.Completed),
        new("Demo: cancelled neurology appointment", "Amr Khaled", "CareConnect Cairo Hospital",
            8, new TimeOnly(9, 0), AppointmentStatus.Cancelled),
        new("Demo: rejected dental appointment request", "Rana Adel", "Shubra Medical Center",
            6, new TimeOnly(9, 0), AppointmentStatus.Rejected),
        new("Demo: no-show gynecology appointment", "Hala Hassan", "Alexandria Health Hospital",
            -3, new TimeOnly(10, 0), AppointmentStatus.NoShow)
    ];

    /// <summary>
    /// The Reason field doubles as the stable natural key: dates are calculated relative to
    /// "today" only the first time a given marker is seeded, so re-running the app on a
    /// different calendar day never creates a second copy of the same demo appointment.
    /// </summary>
    private async Task<Dictionary<string, Appointment>> SeedAppointmentsAsync(
        PatientProfile patient,
        Dictionary<string, DoctorProfile> doctors,
        Dictionary<string, HospitalProfile> hospitals,
        CancellationToken ct)
    {
        var result = new Dictionary<string, Appointment>();

        var existing = await _context.Appointments
            .Where(a => a.PatientProfileId == patient.Id && a.Reason != null)
            .ToListAsync(ct);

        foreach (var appointment in existing)
        {
            if (appointment.Reason is not null)
            {
                result[appointment.Reason] = appointment;
            }
        }

        var created = 0;
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        foreach (var seed in AppointmentSeeds)
        {
            if (result.ContainsKey(seed.Marker))
            {
                continue;
            }

            if (!doctors.TryGetValue(seed.DoctorFullName, out var doctor) ||
                !hospitals.TryGetValue(seed.HospitalName, out var hospital))
            {
                continue;
            }

            var date = today.AddDays(seed.DayOffset);
            var endTime = seed.StartTime.AddMinutes(30);

            var appointment = new Appointment
            {
                PatientProfileId = patient.Id,
                DoctorProfileId = doctor.Id,
                HospitalProfileId = hospital.Id,
                AppointmentDate = date,
                StartTime = seed.StartTime,
                EndTime = endTime,
                Reason = seed.Marker,
                Status = seed.Status,
                CreatedAt = now
            };

            switch (seed.Status)
            {
                case AppointmentStatus.Confirmed:
                    appointment.ConfirmedAt = now;
                    break;
                case AppointmentStatus.Completed:
                    appointment.ConfirmedAt = now.AddDays(seed.DayOffset - 1);
                    appointment.CompletedAt = now.AddDays(seed.DayOffset);
                    break;
                case AppointmentStatus.Cancelled:
                    appointment.CancellationReason = "Demo data: patient requested a reschedule.";
                    appointment.CancelledAt = now;
                    appointment.CancelledByUserId = patient.UserId;
                    break;
                case AppointmentStatus.Rejected:
                    appointment.RejectionReason = "Demo data: doctor unavailable at the requested time.";
                    appointment.RejectedAt = now;
                    break;
                case AppointmentStatus.NoShow:
                    appointment.ConfirmedAt = now.AddDays(seed.DayOffset - 1);
                    break;
            }

            _context.Appointments.Add(appointment);
            result[seed.Marker] = appointment;
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Appointments verified ({Created} created) for the existing demo Patient.", created);

        return result;
    }

    // ========================================================== InsuranceRequests

    private sealed record InsuranceRequestSeed(
        string MemberNumber, string? PolicyNumber, string AppointmentMarker, string CompanyName,
        string ServiceDescription, decimal RequestedAmount, InsuranceRequestStatus Status,
        decimal? ApprovedAmount = null, string? ApprovalReferenceNumber = null, string? RejectionReason = null);

    private static readonly InsuranceRequestSeed[] InsuranceRequestSeeds =
    [
        new("DEMO-MEMBER-1001", "DEMO-POLICY-1001", "Demo: future pending cardiology follow-up",
            "Misr Insurance", "Cardiology consultation", 500m, InsuranceRequestStatus.Pending),
        new("DEMO-MEMBER-1002", "DEMO-POLICY-1002", "Demo: pediatric visit scheduled for tomorrow",
            "AXA Egypt", "Pediatric consultation", 400m, InsuranceRequestStatus.Approved,
            ApprovedAmount: 350m, ApprovalReferenceNumber: "APPROVED-DEMO-1002"),
        new("DEMO-MEMBER-1003", "DEMO-POLICY-1003", "Demo: orthopedic consultation within the next week",
            "Allianz Egypt", "Orthopedic consultation", 550m, InsuranceRequestStatus.Rejected,
            RejectionReason: "Demo policy does not cover the requested service.")
    ];

    private async Task SeedInsuranceRequestsAsync(
        PatientProfile patient,
        Dictionary<string, Appointment> appointments,
        Dictionary<string, HospitalProfile> hospitals,
        CancellationToken ct)
    {
        var existingMemberNumbers = await _context.InsuranceRequests
            .Where(r => r.PatientProfileId == patient.Id)
            .Select(r => r.MemberNumber)
            .ToListAsync(ct);

        var created = 0;
        var now = DateTime.UtcNow;

        foreach (var seed in InsuranceRequestSeeds)
        {
            if (existingMemberNumbers.Contains(seed.MemberNumber))
            {
                continue;
            }

            if (!appointments.TryGetValue(seed.AppointmentMarker, out var appointment))
            {
                continue;
            }

            var companyId = await GetInsuranceCompanyIdAsync(seed.CompanyName, ct);
            if (companyId is null)
            {
                continue;
            }

            var request = new InsuranceRequest
            {
                PatientProfileId = patient.Id,
                HospitalProfileId = appointment.HospitalProfileId,
                AppointmentId = appointment.Id,
                InsuranceCompanyId = companyId.Value,
                MemberNumber = seed.MemberNumber,
                PolicyNumber = seed.PolicyNumber,
                ServiceDescription = seed.ServiceDescription,
                RequestedAmount = seed.RequestedAmount,
                Status = seed.Status,
                SubmittedAt = now,
                CreatedAt = now
            };

            switch (seed.Status)
            {
                case InsuranceRequestStatus.Approved:
                    request.ApprovedAmount = seed.ApprovedAmount;
                    request.ApprovalReferenceNumber = seed.ApprovalReferenceNumber;
                    request.ReviewedAt = now;
                    request.ApprovedAt = now;
                    break;
                case InsuranceRequestStatus.Rejected:
                    request.RejectionReason = seed.RejectionReason;
                    request.ReviewedAt = now;
                    request.RejectedAt = now;
                    break;
            }

            _context.InsuranceRequests.Add(request);
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Insurance requests verified ({Created} created) for the existing demo Patient.", created);
    }

    // ============================================================== BloodRequests

    private sealed record BloodRequestSeed(
        string HospitalName, BloodGroup Group, string BeneficiaryName, int BeneficiaryAge,
        string ContactPhoneNumber, int UnitsRequested, BloodRequestUrgency Urgency, string? MedicalCondition,
        string? RequestNotes, BloodRequestStatus Status, string? HospitalNotes = null, string? RejectionReason = null);

    private static readonly BloodRequestSeed[] BloodRequestSeeds =
    [
        new("CareConnect Cairo Hospital", BloodGroup.OPositive, "Mona Hassan", 45, "01099999901", 2,
            BloodRequestUrgency.Normal, "Scheduled minor surgery", "Demo data: routine pre-surgery reserve.",
            BloodRequestStatus.Pending),
        new("Maadi Family Hospital", BloodGroup.APositive, "Tarek Fathy", 60, "01099999902", 3,
            BloodRequestUrgency.Urgent, "Post-operative recovery", "Demo data: approved for immediate use.",
            BloodRequestStatus.Approved, HospitalNotes: "Confirmed availability, prepared for pickup."),
        new("Giza Specialized Hospital", BloodGroup.ABNegative, "Laila Nabil", 30, "01099999903", 1,
            BloodRequestUrgency.Emergency, "Emergency trauma case", "Demo data: emergency demo request.",
            BloodRequestStatus.Rejected, RejectionReason: "Demo policy: insufficient verified medical justification."),
        new("Shubra Medical Center", BloodGroup.BPositive, "Hossam Adly", 52, "01099999904", 2,
            BloodRequestUrgency.Normal, "Scheduled transfusion", "Demo data: fulfilled demo request.",
            BloodRequestStatus.Fulfilled, HospitalNotes: "Units handed off to the care team."),
        new("Alexandria Health Hospital", BloodGroup.ONegative, "Dina Samy", 27, "01099999905", 1,
            BloodRequestUrgency.Urgent, "Postpartum precaution", "Demo data: cancelled by patient.",
            BloodRequestStatus.Cancelled)
    ];

    private async Task SeedBloodRequestsAsync(
        PatientProfile patient, Dictionary<string, HospitalProfile> hospitals, CancellationToken ct)
    {
        var existing = await _context.BloodRequests
            .Where(r => r.PatientProfileId == patient.Id)
            .Select(r => new { r.HospitalProfileId, r.BloodGroup, r.BeneficiaryName })
            .ToListAsync(ct);
        var existingSet = existing
            .Select(e => (e.HospitalProfileId, e.BloodGroup, e.BeneficiaryName))
            .ToHashSet();

        var created = 0;
        var now = DateTime.UtcNow;

        foreach (var seed in BloodRequestSeeds)
        {
            if (!hospitals.TryGetValue(seed.HospitalName, out var hospital))
            {
                continue;
            }

            if (existingSet.Contains((hospital.Id, seed.Group, seed.BeneficiaryName)))
            {
                continue;
            }

            var stock = await _context.BloodStocks.FirstOrDefaultAsync(
                s => s.HospitalProfileId == hospital.Id && s.BloodGroup == seed.Group, ct);

            if (stock is null)
            {
                continue;
            }

            // The seeded stock quantities already represent the resting state after any demo
            // allocation, so approving/fulfilling a demo request here never touches BloodStock -
            // that would silently subtract units again on every restart.
            var request = new BloodRequest
            {
                PatientProfileId = patient.Id,
                HospitalProfileId = hospital.Id,
                BloodStockId = stock.Id,
                BloodGroup = seed.Group,
                UnitsRequested = seed.UnitsRequested,
                BeneficiaryName = seed.BeneficiaryName,
                BeneficiaryAge = seed.BeneficiaryAge,
                ContactPhoneNumber = seed.ContactPhoneNumber,
                MedicalCondition = seed.MedicalCondition,
                RequestNotes = seed.RequestNotes,
                HospitalNotes = seed.HospitalNotes,
                Urgency = seed.Urgency,
                Status = seed.Status,
                SubmittedAt = now,
                CreatedAt = now
            };

            switch (seed.Status)
            {
                case BloodRequestStatus.Approved:
                    request.ApprovedAt = now;
                    break;
                case BloodRequestStatus.Rejected:
                    request.RejectionReason = seed.RejectionReason;
                    request.RejectedAt = now;
                    break;
                case BloodRequestStatus.Fulfilled:
                    request.ApprovedAt = now.AddHours(-2);
                    request.FulfilledAt = now;
                    break;
                case BloodRequestStatus.Cancelled:
                    request.CancelledAt = now;
                    break;
            }

            _context.BloodRequests.Add(request);
            existingSet.Add((hospital.Id, seed.Group, seed.BeneficiaryName));
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Blood requests verified ({Created} created) for the existing demo Patient.", created);
    }

    // ==================================================================== Helpers

    private void LogIdentityErrors(string action, params IdentityResult[] results)
    {
        var errors = results
            .SelectMany(r => r.Errors)
            .Select(e => e.Description)
            .ToList();

        if (errors.Count > 0)
        {
            _logger.LogError("Failed to {Action}: {Errors}", action, string.Join("; ", errors));
        }
    }
}
