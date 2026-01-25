using Messaging;

namespace Shipping.Application.Events;

public sealed record ShipmentStatusChangedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt
) : BaseEvent
{
    public ShipmentStatusChangedEvent() : this(Guid.Empty, string.Empty, string.Empty, string.Empty, DateTime.MinValue)
    {
        EventType = nameof(ShipmentStatusChangedEvent);
        SourceService = "ShippingService";
    }
}

