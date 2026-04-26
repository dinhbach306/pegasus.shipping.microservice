using System.Text.Json;
using Messaging.Abstractions;

namespace Messaging.Kafka;

public sealed class KafkaMessageSerializer : IKafkaMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Serialize<T>(T message) => JsonSerializer.Serialize(message, Options);

    public T? Deserialize<T>(string data) => JsonSerializer.Deserialize<T>(data, Options);
}
