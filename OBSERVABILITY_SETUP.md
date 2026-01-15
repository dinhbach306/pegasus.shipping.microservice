# Observability Setup Guide

## Overview

This project uses the **LGTM Stack** (Loki, Grafana, Tempo, Mimir/Prometheus) for full observability:

- **Prometheus** - Metrics collection and storage
- **Grafana** - Visualization and dashboards
- **Loki** - Log aggregation
- **Tempo** - Distributed tracing
- **OpenTelemetry Collector** - Telemetry pipeline
- **Promtail** - Log shipper for Loki

## Architecture

```
┌────────────────────────────────────────────────────────────┐
│  Microservices (.NET APIs)                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                 │
│  │Identity  │  │Shipping  │  │Gateway   │                 │
│  │   API    │  │   API    │  │          │                 │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘                 │
│       │             │             │                        │
│       │ Metrics     │ Traces      │ Logs                  │
│       └─────────────┴─────────────┘                        │
│                     │                                      │
└─────────────────────┼──────────────────────────────────────┘
                      ▼
         ┌────────────────────────┐
         │ OpenTelemetry Collector│
         │  :4317 (gRPC)          │
         │  :4318 (HTTP)          │
         └──┬──────────────────┬──┘
            │                  │
    Metrics │                  │ Traces
            ▼                  ▼
    ┌──────────────┐   ┌──────────────┐
    │ Prometheus   │   │    Tempo     │
    │   :9090      │   │    :3200     │
    └──────┬───────┘   └──────┬───────┘
           │                  │
           └────────┬─────────┘
                    ▼
            ┌───────────────┐
            │   Grafana     │
            │    :3000      │
            └───────────────┘
                    ▲
                    │
    ┌───────────────┴────────────────┐
    │                                │
┌───┴────┐                    ┌──────┴──────┐
│  Loki  │◄──────────────────┤  Promtail   │
│ :3100  │    Logs Push       │             │
└────────┘                    └─────────────┘
                                     ▲
                                     │
                          ┌──────────┴──────────┐
                          │ Docker Containers   │
                          │ (collect logs via   │
                          │  Docker socket)     │
                          └─────────────────────┘
```

## Components

### 1. OpenTelemetry Collector

**Purpose:** Central telemetry pipeline that receives metrics and traces from services

**Ports:**

- `4317` - gRPC endpoint (OTLP)
- `4318` - HTTP endpoint (OTLP)
- `8889` - Prometheus metrics endpoint

**Configuration:** `deploy/otel-collector-config.yaml`

**Pipelines:**

- **Metrics**: OTLP → Batch → Prometheus
- **Traces**: OTLP → Batch → Tempo

**Note:** Logs pipeline is handled by Promtail directly from Docker containers (standard Loki approach)

### 2. Prometheus

**Purpose:** Metrics storage and querying

**Port:** `9090`

**What it collects:**

- HTTP request durations
- Database query times
- Kafka message throughput
- Custom business metrics

**Access:** http://localhost:9090

**Example Queries:**

```promql
# Request rate per service
rate(http_server_duration_count[5m])

# Database query duration (95th percentile)
histogram_quantile(0.95, rate(db_query_duration_bucket[5m]))

# Error rate
rate(http_server_duration_count{http_response_status_code=~"5.."}[5m])
```

### 3. Loki

**Purpose:** Log aggregation and querying

**Port:** `3100`

**What it stores:**

- Application logs (via Promtail)
- Container logs
- Structured logs with labels

**Log Labels (automatically added by Promtail):**

- `container` - Container name
- `service` - Docker Compose service name
- `container_id` - Docker container ID
- `stream` - stdout/stderr

**Example LogQL Queries:**

```logql
# All logs from Shipping service
{service="shipping-api"}

# Error logs from all services
{service=~".+"} |= "error" or "Error" or "ERROR"

# Logs with specific tracking number
{service="shipping-api"} |= "TRACK123"

# Rate of error logs
rate({service="shipping-api"} |= "error" [5m])
```

### 4. Promtail

**Purpose:** Log shipper that collects logs from Docker containers and sends to Loki

**Configuration:** `deploy/promtail-config.yml`

**How it works:**

1. Connects to Docker socket (`/var/run/docker.sock`)
2. Discovers running containers
3. Reads logs from `/var/lib/docker/containers`
4. Adds labels (service, container, etc.)
5. Pushes logs to Loki HTTP endpoint

**Filtering:**
Only collects logs from containers matching:

- `pegasus-*` (infrastructure)
- `identity-api`
- `shipping-api`
- `api-gateway`

### 5. Tempo

**Purpose:** Distributed tracing backend

**Ports:**

- `3200` - HTTP API
- `4319` - OTLP gRPC (mapped from 4317)

**What it stores:**

- Trace spans from services
- Distributed traces across microservices
- Request flow visualization

**Trace Example:**

```
TraceID: abc123
│
├─ Span: Gateway.ReceiveRequest (50ms)
│  └─ Span: Shipping.CreateShipment (45ms)
│     ├─ Span: Database.Insert (20ms)
│     └─ Span: Kafka.Publish (15ms)
```

### 6. Grafana

**Purpose:** Visualization and dashboards

**Port:** `3000`

**Access:** http://localhost:3000

- Username: `admin`
- Password: `admin` (change on first login)

**Pre-configured Data Sources:**

- Prometheus (metrics)
- Loki (logs)
- Tempo (traces)

**Dashboard Examples:**

**API Performance Dashboard:**

- Request rate
- Response times (p50, p95, p99)
- Error rate
- Database query duration

**Service Health Dashboard:**

- CPU/Memory usage
- Kafka lag
- Database connections
- Active requests

**Business Metrics Dashboard:**

- Shipments created per hour
- User registrations
- Top errors

## Setup Instructions

### 1. Start All Services

```bash
docker compose up -d
```

### 2. Verify Services Are Running

```bash
docker ps
```

You should see:

- `pegasus-otel-collector`
- `pegasus-prometheus`
- `pegasus-loki`
- `pegasus-promtail`
- `pegasus-tempo`
- `pegasus-grafana`

### 3. Check Logs

```bash
# OTEL Collector
docker logs pegasus-otel-collector

# Loki
docker logs pegasus-loki

# Promtail
docker logs pegasus-promtail

# Should see: "Promtail starting" and "clients configured"
```

### 4. Configure Grafana

1. Open http://localhost:3000
2. Login: `admin` / `admin`
3. Go to **Configuration** → **Data Sources**
4. Add data sources:

**Prometheus:**

```
URL: http://prometheus:9090
```

**Loki:**

```
URL: http://loki:3100
```

**Tempo:**

```
URL: http://tempo:3200
```

### 5. Create Dashboard

1. Go to **Dashboards** → **New** → **New Dashboard**
2. Add panels:

**Panel 1: Request Rate**

- Data source: Prometheus
- Query: `rate(http_server_duration_count[5m])`
- Legend: `{{service}}`

**Panel 2: Response Time**

- Data source: Prometheus
- Query: `histogram_quantile(0.95, rate(http_server_duration_bucket[5m]))`

**Panel 3: Logs**

- Data source: Loki
- Query: `{service=~".+"}`

**Panel 4: Traces**

- Data source: Tempo
- Query: TraceID from logs

## Application Integration

### .NET Service Configuration

Services are already configured in `ServiceDefaults`:

```csharp
// In ServiceDefaultsExtensions.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Shipping.Api")
            .AddSource("Identity.Api")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
    });
```

### Environment Variables

```bash
# In appsettings.json or environment
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=shipping-api
```

## Querying Data

### Prometheus Queries (PromQL)

```promql
# Request rate
rate(http_server_duration_count[5m])

# Error rate
sum(rate(http_server_duration_count{http_response_status_code=~"5.."}[5m])) by (service)

# 95th percentile latency
histogram_quantile(0.95, rate(http_server_duration_bucket[5m]))

# Active database connections
db_client_connections_usage
```

### Loki Queries (LogQL)

```logql
# All logs from Shipping service
{service="shipping-api"}

# Errors only
{service="shipping-api"} |= "error"

# JSON logs parsed
{service="shipping-api"} | json | level="Error"

# Count errors per minute
sum(rate({service="shipping-api"} |= "error" [1m]))

# Logs with specific TraceID
{service=~".+"} |= "TraceId: abc123"
```

### Tempo Queries (TraceQL)

In Grafana, go to **Explore** → **Tempo**:

```
# Find traces by service
{ service.name = "shipping-api" }

# Find slow traces
{ duration > 1s }

# Find traces with errors
{ status = error }
```

## Troubleshooting

### OTEL Collector Not Receiving Data

**Check 1: Endpoint configuration**

```bash
# In service appsettings.json
"OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
```

**Check 2: Collector logs**

```bash
docker logs pegasus-otel-collector
```

**Check 3: Test endpoint**

```bash
curl http://localhost:4318/v1/metrics
```

### Promtail Not Collecting Logs

**Check 1: Promtail is running**

```bash
docker ps | grep promtail
docker logs pegasus-promtail
```

**Check 2: Docker socket mounted**

```bash
docker exec pegasus-promtail ls /var/run/docker.sock
```

**Check 3: Promtail targets**

```bash
curl http://localhost:9080/targets
```

### Loki Not Receiving Logs

**Check 1: Loki is running**

```bash
docker logs pegasus-loki
```

**Check 2: Query Loki directly**

```bash
curl http://localhost:3100/ready
curl -G -s "http://localhost:3100/loki/api/v1/query" --data-urlencode 'query={service="shipping-api"}'
```

**Check 3: Promtail can reach Loki**

```bash
docker exec pegasus-promtail wget -O- http://loki:3100/ready
```

### No Metrics in Prometheus

**Check 1: Prometheus targets**

Go to http://localhost:9090/targets

Should see:

- `otel-collector` - UP

**Check 2: Service is exporting metrics**

```bash
curl http://localhost:5100/metrics
```

**Check 3: OTEL Collector Prometheus exporter**

```bash
curl http://localhost:8889/metrics
```

### No Traces in Tempo

**Check 1: Tempo is running**

```bash
docker logs pegasus-tempo
```

**Check 2: OTEL Collector sending to Tempo**

```bash
docker logs pegasus-otel-collector | grep tempo
```

**Check 3: Query Tempo**

```bash
curl http://localhost:3200/api/search
```

## Best Practices

### 1. Structured Logging

```csharp
// Good: Structured
_logger.LogInformation(
    "Shipment created: {ShipmentId}, Tracking: {TrackingNumber}",
    shipment.Id,
    shipment.TrackingNumber);

// Bad: String interpolation
_logger.LogInformation($"Shipment created: {shipment.Id}");
```

### 2. Add Trace Context to Logs

```csharp
using var activity = Activity.Current;
_logger.LogInformation(
    "Processing request. TraceId: {TraceId}, SpanId: {SpanId}",
    activity?.TraceId,
    activity?.SpanId);
```

### 3. Custom Metrics

```csharp
private static readonly Counter ShipmentsCreated = Metrics.CreateCounter(
    "shipments_created_total",
    "Total number of shipments created");

public async Task CreateShipment(...)
{
    // ... business logic ...
    ShipmentsCreated.Inc();
}
```

### 4. Add Labels to Logs

Configure structured logging in `Program.cs`:

```csharp
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.JsonWriterOptions = new JsonWriterOptions
    {
        Indented = false
    };
});
```

### 5. Correlation IDs

Already implemented in API Gateway (forwards via `X-Correlation-Id` header)

## Alerting (Future Enhancement)

Configure alerts in Grafana:

**High Error Rate:**

```promql
rate(http_server_duration_count{http_response_status_code=~"5.."}[5m]) > 10
```

**Slow Responses:**

```promql
histogram_quantile(0.95, rate(http_server_duration_bucket[5m])) > 1
```

**High Memory Usage:**

```promql
process_working_set_bytes > 1073741824  # 1GB
```

## Ports Summary

| Service        | Port | Purpose            |
| -------------- | ---- | ------------------ |
| OTEL Collector | 4317 | OTLP gRPC          |
| OTEL Collector | 4318 | OTLP HTTP          |
| OTEL Collector | 8889 | Prometheus metrics |
| Prometheus     | 9090 | Web UI & API       |
| Loki           | 3100 | HTTP API           |
| Promtail       | 9080 | Internal metrics   |
| Tempo          | 3200 | HTTP API           |
| Tempo          | 4319 | OTLP gRPC          |
| Grafana        | 3000 | Web UI             |

## Key Files

- `docker-compose.yml` - Service definitions
- `deploy/otel-collector-config.yaml` - OTEL pipeline
- `deploy/prometheus.yml` - Prometheus scrape config
- `deploy/loki-config.yml` - Loki storage config
- `deploy/promtail-config.yml` - Log collection config
- `deploy/tempo-config.yml` - Tempo storage config
- `src/BuildingBlocks/ServiceDefaults/ServiceDefaultsExtensions.cs` - .NET OTEL setup

## References

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Prometheus Docs](https://prometheus.io/docs/)
- [Loki Docs](https://grafana.com/docs/loki/latest/)
- [Tempo Docs](https://grafana.com/docs/tempo/latest/)
- [Grafana Docs](https://grafana.com/docs/grafana/latest/)
