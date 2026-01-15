using Microsoft.AspNetCore.Authorization;

namespace Identity.Api.Authorization;

/// <summary>
/// Authorization requirement that checks for specific permissions
/// User must have AT LEAST ONE of the specified permissions
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string[] Permissions { get; }

    public PermissionRequirement(params string[] permissions)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }
}

