using Identity.Application;
using Identity.Infrastructure.Auth0;
using Identity.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
        });

        services.Configure<SendGridOptions>(configuration.GetSection(SendGridOptions.SectionName));
        services.AddScoped<IEmailSender, SendGridEmailSender>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        // Auth0 Configuration
        services.Configure<Auth0Options>(configuration.GetSection("Auth0"));
        
        // Auth0Service: Uses Auth0 SDK (no HttpClient needed)
        services.AddScoped<IAuth0Service, Auth0Service>();
        
        // Auth0ManagementService: Uses HttpClient for Management API calls
        services.AddHttpClient<IAuth0ManagementService, Auth0ManagementService>();

        return services;
    }
}

