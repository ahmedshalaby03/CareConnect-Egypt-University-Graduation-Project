using CareConnect.Domain.Constants;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareConnect.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates the five roles and the single SuperAdmin account. Idempotent, so it is safe to
/// run on every start. It never applies migrations - the database schema is the developer's
/// call, on purpose.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SuperAdminSettings _superAdmin;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<SuperAdminSettings> superAdmin,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _superAdmin = superAdmin.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!await _context.Database.CanConnectAsync(ct))
        {
            _logger.LogWarning(
                "Skipping seeding: cannot connect to the database. Run 'dotnet ef database update' first.");
            return;
        }

        await SeedRolesAsync();
        await SeedSuperAdminAsync();
        await SeedSpecialtiesAsync(ct);
        await SeedInsuranceCompaniesAsync(ct);
    }

    /// <summary>
    /// Adds any seed insurance company that is not present yet, matching on name. Existing
    /// rows are left exactly as they are, so an administrator's edits survive a restart.
    /// </summary>
    private async Task SeedInsuranceCompaniesAsync(CancellationToken ct)
    {
        var existingNames = await _context.InsuranceCompanies
            .Select(c => c.Name)
            .ToListAsync(ct);

        var missing = InsuranceCompanySeedData.Items
            .Where(seed => !existingNames.Contains(seed.Name, StringComparer.OrdinalIgnoreCase))
            .Select(seed => new InsuranceCompany
            {
                Name = seed.Name,
                ArabicName = seed.ArabicName,
                Description = seed.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        _context.InsuranceCompanies.AddRange(missing);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Seeded {Count} insurance companies.", missing.Count);
    }

    /// <summary>
    /// Adds any seed specialty that is not present yet, matching on name. Existing rows are
    /// left exactly as they are, so an administrator's edits survive a restart.
    /// </summary>
    private async Task SeedSpecialtiesAsync(CancellationToken ct)
    {
        var existingNames = await _context.Specialties
            .Select(s => s.Name)
            .ToListAsync(ct);

        var missing = SpecialtySeedData.Items
            .Where(seed => !existingNames.Contains(seed.Name, StringComparer.OrdinalIgnoreCase))
            .Select(seed => new Specialty
            {
                Name = seed.Name,
                ArabicName = seed.ArabicName,
                Description = seed.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        _context.Specialties.AddRange(missing);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Seeded {Count} medical specialties.", missing.Count);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in AppRoles.All)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new ApplicationRole(roleName));
            if (result.Succeeded)
            {
                _logger.LogInformation("Seeded role {Role}.", roleName);
            }
            else
            {
                _logger.LogError(
                    "Failed to seed role {Role}: {Errors}",
                    roleName,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedSuperAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(_superAdmin.Email) || string.IsNullOrWhiteSpace(_superAdmin.Password))
        {
            _logger.LogWarning(
                "Skipping SuperAdmin seeding: the SuperAdmin:Email / SuperAdmin:Password " +
                "configuration values are not set.");
            return;
        }

        var existing = await _userManager.FindByEmailAsync(_superAdmin.Email);
        if (existing is not null)
        {
            // The account is already there. We top up the role assignment if it somehow went
            // missing, but we never touch an existing password.
            if (!await _userManager.IsInRoleAsync(existing, AppRoles.SuperAdmin))
            {
                await _userManager.AddToRoleAsync(existing, AppRoles.SuperAdmin);
                _logger.LogInformation("Restored the SuperAdmin role on {Email}.", _superAdmin.Email);
            }

            return;
        }

        var admin = new ApplicationUser
        {
            UserName = _superAdmin.Email,
            Email = _superAdmin.Email,
            FullName = string.IsNullOrWhiteSpace(_superAdmin.FullName)
                ? "CareConnect Administrator"
                : _superAdmin.FullName,
            PhoneNumber = string.IsNullOrWhiteSpace(_superAdmin.PhoneNumber) ? null : _superAdmin.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(admin, _superAdmin.Password);
        if (!createResult.Succeeded)
        {
            _logger.LogError(
                "Failed to seed the SuperAdmin account: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(admin, AppRoles.SuperAdmin);

        _logger.LogInformation("Seeded the SuperAdmin account {Email}.", _superAdmin.Email);
        _logger.LogWarning(
            "Change the seeded SuperAdmin password before deploying this application anywhere public.");
    }
}
