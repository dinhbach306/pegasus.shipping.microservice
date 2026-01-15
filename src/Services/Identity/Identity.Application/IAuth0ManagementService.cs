using Identity.Application.DTOs;

namespace Identity.Application;

public interface IAuth0ManagementService
{
    Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task AssignRoleToUserAsync(string userId, string[] roleIds, CancellationToken cancellationToken = default);
    Task RemoveRoleFromUserAsync(string userId, string[] roleIds, CancellationToken cancellationToken = default);
}

