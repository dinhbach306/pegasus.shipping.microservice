using System.Collections.Generic;

namespace Messaging.Abstractions;

public interface IKafkaProducer
{
    Task PublishAsync(
        string topic, 
        string key, 
        string payload, 
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task ProduceAsync<TMessage>(
        string topic,
        string key,
        TMessage message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where TMessage : class;
}

