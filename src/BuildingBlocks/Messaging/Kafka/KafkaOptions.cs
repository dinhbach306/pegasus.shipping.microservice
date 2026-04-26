using Confluent.Kafka;

namespace Messaging.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;
    public string ClientId { get; init; } = "pegasus-client";
    public string GroupId { get; init; } = "pegasus-group";
    public AutoOffsetReset AutoOffsetReset { get; init; } = AutoOffsetReset.Earliest;
    public bool EnableAutoCommit { get; init; } = false;
}

