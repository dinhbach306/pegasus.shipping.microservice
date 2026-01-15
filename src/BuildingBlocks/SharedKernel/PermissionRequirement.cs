using Microsoft.AspNetCore.Authorization;

namespace SharedKernel;

/// <summary>
/// Authorization requirement for permission-based access control
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string[] Permissions { get; }

    public PermissionRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }
}

