using Microsoft.EntityFrameworkCore;
using Shipping.Application;
using Shipping.Domain;

namespace Shipping.Infrastructure;

public sealed class ShipmentRepository(ShippingDbContext dbContext) : IShipmentRepository
{
    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return dbContext.Shipments.FirstOrDefaultAsync(shipment => shipment.TrackingNumber == trackingNumber, cancellationToken);
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await dbContext.Shipments.AddAsync(shipment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

