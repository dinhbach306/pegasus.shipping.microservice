using Shipping.Domain;

namespace Shipping.Application;

public interface IShipmentService
{
    Task<Shipment> CreateAsync(string trackingNumber, string? createdByUserId, string? createdByEmail, CancellationToken cancellationToken = default);
}

