using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Infrastructure.Persistence.Seed;
using CareConnect.Infrastructure.Services;
using CareConnect.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SuperAdminSettings>(configuration.GetSection(SuperAdminSettings.SectionName));
        services.Configure<DemoDataOptions>(configuration.GetSection(DemoDataOptions.SectionName));
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .Validate(options => options.MaxOutputTokens is >= 100 and <= 2_000,
                "Gemini:MaxOutputTokens must be between 100 and 2000.")
            .Validate(options => options.TimeoutSeconds is >= 5 and <= 120,
                "Gemini:TimeoutSeconds must be between 5 and 120.")
            .Validate(options => options.MaximumHistoryMessages is >= 0 and <= 10,
                "Gemini:MaximumHistoryMessages must be between 0 and 10.")
            .Validate(options => options.MaximumMessageCharacters is >= 1 and <= 2_000,
                "Gemini:MaximumMessageCharacters must be between 1 and 2000.");

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in configuration.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentityServices();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IDoctorProfileService, DoctorProfileService>();
        services.AddScoped<IHospitalProfileService, HospitalProfileService>();
        services.AddScoped<IDoctorHospitalAffiliationService, DoctorHospitalAffiliationService>();
        services.AddScoped<IHealthcareDirectoryService, HealthcareDirectoryService>();
        services.AddScoped<IGeoDistanceService, GeoDistanceService>();
        services.AddScoped<IHospitalDiscoveryService, HospitalDiscoveryService>();
        services.AddScoped<IMedicalServiceCategoryService, MedicalServiceCategoryService>();
        services.AddScoped<IMedicalServiceProviderProfileService, MedicalServiceProviderProfileService>();
        services.AddScoped<IMedicalServiceProviderDirectoryService, MedicalServiceProviderDirectoryService>();
        services.AddScoped<IMedicalServiceRequestService, MedicalServiceRequestService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<IReviewService>(sp => sp.GetRequiredService<ReviewService>());
        services.AddScoped<IRatingQueryService>(sp => sp.GetRequiredService<ReviewService>());
        services.AddScoped<IReviewModerationService>(sp => sp.GetRequiredService<ReviewService>());

        services.AddScoped<IDoctorAvailabilityService, DoctorAvailabilityService>();
        services.AddScoped<IDoctorUnavailablePeriodService, DoctorUnavailablePeriodService>();
        services.AddScoped<IAvailableSlotService, AvailableSlotService>();
        services.AddScoped<IAppointmentService, AppointmentService>();

        services.AddScoped<IInsuranceCompanyService, InsuranceCompanyService>();
        services.AddScoped<IInsuranceRequestService, InsuranceRequestService>();

        services.AddScoped<IBloodStockService, BloodStockService>();
        services.AddScoped<IBloodAvailabilityService, BloodAvailabilityService>();
        services.AddScoped<IBloodRequestService, BloodRequestService>();
        services.AddHttpClient<IAiMedicalAssistantService, GeminiMedicalAssistantService>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        });

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

        return services;
    }

    /// <summary>
    /// AddIdentityCore rather than AddIdentity: this is a token API, so we do not want the
    /// cookie handlers that would turn a rejected request into a redirect to a login page.
    /// </summary>
    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;

            options.User.RequireUniqueEmail = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        // No AddDefaultTokenProviders: nothing in this step issues email-confirmation or
        // password-reset tokens, and it would pull the full ASP.NET Identity UI stack in here.

        return services;
    }
}
