namespace Identity.Infrastructure.Auth0;

/// <summary>
/// Auth0 Role IDs - Get these from Auth0 Dashboard → User Management → Roles
/// </summary>
public static class Auth0Roles
{
    // Role IDs from Auth0
    public const string AdminRoleId = "rol_uEnmeymNgUPtyxhk";
    public const string ManagerRoleId = "rol_bRzI52g8cwxB3sxm";
    public const string UserRoleId = "rol_X2oINGMH62lo9RIQ";

    // Role names (case-insensitive lookup)
    public static string GetRoleId(string roleName)
    {
        return roleName?.ToLowerInvariant() switch
        {
            "admin" => AdminRoleId,
            "manager" => ManagerRoleId,
            "user" => UserRoleId,
            _ => UserRoleId // Default to user role
        };
    }

    public static string GetRoleName(string roleId)
    {
        return roleId switch
        {
            AdminRoleId => "admin",
            ManagerRoleId => "manager",
            UserRoleId => "user",
            _ => "user"
        };
    }
}

