using Microsoft.AspNetCore.Authorization;

namespace SharedKernel;

/// <summary>
/// Attribute to require specific permissions for an endpoint
/// User must have AT LEAST ONE of the specified permissions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(params string[] permissions)
    {
        Policy = $"Permission:{string.Join(",", permissions)}";
    }
}

