using Identity.Application.Events;
using Messaging;

namespace Identity.Api.Consumers;

/// <summary>
/// Example consumer that listens to shipment created events from Shipping service
/// This demonstrates cross-service communication via Kafka
/// </summary>
public sealed class ShipmentCreatedConsumer : IKafkaConsumer<ShipmentCreatedEvent>
{
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(ILogger<ShipmentCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ShipmentCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Received ShipmentCreated event: ShipmentId={ShipmentId}, TrackingNumber={TrackingNumber}, CreatedBy={UserId}",
            message.ShipmentId,
            message.TrackingNumber,
            message.CreatedByUserId);

        // Example: Update user statistics, send notification, etc.
        // For now, just log the event
        
        return Task.CompletedTask;
    }
}

