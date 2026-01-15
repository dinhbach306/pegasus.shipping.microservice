namespace Messaging;

/// <summary>
/// Centralized Kafka topic names for all services
/// Each service publishes to its own topic(s)
/// </summary>
public static class KafkaTopics
{
    // Identity Service Topics
    public const string IdentityEvents = "identity-events";
    public const string UserCreated = "identity.user.created";
    public const string UserUpdated = "identity.user.updated";
    public const string UserDeactivated = "identity.user.deactivated";
    
    // Shipping Service Topics
    public const string ShippingEvents = "shipping-events";
    public const string ShipmentCreated = "shipping.shipment.created";
    public const string ShipmentUpdated = "shipping.shipment.updated";
    public const string ShipmentStatusChanged = "shipping.shipment.status-changed";
    
    // Notification Service Topics (future)
    public const string NotificationEvents = "notification-events";
    
    // Dead Letter Queue
    public const string DeadLetterQueue = "dlq-events";
}

