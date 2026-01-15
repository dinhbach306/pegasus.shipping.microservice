using Identity.Application;
using Identity.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(
    IAuth0ManagementService managementService,
    HeaderUserContext userContext) : ControllerBase
{
    /// <summary>
    /// Get all users - Requires admin:all or read:users permission
    /// </summary>
    [RequirePermission("admin:all", "read:users")]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        try
        {
            var users = await managementService.GetUsersAsync(cancellationToken);
            return Ok(new
            {
                users,
                requestedBy = new
                {
                    userContext.UserId,
                    userContext.Email,
                    permissions = userContext.Permissions
                }
            });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { error = "Failed to fetch users from Auth0", details = ex.Message });
        }
    }

    /// <summary>
    /// Get user by ID - Requires admin:all or read:users permission
    /// </summary>
    [RequirePermission("admin:all", "read:users")]
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserById(string userId, CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        try
        {
            var user = await managementService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(new { error = "User not found", userId });
            }

            return Ok(user);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { error = "Failed to fetch user from Auth0", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all roles - Requires admin:all or read:roles permission
    /// </summary>
    [RequirePermission("admin:all", "read:roles")]
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        try
        {
            var roles = await managementService.GetRolesAsync(cancellationToken);
            return Ok(new
            {
                roles,
                requestedBy = new
                {
                    userContext.UserId,
                    userContext.Email,
                    permissions = userContext.Permissions
                }
            });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { error = "Failed to fetch roles from Auth0", details = ex.Message });
        }
    }

    /// <summary>
    /// Assign roles to user - Requires admin:all or create:role_members permission
    /// </summary>
    [RequirePermission("admin:all", "create:role_members")]
    [HttpPost("users/{userId}/roles")]
    public async Task<IActionResult> AssignRolesToUser(
        string userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        try
        {
            await managementService.AssignRoleToUserAsync(userId, request.Roles, cancellationToken);
            return Ok(new
            {
                message = $"Roles assigned to user {userId} successfully",
                roles = request.Roles,
                assignedBy = new
                {
                    userContext.UserId,
                    userContext.Email,
                    permissions = userContext.Permissions
                }
            });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { error = "Failed to assign roles", details = ex.Message });
        }
    }

    /// <summary>
    /// Remove roles from user - Requires admin:all or delete:role_members permission
    /// </summary>
    [RequirePermission("admin:all", "delete:role_members")]
    [HttpDelete("users/{userId}/roles")]
    public async Task<IActionResult> RemoveRolesFromUser(
        string userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        try
        {
            await managementService.RemoveRoleFromUserAsync(userId, request.Roles, cancellationToken);
            return Ok(new
            {
                message = $"Roles removed from user {userId} successfully",
                roles = request.Roles,
                removedBy = new
                {
                    userContext.UserId,
                    userContext.Email,
                    permissions = userContext.Permissions
                }
            });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { error = "Failed to remove roles", details = ex.Message });
        }
    }

    /// <summary>
    /// Admin-only endpoint - Test admin permissions
    /// </summary>
    [RequirePermission("admin:all")]
    [HttpGet("test")]
    public IActionResult TestAdminAccess()
    {
        return Ok(new
        {
            message = "You have admin access!",
            user = new
            {
                userContext.UserId,
                userContext.Email,
                permissions = userContext.Permissions
            }
        });
    }
}

