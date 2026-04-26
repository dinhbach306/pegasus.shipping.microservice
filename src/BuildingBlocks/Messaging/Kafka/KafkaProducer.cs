using System.Text;
using Confluent.Kafka;
using Messaging.Abstractions;
using Microsoft.Extensions.Options;

namespace Messaging.Kafka;

public sealed class KafkaProducer(IOptions<KafkaOptions> options, IKafkaMessageSerializer serializer) : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(new ProducerConfig
    {
        BootstrapServers = options.Value.BootstrapServers,
        ClientId = options.Value.ClientId
    }).Build();

    public async Task PublishAsync(
        string topic, 
        string key, 
        string payload, 
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = payload
        };

        if (headers != null)
        {
            message.Headers = new Headers();
            foreach (var (headerKey, value) in headers)
            {
                message.Headers.Add(headerKey, Encoding.UTF8.GetBytes(value));
            }
        }

        await _producer.ProduceAsync(topic, message, cancellationToken);
    }

    public async Task ProduceAsync<TMessage>(
        string topic,
        string key,
        TMessage message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        var payload = serializer.Serialize(message);
        
        if (message is BaseEvent baseEvent)
        {
            headers ??= new Dictionary<string, string>();
            headers.TryAdd("x-event-type", baseEvent.EventType);
            headers.TryAdd("x-source-service", baseEvent.SourceService);
            if (baseEvent.CorrelationId != null)
                headers.TryAdd("x-correlation-id", baseEvent.CorrelationId);
        }

        await PublishAsync(topic, key, payload, headers, cancellationToken);
    }

    public void Dispose() => _producer.Dispose();
}

