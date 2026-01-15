using Microsoft.AspNetCore.Authorization;
using SharedKernel;

namespace Identity.Api.Authorization;

public static class PermissionAuthorizationSetup
{
    public static IServiceCollection AddPermissionBasedAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            // Register dynamic policies for permissions
            options.AddPolicy("Permission:admin:all", policy =>
                policy.Requirements.Add(new PermissionRequirement("admin:all")));

            // Auth0 Management API permissions
            options.AddPolicy("Permission:read:users", policy =>
                policy.Requirements.Add(new PermissionRequirement("read:users")));

            options.AddPolicy("Permission:update:users", policy =>
                policy.Requirements.Add(new PermissionRequirement("update:users")));

            options.AddPolicy("Permission:create:users", policy =>
                policy.Requirements.Add(new PermissionRequirement("create:users")));

            options.AddPolicy("Permission:read:roles", policy =>
                policy.Requirements.Add(new PermissionRequirement("read:roles")));

            options.AddPolicy("Permission:update:roles", policy =>
                policy.Requirements.Add(new PermissionRequirement("update:roles")));

            options.AddPolicy("Permission:create:roles", policy =>
                policy.Requirements.Add(new PermissionRequirement("create:roles")));

            options.AddPolicy("Permission:read:role_members", policy =>
                policy.Requirements.Add(new PermissionRequirement("read:role_members")));

            options.AddPolicy("Permission:create:role_members", policy =>
                policy.Requirements.Add(new PermissionRequirement("create:role_members")));

            options.AddPolicy("Permission:delete:role_members", policy =>
                policy.Requirements.Add(new PermissionRequirement("delete:role_members")));

            // Composite policies - user has ANY of the permissions
            options.AddPolicy("Permission:admin:all,read:users", policy =>
                policy.Requirements.Add(new PermissionRequirement("admin:all", "read:users")));

            options.AddPolicy("Permission:admin:all,read:roles", policy =>
                policy.Requirements.Add(new PermissionRequirement("admin:all", "read:roles")));

            options.AddPolicy("Permission:admin:all,create:role_members", policy =>
                policy.Requirements.Add(new PermissionRequirement("admin:all", "create:role_members")));

            options.AddPolicy("Permission:admin:all,delete:role_members", policy =>
                policy.Requirements.Add(new PermissionRequirement("admin:all", "delete:role_members")));
        });

        return services;
    }
}

