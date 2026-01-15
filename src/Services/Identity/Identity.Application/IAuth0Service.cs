using Identity.Application.DTOs;

namespace Identity.Application;

public interface IAuth0Service
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    string GetGoogleLoginUrl(string redirectUri);
    Task<AuthResponse> ExchangeCodeForTokenAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
}

