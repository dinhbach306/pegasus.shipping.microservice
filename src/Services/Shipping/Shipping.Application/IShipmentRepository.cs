using Shipping.Domain;

namespace Shipping.Application;

public interface IShipmentRepository
{
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);
}

