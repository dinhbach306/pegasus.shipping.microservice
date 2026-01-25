using System;

namespace Messaging;

/// <summary>
/// Base class for all domain events published to Kafka.
/// Follows the standard event-driven architecture design.
/// </summary>
public abstract record BaseEvent
{
    /// <summary>
    /// Unique identifier for this specific event instance.
    /// Useful for idempotency checks.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The timestamp when the event occurred (UTC).
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The type of the event (e.g., "ShipmentCreated").
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// The service that published this event.
    /// </summary>
    public string SourceService { get; init; } = string.Empty;

    /// <summary>
    /// Correlation ID to track a request across multiple microservices.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Version of the event schema.
    /// </summary>
    public int Version { get; init; } = 1;
}
