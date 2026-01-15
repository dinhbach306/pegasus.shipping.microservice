using Microsoft.AspNetCore.Authorization;
using SharedKernel;

namespace Shipping.Api.Authorization;

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

            options.AddPolicy("Permission:read:products", policy =>
                policy.Requirements.Add(new PermissionRequirement("read:products")));

            options.AddPolicy("Permission:write:products", policy =>
                policy.Requirements.Add(new PermissionRequirement("write:products")));

            options.AddPolicy("Permission:read:products,admin:all", policy =>
                policy.Requirements.Add(new PermissionRequirement("read:products", "admin:all")));

            options.AddPolicy("Permission:write:products,admin:all", policy =>
                policy.Requirements.Add(new PermissionRequirement("write:products", "admin:all")));
        });

        return services;
    }
}

