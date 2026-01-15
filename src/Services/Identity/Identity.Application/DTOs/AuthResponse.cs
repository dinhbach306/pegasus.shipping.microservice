namespace Identity.Application.DTOs;

public sealed record AuthResponse(
    string AccessToken,
    string? IdToken,
    string TokenType,
    int ExpiresIn
);

