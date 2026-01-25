using System.Collections.Generic;
using System.Text.Json;

namespace Messaging;

public interface IKafkaProducer
{
    Task PublishAsync(
        string topic, 
        string key, 
        string payload, 
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}

public static class KafkaProducerExtensions
{
    /// <summary>
    /// Produce a strongly-typed message to Kafka (auto-serializes to JSON).
    /// If the message inherits from BaseEvent, standard metadata can be provided.
    /// </summary>
    public static Task ProduceAsync<TMessage>(
        this IKafkaProducer producer,
        string topic,
        string key,
        TMessage message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var payload = JsonSerializer.Serialize(message, options);
        
        // If it's a BaseEvent, we might want to extract some info for headers 
        // or ensure headers include standard info like EventType
        if (message is BaseEvent baseEvent)
        {
            headers ??= new Dictionary<string, string>();
            if (!headers.ContainsKey("x-event-type"))
                headers["x-event-type"] = baseEvent.EventType;
            if (!headers.ContainsKey("x-source-service"))
                headers["x-source-service"] = baseEvent.SourceService;
            if (baseEvent.CorrelationId != null && !headers.ContainsKey("x-correlation-id"))
                headers["x-correlation-id"] = baseEvent.CorrelationId;
        }

        return producer.PublishAsync(topic, key, payload, headers, cancellationToken);
    }
}

