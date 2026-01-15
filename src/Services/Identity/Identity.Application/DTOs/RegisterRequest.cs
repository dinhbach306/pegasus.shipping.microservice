namespace Identity.Application.DTOs;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string Name,
    string? Role = "user"  // Default role is "user" if not specified
);

