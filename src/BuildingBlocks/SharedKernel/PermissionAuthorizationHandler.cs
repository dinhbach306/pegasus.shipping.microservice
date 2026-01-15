using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace SharedKernel;

/// <summary>
/// Handles permission-based authorization using headers forwarded by API Gateway
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        // Read permissions from header forwarded by Gateway
        var permissionsHeader = httpContext.Request.Headers["X-User-Permissions"].FirstOrDefault();
        if (string.IsNullOrEmpty(permissionsHeader))
        {
            return Task.CompletedTask;
        }

        // Parse comma-separated permissions
        var userPermissions = permissionsHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check if user has ANY of the required permissions
        var hasPermission = requirement.Permissions.Any(p => userPermissions.Contains(p));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

