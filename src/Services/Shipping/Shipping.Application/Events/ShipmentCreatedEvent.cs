namespace Shipping.Application.Events;

public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string Status,
    DateTime CreatedAt,
    string CreatedByUserId,
    string CreatedByEmail
);

