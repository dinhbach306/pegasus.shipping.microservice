namespace Messaging;

/// <summary>
/// Interface for Kafka message consumers
/// </summary>
public interface IKafkaConsumer<TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}

