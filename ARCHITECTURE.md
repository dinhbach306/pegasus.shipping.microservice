# Architecture Documentation

## System Overview

Pegasus.Ship is a microservices application built with .NET 10 following Clean Architecture principles and using an API Gateway pattern for centralized authentication and routing.

## High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                         External Clients                         │
│                    (Web, Mobile, Third-party)                    │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             │ HTTP/HTTPS
                             ▼
                  ┌──────────────────────┐
                  │   API Gateway        │
                  │   (Ocelot)           │
                  │   Port: 5100         │
                  │                      │
                  │  ✓ JWT Validation    │
                  │  ✓ Claims Extraction │
                  │  ✓ Rate Limiting     │
                  │  ✓ Routing           │
                  └──────────┬───────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌───────────────┐    ┌──────────────┐    ┌──────────────┐
│Identity Service│    │Shipping Srv  │    │Future        │
│  Port: 5000    │    │  Port: 5001  │    │Services...   │
│                │    │              │    │              │
│ • Register     │    │ • Shipments  │    │              │
│ • Login        │    │ • Tracking   │    │              │
│ • OAuth        │    │ • AI (SK)    │    │              │
└───────┬────────┘    └───────┬──────┘    └──────────────┘
        │                     │
        ▼                     ▼
┌───────────────────────────────────────────────────────────┐
│            Shared Infrastructure Layer                    │
│                                                           │
│  ┌───────────┐  ┌───────────┐  ┌────────┐  ┌─────────┐  │
│  │Identity   │  │Shipping   │  │ Kafka  │  │Auth0    │  │
│  │SQL Server │  │SQL Server │  │Port:   │  │(Cloud)  │  │
│  │Port: 1433 │  │Port: 1434 │  │29092   │  │         │  │
│  └───────────┘  └───────────┘  └────────┘  └─────────┘  │
│                                                           │
│  ┌──────────┐                                            │
│  │SendGrid  │  Database Per Service Pattern              │
│  │(Cloud)   │  Each service has dedicated DB             │
│  └──────────┘                                            │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │   Observability Stack (OLTP + Prometheus + Loki)   │  │
│  └────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

## Authentication Flow

### 1. User Registration/Login

```
┌────────┐                ┌─────────┐              ┌──────────┐         ┌──────┐
│ Client │                │ Gateway │              │ Identity │         │Auth0 │
└───┬────┘                └────┬────┘              └────┬─────┘         └───┬──┘
    │                          │                        │                   │
    │ POST /identity/login     │                        │                   │
    ├─────────────────────────►│                        │                   │
    │ {email, password}        │                        │                   │
    │                          │  Forward request       │                   │
    │                          ├───────────────────────►│                   │
    │                          │                        │ Validate via Auth0│
    │                          │                        ├──────────────────►│
    │                          │                        │                   │
    │                          │                        │ JWT Token         │
    │                          │                        │◄──────────────────┤
    │                          │  Return token          │                   │
    │                          │◄───────────────────────┤                   │
    │ {accessToken, idToken}   │                        │                   │
    │◄─────────────────────────┤                        │                   │
    │                          │                        │                   │
```

### 2. Authenticated Request to Service

```
┌────────┐            ┌─────────┐               ┌──────────┐         ┌──────┐
│ Client │            │ Gateway │               │ Service  │         │Auth0 │
└───┬────┘            └────┬────┘               └────┬─────┘         └───┬──┘
    │                      │                         │                   │
    │ GET /shipments/T123  │                         │                   │
    │ Authorization: Bearer│                         │                   │
    ├─────────────────────►│                         │                   │
    │                      │                         │                   │
    │                      │ Validate JWT            │                   │
    │                      ├────────────────────────────────────────────►│
    │                      │                         │                   │
    │                      │ Token Valid             │                   │
    │                      │◄────────────────────────────────────────────┤
    │                      │                         │                   │
    │                      │ Extract Claims:         │                   │
    │                      │ - sub → X-User-Id       │                   │
    │                      │ - email → X-User-Email  │                   │
    │                      │                         │                   │
    │                      │ Forward with headers    │                   │
    │                      ├────────────────────────►│                   │
    │                      │ X-User-Id: auth0|123    │                   │
    │                      │ X-User-Email: u@e.com   │                   │
    │                      │                         │                   │
    │                      │  Response + user context│                   │
    │                      │◄────────────────────────┤                   │
    │ {data, requestedBy}  │                         │                   │
    │◄─────────────────────┤                         │                   │
    │                      │                         │                   │
```

## Service Internal Architecture (Clean Architecture)

Each service follows Clean Architecture:

```
┌───────────────────────────────────────────────────────────┐
│                      Service.Api                          │
│  • Controllers (HTTP endpoints)                           │
│  • Middleware (user context extraction)                   │
│  • Program.cs (DI, configuration)                         │
└─────────────────────────┬─────────────────────────────────┘
                          │ depends on
                          ▼
┌───────────────────────────────────────────────────────────┐
│                  Service.Application                      │
│  • Use Cases / Application Services                       │
│  • DTOs                                                   │
│  • Interfaces (repository, external services)             │
└─────────────────────────┬─────────────────────────────────┘
                          │ depends on
                          ▼
┌───────────────────────────────────────────────────────────┐
│                    Service.Domain                         │
│  • Entities (business logic)                              │
│    - Inherit from Entity base class                       │
│    - Automatic audit fields (CreatedAt, UpdatedAt)        │
│    - Soft delete support (IsActive)                       │
│  • Value Objects                                          │
│  • Domain Events                                          │
│  • No dependencies                                        │
└───────────────────────────────────────────────────────────┘
                          ▲
                          │ implements
┌─────────────────────────┴─────────────────────────────────┐
│                 Service.Infrastructure                    │
│  • DbContext (EF Core)                                    │
│  • Repositories (data access)                             │
│  • External API clients                                   │
│  • Message publishers (Kafka)                             │
└───────────────────────────────────────────────────────────┘
```

## Building Blocks

Shared libraries used across all services:

- **SharedKernel**: Base `Entity`, `ValueObject`, `HeaderUserContext`
- **Messaging**: Kafka producer/consumer abstractions, topic definitions (`KafkaTopics`)
- **Observability**: OpenTelemetry ActivitySource and Meter names
- **ServiceDefaults**: OpenTelemetry configuration, health checks, metrics

## Data Flow Example: Create Shipment

```
1. Client → Gateway: POST /shipments + JWT
2. Gateway validates JWT with Auth0
3. Gateway extracts userId, email from token
4. Gateway forwards to Shipping.Api with headers:
   X-User-Id: auth0|123456
   X-User-Email: user@example.com
5. Shipping.Api reads HeaderUserContext from headers
6. Shipping.Application.ShipmentService creates shipment
7. Shipping.Domain.Shipment entity validates business rules
8. Shipping.Infrastructure persists to SQL Server
9. Shipping.Infrastructure publishes ShipmentCreated event to Kafka topic `shipping.shipment.created`
10. Other services (Identity, Notification) consume this event from their own consumers
11. Shipping.Api returns response with shipment + createdBy info
11. Gateway forwards response to client
```

## Security Model

### API Gateway Responsibilities

- ✅ Validate JWT signature (RSA256)
- ✅ Verify token expiration
- ✅ Check audience matches API identifier
- ✅ Extract claims and forward as headers
- ✅ Rate limiting per client
- ✅ CORS configuration

### Downstream Service Responsibilities

- ✅ Read user context from headers (`X-User-Id`, `X-User-Email`)
- ✅ Business-level authorization (roles, permissions)
- ✅ Reject requests without user context (not from Gateway)
- ❌ Do NOT validate JWT (Gateway already did)

### Production Considerations

- Use **internal network** or **service mesh** so services only accept Gateway requests
- Enable **mutual TLS** between Gateway and services
- Use **API keys** or **service tokens** for internal auth
- Implement **distributed tracing** to track requests across services

## Observability

All services export telemetry to OpenTelemetry Collector:

```
Services → OTLP Collector → ┌─ Prometheus (metrics)
                            ├─ Tempo (traces)
                            └─ Loki (logs)
                                    ↓
                              Grafana (visualization)
```

### Key Metrics

- Request rate, latency, errors (RED metrics)
- Gateway routing performance
- Auth validation time
- Database query duration
- Kafka message throughput (by topic: `identity.*`, `shipping.*`)

### Distributed Tracing

- Trace spans across Gateway → Service → Database
- Correlation IDs in headers
- User context included in spans

## Deployment

### Development

```bash
# Infrastructure
docker compose up -d

# Services (3 terminals)
dotnet run --project src/ApiGateway
dotnet run --project src/Services/Identity/Identity.Api
dotnet run --project src/Services/Shipping/Shipping.Api
```

### Production (Kubernetes example)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: api-gateway
spec:
  type: LoadBalancer
  ports:
    - port: 80
      targetPort: 8080
  selector:
    app: api-gateway
---
# Internal services only accessible within cluster
apiVersion: v1
kind: Service
metadata:
  name: identity-service
spec:
  type: ClusterIP # Not exposed externally
  ports:
    - port: 80
  selector:
    app: identity-service
```

## Event-Driven Communication

Services communicate asynchronously via **Kafka** using dedicated topics per service:

**Topic Structure:**

```
identity.user.created         → User registration events
identity.user.updated         → Profile updates
shipping.shipment.created     → New shipment events
shipping.shipment.status-changed → Status updates
```

**Producer Example (Shipping Service):**

```csharp
// Publish event after creating shipment
await _producer.ProduceAsync(
    KafkaTopics.ShipmentCreated,
    shipment.Id.ToString(),
    new ShipmentCreatedEvent(shipment.Id, trackingNumber, ...)
);
```

**Consumer Example (Identity Service):**

```csharp
// Register consumer in Program.cs
builder.Services.AddKafkaConsumer<ShipmentCreatedEvent, ShipmentCreatedConsumer>(
    KafkaTopics.ShipmentCreated
);

// Handle events
public class ShipmentCreatedConsumer : IKafkaConsumer<ShipmentCreatedEvent>
{
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken ct)
    {
        // React: log user activity, send notification, update analytics
    }
}
```

**See:** `EVENT_DRIVEN_ARCHITECTURE.md` for complete documentation

## Future Enhancements

- [x] Event-driven architecture with Kafka (✓ Implemented)
- [x] Topic-per-service pattern (✓ Implemented)
- [ ] Add Redis for distributed caching
- [ ] Implement circuit breaker pattern (Polly)
- [ ] Add BFF (Backend for Frontend) for web/mobile
- [ ] Transactional Outbox pattern for guaranteed event publishing
- [ ] GraphQL Gateway (Hot Chocolate)
- [ ] Service mesh (Istio/Linkerd) for production
