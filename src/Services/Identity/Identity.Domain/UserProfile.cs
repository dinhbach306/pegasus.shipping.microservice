using SharedKernel;

namespace Identity.Domain;

public sealed class UserProfile : Entity
{
    public string Auth0UserId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public string Role { get; private set; } = "user"; // Default role

    private UserProfile() { }

    public UserProfile(string auth0UserId, string email, string? fullName, string? role = "user")
    {
        Auth0UserId = auth0UserId;
        Email = email;
        FullName = fullName;
        Role = role ?? "user";
    }

    public void UpdateRole(string role)
    {
        Role = role;
        MarkAsUpdated();
    }
}

