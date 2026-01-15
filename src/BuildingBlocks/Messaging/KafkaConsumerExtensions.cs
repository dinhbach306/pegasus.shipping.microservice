using Microsoft.Extensions.DependencyInjection;

namespace Messaging;

public static class KafkaConsumerExtensions
{
    /// <summary>
    /// Register a Kafka consumer for a specific message type and topic
    /// </summary>
    public static IServiceCollection AddKafkaConsumer<TMessage, THandler>(
        this IServiceCollection services,
        string topic)
        where TMessage : class
        where THandler : class, IKafkaConsumer<TMessage>
    {
        services.AddScoped<IKafkaConsumer<TMessage>, THandler>();
        services.AddHostedService(provider =>
            new KafkaConsumerService<TMessage>(
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaOptions>>(),
                provider.GetRequiredService<IKafkaConsumer<TMessage>>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KafkaConsumerService<TMessage>>>(),
                topic));

        return services;
    }
}

