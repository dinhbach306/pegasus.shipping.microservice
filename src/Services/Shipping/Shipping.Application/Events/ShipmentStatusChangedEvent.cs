namespace Shipping.Application.Events;

public sealed record ShipmentStatusChangedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt
);

