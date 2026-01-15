namespace Identity.Application.DTOs;

public sealed record UserDto(
    string UserId,
    string Email,
    string? Name,
    bool EmailVerified,
    string[] Permissions
);

public sealed record RoleDto(
    string Id,
    string Name,
    string Description
);

public sealed record AssignRoleRequest(
    string UserId,
    string[] Roles
);

public sealed record AssignPermissionsRequest(
    string UserId,
    string[] Permissions
);

