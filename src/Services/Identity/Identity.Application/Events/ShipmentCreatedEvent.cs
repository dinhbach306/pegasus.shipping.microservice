using Messaging;

namespace Identity.Application.Events;

/// <summary>
/// Event consumed from Shipping service
/// </summary>
public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string Status,
    DateTime CreatedAt,
    string CreateByUserName,
    string CreatedByUserId,
    string CreatedByEmail
) : BaseEvent
{
    /// <summary>
    /// Parameterless constructor required for JSON deserialization
    /// </summary>
    public ShipmentCreatedEvent() : this(Guid.Empty, string.Empty, string.Empty, DateTime.MinValue, string.Empty, string.Empty, string.Empty)
    {
    }
}

