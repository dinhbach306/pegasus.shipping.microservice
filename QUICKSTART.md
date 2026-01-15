# Quick Start Guide

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Auth0 account (free tier works)
- (Optional) SendGrid account for email
- (Optional) OpenAI API key for Semantic Kernel

## Step 1: Start Infrastructure

```bash
docker compose up -d
```

Wait ~30 seconds for all services to be ready. Verify:

- Identity Database (SQL Server): `localhost:1433`
- Shipping Database (SQL Server): `localhost:1434`
- Kafka: `localhost:29092`
- Kafka UI: http://localhost:8080

**Note:** Each microservice has its own dedicated database instance (Database Per Service pattern).

## Step 2: Configure Auth0

1. Go to https://auth0.com and create a free account
2. Create a new **API**:
   - Name: `Pegasus API`
   - Identifier: `https://pegasus-api` (use this as `Audience`)
3. Create a new **Application** (Regular Web App):
   - Copy `Domain`, `Client ID`, `Client Secret`
4. Enable **Database** connection (Username-Password-Authentication)
5. Enable **Google Social** connection (optional)
6. Add callback URL: `http://localhost:5100/api/identity/callback`

## Step 3: Update Configuration

Update `src/ApiGateway/appsettings.json`:

```json
"Auth0": {
  "Domain": "your-tenant.auth0.com",
  "Audience": "https://pegasus-api"
}
```

Update `src/Services/Identity/Identity.Api/appsettings.json`:

```json
"Auth0": {
  "Domain": "your-tenant.auth0.com",
  "Audience": "https://pegasus-api",
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET",
  "Connection": "Username-Password-Authentication"
}
```

## Step 4: Run Services

Open **3 separate terminals**:

### Terminal 1 - API Gateway

```bash
cd src/ApiGateway
dotnet run
```

Should start on http://localhost:5100

### Terminal 2 - Identity Service

```bash
cd src/Services/Identity/Identity.Api
dotnet run
```

Should start on http://localhost:5000

### Terminal 3 - Shipping Service

```bash
cd src/Services/Shipping/Shipping.Api
dotnet run
```

Should start on http://localhost:5001

## Step 5: Test the System

### 1. Check Gateway Health

```bash
curl http://localhost:5100/health
```

### 2. Check Services Status (Public)

```bash
curl http://localhost:5100/api/shipments/status
```

Should return:

```json
{
  "service": "Shipping API",
  "status": "operational",
  "version": "1.0.0",
  ...
}
```

### 3. Register a User

```bash
curl -X POST http://localhost:5100/api/identity/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "name": "Test User"
  }'
```

Should return tokens:

```json
{
  "accessToken": "eyJ0eXAi...",
  "idToken": "eyJ0eXAi...",
  "tokenType": "Bearer",
  "expiresIn": 86400
}
```

**Save the `accessToken`!**

### 4. Login (Alternative)

```bash
curl -X POST http://localhost:5100/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#"
  }'
```

### 5. Access Protected Endpoint

Replace `YOUR_TOKEN` with the token from step 3 or 4:

```bash
curl -X GET http://localhost:5100/api/identity/me \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Should return:

```json
{
  "userId": "auth0|123456",
  "email": "test@example.com"
}
```

### 6. Create a Shipment (Authenticated)

```bash
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "trackingNumber": "TRACK123"
  }'
```

Should return shipment with creator info:

```json
{
  "shipment": {
    "id": "...",
    "trackingNumber": "TRACK123",
    ...
  },
  "createdBy": {
    "userId": "auth0|123456",
    "email": "test@example.com"
  }
}
```

### 7. Track Shipment (Authenticated)

```bash
curl -X GET http://localhost:5100/api/shipments/TRACK123 \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Step 6: Explore Observability

- **Prometheus**: http://localhost:9090
  - Query: `http_server_duration_milliseconds_sum`
- **Grafana**: http://localhost:3000
  - Default credentials: `admin` / `admin`
  - Add Prometheus, Loki, Tempo datasources
- **Kafka UI**: http://localhost:8080
  - View topics and messages

## Troubleshooting

### "401 Unauthorized" on protected endpoints

- Check that you're using the correct token
- Verify Auth0 `Domain` and `Audience` match in Gateway and Identity configs
- Check token hasn't expired (24 hours by default)

### Gateway can't reach services

- Ensure all 3 services are running
- Check ports: Gateway (5100), Identity (5000), Shipping (5001)
- Verify no firewall blocking localhost connections

### Auth0 registration fails

- Check `ClientId` and `ClientSecret` are correct
- Verify Database connection is enabled in Auth0 dashboard
- Check Auth0 logs in dashboard for error details

### Services can't connect to infrastructure

- Run `docker compose ps` - all services should be "Up"
- SQL Server: `docker logs pegasus-sqlserver`
- Kafka: `docker logs pegasus-kafka`

## Next Steps

1. **Add Database Migrations**:

   ```bash
   cd src/Services/Identity/Identity.Infrastructure
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

2. **Configure SendGrid** for emails in `Identity.Api/appsettings.json`

3. **Configure Semantic Kernel** for AI features in `Shipping.Api/appsettings.json`

4. **Add More Services** following the same pattern:

   - Create Service.Api, Service.Application, Service.Domain, Service.Infrastructure
   - Add routes in `ApiGateway/ocelot.json`
   - Use `HeaderUserContext` to read user info

5. **Review Architecture**: See `ARCHITECTURE.md` for detailed documentation

## Development Tips

- Use **Swagger/OpenAPI**: Available at each service's `/swagger` in Development mode
- **Health Checks**: Each service exposes `/health` endpoint
- **Metrics**: Each service exposes `/metrics` for Prometheus scraping
- **Logs**: Check console output in each terminal
- **Database**: Use SQL Server Management Studio or Azure Data Studio to connect
  - Identity DB: `localhost,1433` (sa / YourStrong!Passw0rd)
  - Shipping DB: `localhost,1434` (sa / YourStrong!Passw0rd)

Enjoy building with Pegasus! 🚀
