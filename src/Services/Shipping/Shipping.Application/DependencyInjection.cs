using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Shipping.Application.Mappings;
using System.Reflection;

namespace Shipping.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShippingApplication(this IServiceCollection services)
    {
        // Register Mapster
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}

