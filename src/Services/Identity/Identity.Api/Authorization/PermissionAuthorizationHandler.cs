using Microsoft.AspNetCore.Authorization;
using SharedKernel;

namespace Identity.Api.Authorization;

/// <summary>
/// Authorization handler that checks if user has required permissions from headers
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null");
            context.Fail();
            return Task.CompletedTask;
        }

        // Extract user context from headers
        var userId = httpContext.Request.Headers["X-User-Id"].FirstOrDefault();
        var permissionsHeader = httpContext.Request.Headers["X-User-Permissions"].FirstOrDefault() ?? string.Empty;
        var permissions = permissionsHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();

        // Log for debugging
        _logger.LogInformation(
            "Checking permissions. Required: [{Required}], User has: [{UserPermissions}]",
            string.Join(", ", requirement.Permissions),
            string.Join(", ", permissions));

        // Check if user is authenticated (has UserId from Gateway)
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User not authenticated - no X-User-Id header found");
            context.Fail();
            return Task.CompletedTask;
        }

        // Check if user has ANY of the required permissions
        var hasPermission = requirement.Permissions.Any(required => 
            permissions.Contains(required, StringComparer.OrdinalIgnoreCase));

        if (hasPermission)
        {
            _logger.LogInformation("Permission check passed for user {UserId}", userId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Permission check failed for user {UserId}. Required: [{Required}], Has: [{Has}]",
                userId,
                string.Join(", ", requirement.Permissions),
                string.Join(", ", permissions));
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

