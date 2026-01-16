using Shipping.Application.DTOs;
using Shipping.Domain;

namespace Shipping.Application;

public interface IShipmentService
{
    Task<Shipment> CreateAsync(CreateShipmentRequest request, string? createdByUserId, string? createdByEmail, CancellationToken cancellationToken = default);
}

