using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shipping.Application;
using Shipping.Infrastructure.SemanticKernel;

namespace Shipping.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShippingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ShippingDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("ShippingDb"));
        });

        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddSemanticKernel(configuration);

        return services;
    }
}

