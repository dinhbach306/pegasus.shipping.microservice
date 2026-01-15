using System.Text.Json;

namespace Messaging;

public interface IKafkaProducer
{
    Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default);
}

public static class KafkaProducerExtensions
{
    /// <summary>
    /// Produce a strongly-typed message to Kafka (auto-serializes to JSON)
    /// </summary>
    public static Task ProduceAsync<TMessage>(
        this IKafkaProducer producer,
        string topic,
        string key,
        TMessage message,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        return producer.PublishAsync(topic, key, payload, cancellationToken);
    }
}

