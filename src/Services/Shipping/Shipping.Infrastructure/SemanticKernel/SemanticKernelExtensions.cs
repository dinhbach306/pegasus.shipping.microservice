using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Shipping.Infrastructure.SemanticKernel;

public static class SemanticKernelExtensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SemanticKernelOptions>(configuration.GetSection(SemanticKernelOptions.SectionName));

        services.AddSingleton(provider =>
        {
            var builder = Kernel.CreateBuilder();
            return builder.Build();
        });

        return services;
    }
}

