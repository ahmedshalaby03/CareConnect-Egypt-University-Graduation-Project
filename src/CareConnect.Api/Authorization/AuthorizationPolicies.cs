using CareConnect.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace CareConnect.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string SuperAdminOnly = nameof(SuperAdminOnly);
    public const string PatientOnly = nameof(PatientOnly);
    public const string DoctorOnly = nameof(DoctorOnly);
    public const string HospitalOnly = nameof(HospitalOnly);
    public const string MedicalServiceProviderOnly = nameof(MedicalServiceProviderOnly);

    public static AuthorizationOptions AddCareConnectPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdminOnly, p => p.RequireRole(AppRoles.SuperAdmin));
        options.AddPolicy(PatientOnly, p => p.RequireRole(AppRoles.Patient));
        options.AddPolicy(DoctorOnly, p => p.RequireRole(AppRoles.Doctor));
        options.AddPolicy(HospitalOnly, p => p.RequireRole(AppRoles.Hospital));
        options.AddPolicy(MedicalServiceProviderOnly, p => p.RequireRole(AppRoles.MedicalServiceProvider));

        // Anything not explicitly marked [AllowAnonymous] needs a signed-in caller.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        return options;
    }
}
