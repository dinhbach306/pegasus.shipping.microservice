# API Documentation

## Overview

The Pegasus Shipping Microservice provides RESTful APIs for managing shipping operations.

## Authentication

Currently, the API does not require authentication. Future versions will include JWT-based authentication.

## Response Format

All API responses follow a consistent format:

### Success Response
```json
{
  "success": true,
  "data": { ... }
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Details..."]
}
```

## HTTP Status Codes

- `200 OK` - Successful GET/PUT request
- `201 Created` - Successful POST request
- `400 Bad Request` - Invalid request data
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Rate Limits

Currently no rate limits. Consider implementing rate limiting in production.

## Endpoints

### Health Check

Check if the service is running.

**Endpoint:** `GET /health`

**Response:**
```json
{
  "success": true,
  "message": "Shipping microservice is running",
  "timestamp": "2024-01-15T10:00:00.000Z"
}
```

---

### Calculate Shipping Rates

Calculate shipping rates for a package from origin to destination.

**Endpoint:** `POST /api/shipping/rates`

**Request Body:**
```json
{
  "origin": {
    "street": "string (1-200 chars)",
    "city": "string (1-100 chars)",
    "state": "string (2-100 chars)",
    "zipCode": "string (5 or 9 digits, format: 12345 or 12345-6789)",
    "country": "string (2 char ISO code, e.g., US)"
  },
  "destination": {
    "street": "string",
    "city": "string",
    "state": "string",
    "zipCode": "string",
    "country": "string"
  },
  "package": {
    "weight": "number (kg, max: 1000)",
    "length": "number (cm, max: 500)",
    "width": "number (cm, max: 500)",
    "height": "number (cm, max: 500)"
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "carrier": "FastShip Express",
      "service": "Next Day",
      "cost": 45.50,
      "currency": "USD",
      "estimatedDays": 1
    },
    {
      "carrier": "FastShip Express",
      "service": "2-Day",
      "cost": 32.00,
      "currency": "USD",
      "estimatedDays": 2
    },
    {
      "carrier": "Standard Logistics",
      "service": "Ground",
      "cost": 15.75,
      "currency": "USD",
      "estimatedDays": 5
    },
    {
      "carrier": "Economy Shipping",
      "service": "Standard",
      "cost": 9.99,
      "currency": "USD",
      "estimatedDays": 7
    }
  ]
}
```

---

### Create Shipment

Create a new shipment and generate a tracking number.

**Endpoint:** `POST /api/shipping/shipments`

**Request Body:** Same as Calculate Rates

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "SHP-1705315200000-abc123",
    "trackingNumber": "ABCD1234EFGH",
    "status": "PENDING",
    "origin": {
      "street": "123 Main St",
      "city": "New York",
      "state": "NY",
      "zipCode": "10001",
      "country": "US"
    },
    "destination": {
      "street": "456 Oak Ave",
      "city": "Los Angeles",
      "state": "CA",
      "zipCode": "90001",
      "country": "US"
    },
    "package": {
      "weight": 5,
      "length": 30,
      "width": 20,
      "height": 15
    },
    "createdAt": "2024-01-15T10:00:00.000Z",
    "updatedAt": "2024-01-15T10:00:00.000Z",
    "estimatedDelivery": "2024-01-20T10:00:00.000Z"
  }
}
```

---

### Track Shipment

Get detailed tracking information for a shipment.

**Endpoint:** `GET /api/shipping/tracking/:trackingNumber`

**Parameters:**
- `trackingNumber` (path) - Tracking number (10-20 alphanumeric characters)

**Response:**
```json
{
  "success": true,
  "data": {
    "trackingNumber": "ABCD1234EFGH",
    "status": "IN_TRANSIT",
    "events": [
      {
        "timestamp": "2024-01-15T10:00:00.000Z",
        "location": "New York, NY",
        "status": "PENDING",
        "description": "Shipment information received"
      },
      {
        "timestamp": "2024-01-15T11:00:00.000Z",
        "location": "New York, NY",
        "status": "PICKED_UP",
        "description": "Package picked up"
      },
      {
        "timestamp": "2024-01-15T12:00:00.000Z",
        "location": "Distribution Center",
        "status": "IN_TRANSIT",
        "description": "Package in transit"
      }
    ],
    "estimatedDelivery": "2024-01-20T10:00:00.000Z"
  }
}
```

**Error Response (404):**
```json
{
  "success": false,
  "message": "Tracking number not found"
}
```

---

### Update Shipment Status

Update the status of an existing shipment.

**Endpoint:** `PUT /api/shipping/shipments/:trackingNumber/status`

**Parameters:**
- `trackingNumber` (path) - Tracking number

**Request Body:**
```json
{
  "status": "IN_TRANSIT"
}
```

**Valid Status Values:**
- `PENDING` - Shipment information received
- `PICKED_UP` - Package picked up from origin
- `IN_TRANSIT` - Package is in transit
- `OUT_FOR_DELIVERY` - Package is out for delivery
- `DELIVERED` - Package has been delivered
- `FAILED` - Delivery failed

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "SHP-1705315200000-abc123",
    "trackingNumber": "ABCD1234EFGH",
    "status": "IN_TRANSIT",
    "origin": { ... },
    "destination": { ... },
    "package": { ... },
    "createdAt": "2024-01-15T10:00:00.000Z",
    "updatedAt": "2024-01-15T12:30:00.000Z",
    "estimatedDelivery": "2024-01-20T10:00:00.000Z"
  }
}
```

**Error Responses:**

404 - Shipment not found:
```json
{
  "success": false,
  "message": "Shipment not found"
}
```

400 - Invalid status:
```json
{
  "success": false,
  "message": "Invalid shipment status"
}
```

---

## Validation Rules

### Address Validation
- `street`: Required, 1-200 characters
- `city`: Required, 1-100 characters
- `state`: Required, 2-100 characters
- `zipCode`: Required, US format (12345 or 12345-6789)
- `country`: Required, 2-character ISO code, uppercase

### Package Validation
- `weight`: Required, positive number, max 1000 kg
- `length`: Required, positive number, max 500 cm
- `width`: Required, positive number, max 500 cm
- `height`: Required, positive number, max 500 cm

### Tracking Number Format
- 10-20 alphanumeric characters (A-Z, 0-9)

---

## Example Usage

### Using cURL

**Calculate Rates:**
```bash
curl -X POST http://localhost:3000/api/shipping/rates \
  -H "Content-Type: application/json" \
  -d '{
    "origin": {
      "street": "123 Main St",
      "city": "New York",
      "state": "NY",
      "zipCode": "10001",
      "country": "US"
    },
    "destination": {
      "street": "456 Oak Ave",
      "city": "Los Angeles",
      "state": "CA",
      "zipCode": "90001",
      "country": "US"
    },
    "package": {
      "weight": 5,
      "length": 30,
      "width": 20,
      "height": 15
    }
  }'
```

**Create Shipment:**
```bash
curl -X POST http://localhost:3000/api/shipping/shipments \
  -H "Content-Type: application/json" \
  -d '{ ... }' # Same body as above
```

**Track Shipment:**
```bash
curl http://localhost:3000/api/shipping/tracking/ABCD1234EFGH
```

**Update Status:**
```bash
curl -X PUT http://localhost:3000/api/shipping/shipments/ABCD1234EFGH/status \
  -H "Content-Type: application/json" \
  -d '{"status": "IN_TRANSIT"}'
```

---

## Error Codes

| HTTP Status | Message | Cause |
|-------------|---------|-------|
| 400 | Validation error | Invalid request data format |
| 404 | Tracking number not found | Tracking number doesn't exist |
| 404 | Shipment not found | Shipment doesn't exist |
| 500 | Internal server error | Server-side error occurred |

---

## Changelog

### Version 1.0.0 (2024-01-15)
- Initial release
- Rate calculation endpoint
- Shipment creation endpoint
- Tracking endpoint
- Status update endpoint
- Health check endpoint
