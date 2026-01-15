using Messaging;
using Shipping.Application.Events;
using Shipping.Domain;

namespace Shipping.Application;

public sealed class ShipmentService(
    IShipmentRepository repository,
    IKafkaProducer producer) : IShipmentService
{
    public async Task<Shipment> CreateAsync(string trackingNumber, string? createdByUserId, string? createdByEmail, CancellationToken cancellationToken = default)
    {
        var shipment = new Shipment(trackingNumber);
        await repository.AddAsync(shipment, cancellationToken);
        
        // Publish event to Kafka - topic dedicated to Shipping service
        var @event = new ShipmentCreatedEvent(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Status,
            shipment.CreatedAt,
            createdByUserId ?? "anonymous",
            createdByEmail ?? "anonymous@system"
        );
        
        await producer.ProduceAsync(KafkaTopics.ShipmentCreated, shipment.Id.ToString(), @event, cancellationToken);
        
        return shipment;
    }
}

