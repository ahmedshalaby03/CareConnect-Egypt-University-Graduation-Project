using Microsoft.AspNetCore.Identity;

namespace CareConnect.Domain.Entities;

/// <summary>
/// Nothing custom yet, but having our own role type means we can add columns later
/// without a painful Identity re-plumbing.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
