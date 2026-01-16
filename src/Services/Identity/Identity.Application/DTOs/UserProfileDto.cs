namespace Identity.Application.DTOs;

public sealed record UserProfileDto(
    Guid Id,
    string Auth0UserId,
    string Email,
    string? FullName,
    string Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsActive
);

public sealed record CreateUserProfileRequest(
    string Auth0UserId,
    string Email,
    string? FullName,
    string? Role = "user"
);

public sealed record UpdateUserProfileRequest(
    string? FullName,
    string? Role
);

