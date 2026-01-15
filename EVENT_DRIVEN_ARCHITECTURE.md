# Event-Driven Architecture with Kafka

## Overview

This microservices system uses **Apache Kafka** for asynchronous communication between services. Each service has its own dedicated topic(s) for publishing events, following the Event-Driven Architecture pattern.

## Architecture

```
┌─────────────┐                    ┌──────────────┐
│  Identity   │ ─── publish ──────►│ Kafka        │
│  Service    │    to identity.*   │              │
└─────────────┘                    │ Topics:      │
                                   │ • identity.* │
┌─────────────┐                    │ • shipping.* │
│  Shipping   │ ─── publish ──────►│              │
│  Service    │    to shipping.*   │              │
└──────┬──────┘                    └───────┬──────┘
       │                                   │
       └──────── subscribe to ─────────────┘
                 identity.* events
```

## Kafka Topics by Service

### Identity Service Topics

| Topic Name                  | Description             | Event Type           |
| --------------------------- | ----------------------- | -------------------- |
| `identity.user.created`     | User registered         | UserCreatedEvent     |
| `identity.user.updated`     | User profile updated    | UserUpdatedEvent     |
| `identity.user.deactivated` | User deactivated        | UserDeactivatedEvent |
| `identity-events`           | General identity events | Mixed                |

### Shipping Service Topics

| Topic Name                         | Description              | Event Type                 |
| ---------------------------------- | ------------------------ | -------------------------- |
| `shipping.shipment.created`        | Shipment created         | ShipmentCreatedEvent       |
| `shipping.shipment.updated`        | Shipment details updated | ShipmentUpdatedEvent       |
| `shipping.shipment.status-changed` | Status changed           | ShipmentStatusChangedEvent |
| `shipping-events`                  | General shipping events  | Mixed                      |

### Common Topics

| Topic Name   | Description                           |
| ------------ | ------------------------------------- |
| `dlq-events` | Dead Letter Queue for failed messages |

## Topic Naming Convention

```
<service>.<entity>.<action>

Examples:
- identity.user.created
- shipping.shipment.created
- payment.order.completed
- notification.email.sent
```

## Event Schema

All events should include:

```csharp
public record BaseEvent(
    Guid EventId,          // Unique event identifier
    DateTime OccurredAt,   // When event occurred (UTC)
    string EventType,      // Type of event
    string SourceService   // Service that published the event
);
```

### Example: ShipmentCreatedEvent

```csharp
public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string Status,
    DateTime CreatedAt,
    string CreatedByUserId,
    string CreatedByEmail
);
```

## Publishing Events

### 1. Define Event DTO

```csharp
// In Service.Application/Events/
public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string Status,
    DateTime CreatedAt,
    string CreatedByUserId,
    string CreatedByEmail
);
```

### 2. Inject IKafkaProducer

```csharp
public class ShipmentService(
    IShipmentRepository repository,
    IKafkaProducer producer) : IShipmentService
{
    // ...
}
```

### 3. Publish Event After Business Operation

```csharp
public async Task<Shipment> CreateAsync(string trackingNumber, ...)
{
    // 1. Execute business logic
    var shipment = new Shipment(trackingNumber);
    await repository.AddAsync(shipment);

    // 2. Publish event
    var @event = new ShipmentCreatedEvent(
        shipment.Id,
        shipment.TrackingNumber,
        shipment.Status,
        shipment.CreatedAt,
        userId,
        userEmail
    );

    await producer.ProduceAsync(
        KafkaTopics.ShipmentCreated,  // Topic
        shipment.Id.ToString(),        // Key (for partitioning)
        @event,                        // Message (auto-serialized)
        cancellationToken
    );

    return shipment;
}
```

## Consuming Events

### 1. Define Event Handler

```csharp
// In Service.Api/Consumers/
public class ShipmentCreatedConsumer : IKafkaConsumer<ShipmentCreatedEvent>
{
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(ILogger<ShipmentCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(ShipmentCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing shipment created: {TrackingNumber}",
            message.TrackingNumber);

        // Business logic here:
        // - Update local cache
        // - Send notification
        // - Update analytics
        // - etc.

        await Task.CompletedTask;
    }
}
```

### 2. Register Consumer in Program.cs

```csharp
// Register consumer to listen to specific topic
builder.Services.AddKafkaConsumer<ShipmentCreatedEvent, ShipmentCreatedConsumer>(
    KafkaTopics.ShipmentCreated
);
```

The consumer runs as a **BackgroundService** and continuously polls for messages.

## Event Flow Example

### Scenario: Create Shipment

```
1. User calls API Gateway
   POST /api/shipments

2. Gateway validates JWT, forwards to Shipping.Api

3. Shipping.Api creates shipment
   ├─ Save to shipping-db (port 1434)
   └─ Publish ShipmentCreatedEvent to "shipping.shipment.created"

4. Kafka receives event and stores in topic

5. Identity service (subscribed to shipping.shipment.created)
   ├─ Consumer receives event
   ├─ Updates user activity log
   └─ Sends confirmation email (via SendGrid)

6. Notification service (future)
   ├─ Consumer receives event
   └─ Sends push notification to user
```

## Consumer Groups

Each service has its own consumer group:

```csharp
var config = new ConsumerConfig
{
    GroupId = "identity-api-shipmentcreated-consumer",
    //         ^service   ^message type
    AutoOffsetReset = AutoOffsetReset.Earliest
};
```

### Benefits:

- Multiple instances of same service share the load
- Different services can consume same topic independently
- Each group tracks its own offset

## Kafka Configuration in Services

### Producer Configuration

Each service publishes to its own topics:

```json
// Identity.Api/appsettings.json
"Kafka": {
  "BootstrapServers": "localhost:29092",
  "ClientId": "identity-api"
}

// Shipping.Api/appsettings.json
"Kafka": {
  "BootstrapServers": "localhost:29092",
  "ClientId": "shipping-api"
}
```

## Viewing Topics in Kafka UI

1. Open Kafka UI: http://localhost:8080
2. Navigate to **Topics**
3. See all topics:
   - `identity.user.created`
   - `shipping.shipment.created`
   - etc.
4. Click on topic to see:
   - Messages
   - Partitions
   - Consumer groups
   - Configuration

## Testing Event Publishing

### 1. Create a Shipment (publishes event)

```bash
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"trackingNumber":"TRACK123"}'
```

Response includes event info:

```json
{
  "shipment": { ... },
  "createdBy": { ... },
  "eventPublished": "shipping.shipment.created"
}
```

### 2. Check Kafka UI

1. Go to http://localhost:8080
2. Click **Topics** → `shipping.shipment.created`
3. Click **Messages**
4. See the published event:

```json
{
  "shipmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "trackingNumber": "TRACK123",
  "status": "Created",
  "createdAt": "2026-01-15T10:30:00Z",
  "createdByUserId": "auth0|123",
  "createdByEmail": "user@example.com"
}
```

### 3. Check Consumer Logs

If you enabled a consumer (e.g., in Identity service):

```bash
# Check Identity service logs
dotnet run --project src/Services/Identity/Identity.Api

# You'll see:
# info: Started consuming from topic: shipping.shipment.created
# info: Received message from shipping.shipment.created at offset 0
# info: Processing shipment created: TRACK123
# info: Successfully processed message
```

## Error Handling

### Retry Logic

```csharp
public async Task HandleAsync(ShipmentCreatedEvent message, CancellationToken cancellationToken)
{
    try
    {
        await ProcessMessage(message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process message");

        // Don't commit offset - message will be reprocessed
        throw;
    }
}
```

### Dead Letter Queue

For messages that fail repeatedly:

```csharp
private int _retryCount = 0;
private const int MaxRetries = 3;

public async Task HandleAsync(TMessage message, ...)
{
    try
    {
        await ProcessMessage(message);
        _retryCount = 0; // Reset on success
    }
    catch (Exception ex)
    {
        _retryCount++;

        if (_retryCount >= MaxRetries)
        {
            // Send to DLQ
            await _producer.ProduceAsync(
                KafkaTopics.DeadLetterQueue,
                message.Id,
                new FailedMessage(message, ex.Message)
            );

            _retryCount = 0;
            return; // Commit offset, don't retry
        }

        throw; // Retry
    }
}
```

## Message Ordering

Kafka guarantees order **within a partition**:

```csharp
// Same key → same partition → ordered
await producer.ProduceAsync(
    topic,
    shipment.Id.ToString(),  // All events for same shipment go to same partition
    @event
);
```

## Idempotency

Ensure consumers are idempotent (can process same message multiple times):

```csharp
public async Task HandleAsync(ShipmentCreatedEvent message, ...)
{
    // Check if already processed
    var exists = await _repository.ExistsAsync(message.ShipmentId);
    if (exists)
    {
        _logger.LogWarning("Duplicate event, skipping: {Id}", message.ShipmentId);
        return; // Idempotent
    }

    // Process message
    await _repository.CreateAsync(message);
}
```

## Monitoring Events

### Prometheus Metrics

Track published/consumed events:

```csharp
private static readonly Counter EventsPublished = Metrics.CreateCounter(
    "kafka_events_published_total",
    "Total events published to Kafka",
    new CounterConfiguration { LabelNames = new[] { "topic", "service" } }
);

// When publishing
EventsPublished.WithLabels(topic, "shipping-api").Inc();
```

### OpenTelemetry Tracing

Events are traced across services:

```
Trace: Create Shipment
├─ Span: ShippingApi.CreateShipment
│  └─ Span: Database.Insert
├─ Span: Kafka.Publish (shipping.shipment.created)
└─ Span: IdentityService.ConsumeShipmentCreated
   └─ Span: SendEmail
```

## Best Practices

### 1. Use Specific Topics Per Event Type

```
✅ shipping.shipment.created
✅ shipping.shipment.status-changed

❌ shipping-events (too generic)
```

### 2. Include Metadata in Events

```csharp
public record ShipmentCreatedEvent(
    Guid ShipmentId,
    // ... business data ...
    DateTime CreatedAt,         // Timestamp
    string CreatedByUserId,     // Who triggered
    string CorrelationId,       // Trace requests
    int Version                 // Event schema version
);
```

### 3. Version Your Events

```csharp
public sealed record ShipmentCreatedEventV1(...);
public sealed record ShipmentCreatedEventV2(...); // New version

// Handle both versions in consumer
public async Task HandleAsync(object message, ...)
{
    switch (message)
    {
        case ShipmentCreatedEventV1 v1:
            await HandleV1(v1);
            break;
        case ShipmentCreatedEventV2 v2:
            await HandleV2(v2);
            break;
    }
}
```

### 4. Don't Publish Sensitive Data

```csharp
❌ Don't: password, credit card, SSN
✅ Do: userId, email, status

// Instead of full data, publish ID and let consumers call API if needed
public record UserUpdatedEvent(
    string UserId,
    // Don't include: Password, SecurityAnswers, etc.
);
```

### 5. Use Transactional Outbox Pattern

For guaranteed event publishing:

```csharp
// 1. Save entity and event in same transaction
using var transaction = await context.Database.BeginTransactionAsync();

await context.Shipments.AddAsync(shipment);
await context.OutboxEvents.AddAsync(new OutboxEvent
{
    Topic = KafkaTopics.ShipmentCreated,
    Payload = JsonSerializer.Serialize(@event)
});

await context.SaveChangesAsync();
await transaction.CommitAsync();

// 2. Background service publishes events from outbox
public class OutboxPublisher : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var events = await GetUnpublishedEvents();
            foreach (var @event in events)
            {
                await _producer.PublishAsync(@event.Topic, @event.Payload);
                await MarkAsPublished(@event.Id);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

## Service Communication Patterns

### Pattern 1: Event Notification

Service publishes event, other services react:

```
Shipping creates shipment
    ↓ publish event
Kafka (shipping.shipment.created)
    ↓ consume
Identity logs activity
Notification sends email
Analytics updates dashboard
```

### Pattern 2: Event-Carried State Transfer

Include full entity state in event to avoid API calls:

```csharp
public record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string Status,
    // Include all data consumers might need
    ShipmentDetails Details,
    Address OriginAddress,
    Address DestinationAddress
);
```

### Pattern 3: Event Sourcing (Advanced)

Store all changes as events:

```csharp
// Events
- ShipmentCreated
- ShipmentPickedUp
- ShipmentInTransit
- ShipmentDelivered

// Current state = replay all events
var shipment = new Shipment();
foreach (var @event in events)
{
    shipment.Apply(@event);
}
```

## Cross-Service Data Access

### ❌ Anti-Pattern: Database Access

```csharp
// DON'T DO THIS
var user = await identityDbContext.Users.FindAsync(userId);
```

### ✅ Pattern 1: Consume Events & Cache

```csharp
// Identity service publishes
await producer.ProduceAsync(
    KafkaTopics.UserCreated,
    userId,
    new UserCreatedEvent(userId, email, name)
);

// Shipping service consumes and caches
public class UserCreatedConsumer : IKafkaConsumer<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, ...)
    {
        // Store user info locally for shipment association
        await _localUserCache.UpsertAsync(new LocalUser
        {
            UserId = @event.UserId,
            Email = @event.Email,
            Name = @event.Name
        });
    }
}

// Now Shipping service can use local cache
var user = await _localUserCache.GetAsync(userId);
```

### ✅ Pattern 2: API Calls

```csharp
// Call Identity service API when real-time data needed
var user = await _identityServiceClient.GetUserAsync(userId);
```

## Configuration

### appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:29092",
    "ClientId": "shipping-api"
  }
}
```

### Register in Program.cs

```csharp
// Producer (publish events)
builder.Services.AddKafkaProducer(builder.Configuration);

// Consumer (subscribe to events)
builder.Services.AddKafkaConsumer<ShipmentCreatedEvent, ShipmentCreatedConsumer>(
    KafkaTopics.ShipmentCreated
);
```

## Troubleshooting

### Consumer Not Receiving Messages

**Check 1: Consumer is registered**

```csharp
builder.Services.AddKafkaConsumer<TEvent, THandler>(topic);
```

**Check 2: Topic exists**

```bash
# View topics in Kafka UI
http://localhost:8080
```

**Check 3: Consumer group offset**

```bash
# Reset offset to earliest
kafka-consumer-groups --bootstrap-server localhost:29092 \
  --group identity-api-shipmentcreated-consumer \
  --reset-offsets --to-earliest --execute --topic shipping.shipment.created
```

### Messages Not Being Published

**Check 1: Kafka is running**

```bash
docker ps | grep kafka
docker logs pegasus-kafka
```

**Check 2: Producer is registered**

```csharp
builder.Services.AddKafkaProducer(builder.Configuration);
```

**Check 3: Topic auto-creation**

```bash
# Topics are auto-created on first publish
# Or manually create:
kafka-topics --bootstrap-server localhost:29092 \
  --create --topic shipping.shipment.created --partitions 3 --replication-factor 1
```

### Lag in Processing

**Check consumer lag in Kafka UI:**

1. Go to **Consumers** tab
2. Find your consumer group
3. Check lag per partition

**Increase parallelism:**

```csharp
// Increase topic partitions
kafka-topics --alter --topic shipping.shipment.created --partitions 10

// Run multiple consumer instances (auto load-balanced)
docker compose up -d --scale shipping-api=3
```

## Security

### 1. Use SASL Authentication (Production)

```yaml
# docker-compose.yml
kafka:
  environment:
    KAFKA_SASL_ENABLED: "true"
    KAFKA_SASL_MECHANISM: "PLAIN"
```

```csharp
// appsettings.json
"Kafka": {
  "BootstrapServers": "localhost:29092",
  "SecurityProtocol": "SaslPlaintext",
  "SaslMechanism": "Plain",
  "SaslUsername": "admin",
  "SaslPassword": "admin-secret"
}
```

### 2. Encrypt Sensitive Data in Events

```csharp
var encryptedEmail = _encryptionService.Encrypt(user.Email);
var @event = new UserCreatedEvent(user.Id, encryptedEmail);
```

### 3. Use ACLs (Access Control Lists)

```bash
# Only shipping-api can write to shipping.* topics
kafka-acls --add --allow-principal User:shipping-api \
  --producer --topic 'shipping.*'

# Only identity-api can read from shipping.* topics
kafka-acls --add --allow-principal User:identity-api \
  --consumer --group 'identity-api-*' --topic 'shipping.*'
```

## Performance Optimization

### 1. Batch Publishing

```csharp
var events = shipments.Select(s => new ShipmentCreatedEvent(...));
foreach (var @event in events)
{
    // Non-blocking publish
    _ = producer.ProduceAsync(topic, key, @event);
}
await producer.FlushAsync(); // Wait for all
```

### 2. Compression

```csharp
var config = new ProducerConfig
{
    BootstrapServers = "localhost:29092",
    CompressionType = CompressionType.Snappy // or Gzip, Lz4
};
```

### 3. Partitioning Strategy

```csharp
// Use entity ID as key for consistent partitioning
await producer.ProduceAsync(
    topic,
    shipment.Id.ToString(), // Same shipment → same partition → ordered
    @event
);
```

## Topic Management Commands

```bash
# List all topics
kafka-topics --bootstrap-server localhost:29092 --list

# Describe topic
kafka-topics --bootstrap-server localhost:29092 \
  --describe --topic shipping.shipment.created

# Create topic manually
kafka-topics --bootstrap-server localhost:29092 \
  --create --topic shipping.shipment.created \
  --partitions 3 --replication-factor 1

# Delete topic
kafka-topics --bootstrap-server localhost:29092 \
  --delete --topic old-topic

# Increase partitions
kafka-topics --bootstrap-server localhost:29092 \
  --alter --topic shipping.shipment.created --partitions 6
```

## See Also

- [Kafka Documentation](https://kafka.apache.org/documentation/)
- [Confluent Kafka .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- [Event-Driven Microservices](https://microservices.io/patterns/data/event-driven-architecture.html)
- `src/BuildingBlocks/Messaging/KafkaTopics.cs` - Topic definitions
