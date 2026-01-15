using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Application;
using Identity.Application.DTOs;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Auth0;

/// <summary>
/// Service to interact with Auth0 Management API for user/role management
/// </summary>
public sealed class Auth0ManagementService : IAuth0ManagementService
{
    private readonly HttpClient _httpClient;
    private readonly Auth0Options _options;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public Auth0ManagementService(HttpClient httpClient, IOptions<Auth0Options> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri($"https://{_options.Domain}");
    }

    private async Task<string> GetManagementTokenAsync(CancellationToken cancellationToken)
    {
        // Return cached token if still valid
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        // Request new Management API token
        var tokenRequest = new
        {
            client_id = _options.ClientId,
            client_secret = _options.ClientSecret,
            audience = $"https://{_options.Domain}/api/v2/",
            grant_type = "client_credentials"
        };

        var response = await _httpClient.PostAsJsonAsync("/oauth/token", tokenRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _cachedToken = tokenResponse!.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh 1 min before expiry

        return _cachedToken;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(CancellationToken cancellationToken)
    {
        var token = await GetManagementTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _httpClient;
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync(cancellationToken);
        var response = await client.GetAsync("/api/v2/users?per_page=100", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<List<Auth0User>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return users?.Select(u => new UserDto(
            u.UserId,
            u.Email ?? string.Empty,
            u.Name,
            u.EmailVerified,
            Array.Empty<string>() // Permissions would require additional call
        )).ToList() ?? new List<UserDto>();
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync(cancellationToken);
        var response = await client.GetAsync($"/api/v2/users/{Uri.EscapeDataString(userId)}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var user = JsonSerializer.Deserialize<Auth0User>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (user == null) return null;

        // Get user permissions
        var permissionsResponse = await client.GetAsync(
            $"/api/v2/users/{Uri.EscapeDataString(userId)}/permissions", 
            cancellationToken);

        var permissionsContent = await permissionsResponse.Content.ReadAsStringAsync(cancellationToken);
        var permissions = JsonSerializer.Deserialize<List<Auth0Permission>>(permissionsContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Auth0Permission>();

        return new UserDto(
            user.UserId,
            user.Email ?? string.Empty,
            user.Name,
            user.EmailVerified,
            permissions.Select(p => p.PermissionName).ToArray()
        );
    }

    public async Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync(cancellationToken);
        var response = await client.GetAsync("/api/v2/roles", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var roles = JsonSerializer.Deserialize<List<Auth0Role>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return roles?.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Description ?? string.Empty
        )).ToList() ?? new List<RoleDto>();
    }

    public async Task AssignRoleToUserAsync(string userId, string[] roleIds, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync(cancellationToken);
        var payload = new { roles = roleIds };
        
        var response = await client.PostAsJsonAsync(
            $"/api/v2/users/{Uri.EscapeDataString(userId)}/roles",
            payload,
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveRoleFromUserAsync(string userId, string[] roleIds, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync(cancellationToken);
        var payload = new { roles = roleIds };

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v2/users/{Uri.EscapeDataString(userId)}/roles")
        {
            Content = JsonContent.Create(payload)
        };

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record TokenResponse(string AccessToken, int ExpiresIn);
    private sealed record Auth0User(string UserId, string? Email, string? Name, bool EmailVerified);
    private sealed record Auth0Role(string Id, string Name, string? Description);
    private sealed record Auth0Permission(string PermissionName, string ResourceServerIdentifier);
}

