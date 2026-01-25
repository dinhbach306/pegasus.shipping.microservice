namespace SharedKernel;

/// <summary>
/// User context extracted from headers forwarded by API Gateway
/// </summary>
public sealed class HeaderUserContext
{
    public string? UserId { get; init; }
    public string? Email { get; init; }
    
    public string? UserName { get; init; }
    
    public string[] Permissions { get; init; } = Array.Empty<string>();
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    
    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
    
    public bool HasAnyPermission(params string[] permissions)
    {
        return permissions.Any(HasPermission);
    }
}

