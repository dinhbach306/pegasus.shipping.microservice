using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Messaging;

public sealed class KafkaProducer(IOptions<KafkaOptions> options) : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(new ProducerConfig
    {
        BootstrapServers = options.Value.BootstrapServers,
        ClientId = options.Value.ClientId
    }).Build();

    public async Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default)
    {
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = payload
        }, cancellationToken);
    }

    public void Dispose() => _producer.Dispose();
}

