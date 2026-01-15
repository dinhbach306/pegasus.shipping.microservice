namespace Identity.Application.Events;

/// <summary>
/// Event consumed from Shipping service
/// </summary>
public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string Status,
    DateTime CreatedAt,
    string CreatedByUserId,
    string CreatedByEmail
);

