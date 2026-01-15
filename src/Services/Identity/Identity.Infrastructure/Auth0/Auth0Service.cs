using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Identity.Application;
using Identity.Application.DTOs;
using Identity.Domain;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Auth0;

public sealed class Auth0Service : IAuth0Service
{
    private readonly AuthenticationApiClient _authClient;
    private readonly Auth0Options _options;
    private readonly IUserProfileRepository _userProfileRepository;

    public Auth0Service(
        IOptions<Auth0Options> options,
        IUserProfileRepository userProfileRepository)
    {
        _options = options.Value;
        _userProfileRepository = userProfileRepository;
        _authClient = new AuthenticationApiClient(new Uri($"https://{_options.Domain}"));
    }

    private async Task<string> GetManagementApiTokenAsync(CancellationToken cancellationToken)
    {
        var tokenRequest = new ClientCredentialsTokenRequest
        {
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            Audience = $"https://{_options.Domain}/api/v2/"
        };

        var tokenResponse = await _authClient.GetTokenAsync(tokenRequest, cancellationToken);
        return tokenResponse.AccessToken ?? throw new InvalidOperationException("Failed to get Management API token");
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Get Management API token
            var managementToken = await GetManagementApiTokenAsync(cancellationToken);
            var managementClient = new ManagementApiClient(managementToken, new Uri($"https://{_options.Domain}/api/v2"));

            // 2. Create user in Auth0 using Management API
            var auth0UserRequest = new UserCreateRequest
            {
                Email = request.Email,
                Password = request.Password,
                Connection = _options.Connection,
                EmailVerified = false, // User should verify email
                UserMetadata = new
                {
                    full_name = request.Name,
                    role = request.Role ?? "user"
                }
            };

            var auth0User = await managementClient.Users.CreateAsync(auth0UserRequest, cancellationToken);

            // 3. Assign role to user in Auth0
            var roleName = request.Role ?? "user";
            var roleId = Auth0Roles.GetRoleId(roleName);
            
            try
            {
                var rolesRequest = new AssignRolesRequest
                {
                    Roles = new[] { roleId }
                };
                await managementClient.Users.AssignRolesAsync(auth0User.UserId, rolesRequest, cancellationToken);
            }
            catch (Exception roleEx)
            {
                // Log role assignment error but don't fail registration
                // User can be assigned role later by admin
                Console.WriteLine($"Warning: Failed to assign role '{roleName}' (ID: {roleId}) to user {auth0User.UserId}: {roleEx.Message}");
            }

            // 4. Save user to local database
            var userProfile = new UserProfile(
                auth0UserId: auth0User.UserId,
                email: auth0User.Email,
                fullName: request.Name,
                role: roleName
            );

            await _userProfileRepository.AddAsync(userProfile, cancellationToken);

            // 5. Login to get tokens (tokens will include role permissions)
            return await LoginAsync(new LoginRequest(request.Email, request.Password), cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Auth0 registration failed: {ex.Message}. " +
                $"Make sure: " +
                $"1. Application has Password grant enabled. " +
                $"2. Database Connection '{_options.Connection}' is enabled. " +
                $"3. Password meets Auth0 password policy. " +
                $"4. Client has Management API permissions (create:users, update:users, read:roles). " +
                $"Check Auth0 Dashboard → Monitoring → Logs for details.", ex);
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use Resource Owner Password flow with Auth0 SDK
            var tokenRequest = new ResourceOwnerTokenRequest
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret,
                Username = request.Email,
                Password = request.Password,
                Audience = _options.Audience,
                Scope = "openid profile email",
                Realm = _options.Connection  // Specify DB connection for authentication
            };

            var tokenResponse = await _authClient.GetTokenAsync(tokenRequest, cancellationToken);

            return new AuthResponse(
                tokenResponse.AccessToken ?? string.Empty,
                tokenResponse.IdToken,
                tokenResponse.TokenType ?? "Bearer",
                tokenResponse.ExpiresIn
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Auth0 login failed: {ex.Message}. " +
                $"Troubleshooting: " +
                $"1. Enable Password grant in: Applications → Your App → Advanced Settings → Grant Types. " +
                $"2. Set Default Directory in: Applications → APIs → {_options.Audience} → Settings → Default Directory = '{_options.Connection}'. " +
                $"3. Verify user exists and credentials are correct. " +
                $"4. Check Auth0 Dashboard → Monitoring → Logs for detailed error.", ex);
        }
    }

    public string GetGoogleLoginUrl(string redirectUri)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = "openid profile email",
            ["connection"] = "google-oauth2"
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"https://{_options.Domain}/authorize?{queryString}";
    }

    public async Task<AuthResponse> ExchangeCodeForTokenAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenRequest = new AuthorizationCodeTokenRequest
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret,
                Code = code,
                RedirectUri = redirectUri
            };

            var tokenResponse = await _authClient.GetTokenAsync(tokenRequest, cancellationToken);

            return new AuthResponse(
                tokenResponse.AccessToken ?? string.Empty,
                tokenResponse.IdToken,
                tokenResponse.TokenType ?? "Bearer",
                tokenResponse.ExpiresIn
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to exchange authorization code for token: {ex.Message}", ex);
        }
    }
}
