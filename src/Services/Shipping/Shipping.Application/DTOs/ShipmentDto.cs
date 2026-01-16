namespace Shipping.Application.DTOs;

public sealed record ShipmentDto(
    Guid Id,
    string TrackingNumber,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsActive
);

public sealed record CreateShipmentRequest(
    string TrackingNumber
);

public sealed record UpdateShipmentStatusRequest(
    string Status
);

public sealed record ShipmentListDto(
    Guid Id,
    string TrackingNumber,
    string Status,
    DateTime CreatedAt
);

