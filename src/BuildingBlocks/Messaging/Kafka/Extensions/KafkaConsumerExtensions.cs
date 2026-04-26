using Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Kafka.Extensions;

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
        
        // Ensure serializer is registered if not already
        services.AddSingleton<IKafkaMessageSerializer, KafkaMessageSerializer>();

        services.AddHostedService(provider =>
            new KafkaConsumerService<TMessage>(
                provider.GetRequiredService<IOptions<KafkaOptions>>(),
                provider,
                provider.GetRequiredService<ILogger<KafkaConsumerService<TMessage>>>(),
                provider.GetRequiredService<IKafkaMessageSerializer>(),
                topic));

        return services;
    }
}

