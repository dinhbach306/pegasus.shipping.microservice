using Messaging;

namespace Shipping.Application.Events;

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
    public ShipmentCreatedEvent() : this(Guid.Empty, string.Empty, string.Empty, DateTime.MinValue, string.Empty,  string.Empty, string.Empty)
    {
        EventType = nameof(ShipmentCreatedEvent);
        SourceService = "ShippingService";
    }
}

