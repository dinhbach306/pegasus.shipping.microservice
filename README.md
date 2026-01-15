# Pegasus.Ship Microservice Boilerplate (.NET 10)

This repository provides a Clean Architecture microservice template using .NET 10 with **API Gateway pattern**, SQL Server, Kafka, Auth0, SendGrid, Microsoft Semantic Kernel, and OpenTelemetry with Prometheus/Grafana/Loki/Tempo.

## Architecture Overview

```
┌─────────────────┐
│   API Gateway   │ ← JWT Validation happens here
│    (Ocelot)     │   Extract user claims → forward as headers
└────────┬────────┘
         │
    ┌────┴─────┬──────────┐
    │          │          │
┌───▼───┐  ┌──▼──────┐  ┌▼──────────┐
│Identity│  │Shipping │  │Other      │
│Service │  │Service  │  │Services...│
└────────┘  └─────────┘  └───────────┘
   ↓             ↓
Read user context from headers (X-User-Id, X-User-Email)
```

### Responsibilities

**API Gateway (Ocelot)**

- Validate JWT tokens from Auth0 (signature, expiration, audience)
- Extract user claims (sub, email, roles, etc.)
- Forward user context via custom headers (`X-User-Id`, `X-User-Email`)
- Route requests to downstream services
- Rate limiting, CORS, centralized monitoring

**Identity Service**

- Register/Login/Logout
- Issue JWT tokens (via Auth0)
- Manage user profiles
- Password reset, 2FA
- **Does NOT validate tokens** (Gateway does this)

**Other Services (Shipping, etc.)**

- Read user context from headers
- Business logic & authorization checks (permissions/roles)
- **Do NOT validate JWT** (already validated at Gateway)
- Trust the Gateway as the only entry point

## Structure

- `src/ApiGateway` - **Ocelot API Gateway** with JWT validation
- `src/BuildingBlocks`
  - `SharedKernel` - base entity/value object, user context
  - `Messaging` - Kafka abstractions
  - `Observability` - telemetry naming
  - `ServiceDefaults` - OpenTelemetry + health + metrics endpoints
- `src/Services`
  - `Identity` - authentication/authorization only
  - `Shipping` - example business microservice
- `deploy` - observability configs

## Tech Stack

- .NET 10
- **Ocelot** API Gateway
- SQL Server
- Kafka (KRaft, no Zookeeper)
- Microsoft Semantic Kernel
- Auth0 (Username/Password + Social Login via OAuth2)
- SendGrid
- OpenTelemetry + Prometheus + Grafana + Loki + Tempo

## Running Infrastructure (Docker)

```bash
docker compose up -d
```

Services exposed:

- **Identity Database** (SQL Server): `localhost:1433`
- **Shipping Database** (SQL Server): `localhost:1434`
- Kafka: `localhost:29092`
- Kafka UI: `http://localhost:8080`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`
- Loki: `http://localhost:3100`
- Tempo: `http://localhost:3200`

**Note:** Each service has its own dedicated database instance following the **Database Per Service** pattern.

## Run Services

Run in **3 separate terminals**:

```bash
# Terminal 1 - API Gateway (port 5100)
dotnet run --project src/ApiGateway

# Terminal 2 - Identity Service (port 5000)
dotnet run --project src/Services/Identity/Identity.Api

# Terminal 3 - Shipping Service (port 5001)
dotnet run --project src/Services/Shipping/Shipping.Api
```

**Important:** All client requests should go through the API Gateway (`localhost:5100`). Direct calls to services will be rejected (no user context).

## Configuration

### Auth0 Setup

Update `Auth0` section in `ApiGateway/appsettings.json`:

```json
"Auth0": {
  "Domain": "your-tenant.auth0.com",
  "Audience": "https://your-api-identifier"
}
```

For Identity service, also add `ClientId`, `ClientSecret`, and `Connection`:

```json
"Auth0": {
  "Domain": "your-tenant.auth0.com",
  "Audience": "https://your-api-identifier",
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret",
  "Connection": "Username-Password-Authentication"
}
```

**Auth0 Dashboard Setup:**

1. Create a new **API** (set Identifier as your Audience value)
2. Enable **Database** connection (Username-Password-Authentication)
3. Enable **Google** social connection (google-oauth2)
4. Create an **Application** (Regular Web Application)
5. Copy `Domain`, `Client ID`, `Client Secret` to your configs
6. Add allowed callback URLs: `http://localhost:5100/api/identity/callback`

### SendGrid (Email)

Update `SendGrid:ApiKey` in `Identity.Api/appsettings.json`:

```json
"SendGrid": {
  "ApiKey": "your-sendgrid-api-key",
  "FromEmail": "no-reply@yourdomain.com",
  "FromName": "Your App Name"
}
```

### Semantic Kernel (Optional - for AI features)

Update `SemanticKernel` section in `Shipping.Api/appsettings.json`:

```json
"SemanticKernel": {
  "Provider": "OpenAI",
  "ModelId": "gpt-4",
  "ApiKey": "your-openai-api-key",
  "Endpoint": ""
}
```

See `env.example.txt` for a complete list of configuration values.

## OpenTelemetry

Set OTLP exporter endpoint:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

The collector forwards:

- Metrics to Prometheus
- Traces to Tempo
- Logs to Loki

## API Usage Examples

**⚠️ All requests go through API Gateway at `localhost:5100`**

### Identity Service (via Gateway)

#### Register a new user

```bash
curl -X POST http://localhost:5100/api/identity/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"YourPassword123!","name":"John Doe"}'
```

#### Login with username/password

```bash
curl -X POST http://localhost:5100/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"YourPassword123!"}'
```

Response:

```json
{
  "accessToken": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "idToken": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "tokenType": "Bearer",
  "expiresIn": 86400
}
```

**Save the `accessToken` for authenticated requests!**

#### Login with Google OAuth

```bash
# Open in browser:
http://localhost:5100/api/identity/login/google
# After authorization, you'll be redirected to /api/identity/callback with tokens
```

#### Get current user info (requires auth)

```bash
curl -X GET http://localhost:5100/api/identity/me \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

Response includes user context extracted by Gateway:

```json
{
  "userId": "auth0|123456789",
  "email": "user@example.com"
}
```

### Shipping Service (via Gateway)

#### Public endpoint (no auth required)

```bash
curl -X GET http://localhost:5100/api/shipments/status
```

Response:

```json
{
  "service": "Shipping API",
  "status": "operational",
  "version": "1.0.0",
  "timestamp": "2026-01-15T10:30:00Z",
  "message": "Shipping service is running..."
}
```

#### Private endpoint - Track shipment (requires auth)

```bash
curl -X GET http://localhost:5100/api/shipments/TRACK123456 \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

Response includes user who made the request:

```json
{
  "shipment": {
    /* shipment data */
  },
  "requestedBy": {
    "userId": "auth0|123456789",
    "email": "user@example.com"
  }
}
```

#### Private endpoint - Create shipment (requires auth)

```bash
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"trackingNumber":"TRACK789"}'
```

## How Authentication & Authorization Works

### Authentication Flow

1. **Client** calls `/api/identity/login` through **Gateway** → receives JWT token
2. **Client** includes token in `Authorization: Bearer TOKEN` header for subsequent requests
3. **Gateway** (Ocelot):
   - Validates JWT signature with Auth0
   - Checks token expiration, audience
   - Extracts claims (`sub`, `email`, `permissions`, etc.)
   - Forwards user context as headers: `X-User-Id`, `X-User-Email`, `X-User-Permissions`
4. **Downstream Services**:
   - Read `X-User-Id`, `X-User-Email`, and `X-User-Permissions` from headers
   - No JWT validation needed
   - Use user context for business logic & authorization

### Authorization (RBAC)

This system implements **Permission-Based Authorization**:

- **Permissions** are defined in Auth0 API (e.g., `admin:all`, `read:products`, `write:products`)
- **Roles** group permissions (e.g., Admin role has `admin:all`, User role has `read:products`)
- **Gateway** extracts permissions from JWT and forwards via `X-User-Permissions` header
- **Services** use `[RequirePermission]` attribute to protect endpoints

```csharp
// Requires read:products OR admin:all
[RequirePermission("read:products", "admin:all")]
[HttpGet("{id}")]
public IActionResult Get(string id) { }

// Requires ONLY admin:all
[RequirePermission("admin:all")]
[HttpDelete("{id}")]
public IActionResult Delete(string id) { }
```

**See `RBAC_GUIDE.md` for detailed setup and usage.**

### Security Notes

- Services should **only accept requests from the Gateway** (use internal network/service mesh in production)
- Gateway is the **only entry point** for external clients
- Services trust the Gateway to have already validated authentication
- Use mutual TLS between Gateway and services in production

## Notes

- `Identity` service handles authentication/authorization, user management, and role/permission management via Auth0.
- `Shipping` service is an example domain service with RBAC using Semantic Kernel integration.
- All protected endpoints require a valid JWT token with appropriate permissions.
- Tokens obtained from Identity service can be used across all microservices.
- API Gateway validates tokens **once** and extracts permissions, downstream services use forwarded user context.
- Admin endpoints (`/api/admin/*`) require `admin:all` or specific management permissions.

## Documentation

- `README.md` - Main documentation (this file)
- `QUICKSTART.md` - Step-by-step setup guide
- `ARCHITECTURE.md` - Detailed architecture and design
- `AUTH0_SETUP_GUIDE.md` - **Auth0 configuration & troubleshooting**
- `OBSERVABILITY_SETUP.md` - **Observability stack (Prometheus, Grafana, Loki, Tempo)**
- `EVENT_DRIVEN_ARCHITECTURE.md` - **Kafka topics & event-driven patterns**
- `RBAC_GUIDE.md` - **Role-Based Access Control guide**
- `RBAC_QUICK_REFERENCE.md` - RBAC quick reference
- `ENTITY_AUDIT_FIELDS.md` - **Entity audit fields (CreatedAt, UpdatedAt, IsActive)**
