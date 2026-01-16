using MapsterMapper;
using Messaging;
using Shipping.Application.DTOs;
using Shipping.Application.Events;
using Shipping.Domain;

namespace Shipping.Application;

public sealed class ShipmentService(
    IShipmentRepository repository,
    IKafkaProducer producer,
    IMapper mapper) : IShipmentService
{
    public async Task<Shipment> CreateAsync(CreateShipmentRequest request, string? createdByUserId, string? createdByEmail, CancellationToken cancellationToken = default)
    {
        // Map DTO to Domain entity
        var shipment = mapper.Map<Shipment>(request);
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

