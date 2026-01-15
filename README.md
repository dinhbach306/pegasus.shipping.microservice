# Pegasus Shipping Microservice

A production-ready microservice for handling shipping operations including rate calculation, shipment creation, tracking, and status updates.

## Features

- 🚀 **Rate Calculation**: Calculate shipping rates from multiple carriers
- 📦 **Shipment Management**: Create and manage shipments
- 🔍 **Real-time Tracking**: Track shipments with detailed event history
- ✅ **Input Validation**: Comprehensive validation using Joi
- 🔒 **Security**: Helmet for security headers, CORS support
- 📝 **Logging**: Winston-based structured logging
- 🐳 **Docker Support**: Containerized deployment ready
- ✨ **TypeScript**: Fully typed for better developer experience
- 🧪 **Testing**: Jest-based unit tests with coverage

## Tech Stack

- **Runtime**: Node.js 18+
- **Language**: TypeScript
- **Framework**: Express.js
- **Validation**: Joi
- **Logging**: Winston
- **Testing**: Jest
- **Security**: Helmet, CORS

## Prerequisites

- Node.js 18.x or higher
- npm or yarn
- Docker and Docker Compose (optional)

## Installation

### Local Development

1. Clone the repository:
```bash
git clone https://github.com/dinhbach306/pegasus.shipping.microservice.git
cd pegasus.shipping.microservice
```

2. Install dependencies:
```bash
npm install
```

3. Create environment file:
```bash
cp .env.example .env
```

4. Start the development server:
```bash
npm run dev
```

The service will be available at `http://localhost:3000`

### Docker Deployment

1. Build and run with Docker Compose:
```bash
docker-compose up -d
```

2. View logs:
```bash
docker-compose logs -f
```

3. Stop the service:
```bash
docker-compose down
```

## API Documentation

### Base URL
```
http://localhost:3000
```

### Endpoints

#### 1. Health Check
```http
GET /health
```

**Response:**
```json
{
  "success": true,
  "message": "Shipping microservice is running",
  "timestamp": "2024-01-15T10:00:00.000Z"
}
```

#### 2. Calculate Shipping Rates
```http
POST /api/shipping/rates
```

**Request Body:**
```json
{
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
      "carrier": "Standard Logistics",
      "service": "Ground",
      "cost": 15.75,
      "currency": "USD",
      "estimatedDays": 5
    }
  ]
}
```

#### 3. Create Shipment
```http
POST /api/shipping/shipments
```

**Request Body:** Same as Calculate Rates

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "SHP-1705315200000-abc123",
    "trackingNumber": "ABCD1234EFGH",
    "status": "PENDING",
    "origin": { ... },
    "destination": { ... },
    "package": { ... },
    "createdAt": "2024-01-15T10:00:00.000Z",
    "updatedAt": "2024-01-15T10:00:00.000Z",
    "estimatedDelivery": "2024-01-20T10:00:00.000Z"
  }
}
```

#### 4. Track Shipment
```http
GET /api/shipping/tracking/:trackingNumber
```

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
      }
    ],
    "estimatedDelivery": "2024-01-20T10:00:00.000Z"
  }
}
```

#### 5. Update Shipment Status
```http
PUT /api/shipping/shipments/:trackingNumber/status
```

**Request Body:**
```json
{
  "status": "IN_TRANSIT"
}
```

**Valid Status Values:**
- `PENDING`
- `PICKED_UP`
- `IN_TRANSIT`
- `OUT_FOR_DELIVERY`
- `DELIVERED`
- `FAILED`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "SHP-1705315200000-abc123",
    "trackingNumber": "ABCD1234EFGH",
    "status": "IN_TRANSIT",
    ...
  }
}
```

## Scripts

- `npm run dev` - Start development server with hot reload
- `npm run build` - Build TypeScript to JavaScript
- `npm start` - Start production server
- `npm test` - Run tests
- `npm run test:watch` - Run tests in watch mode
- `npm run test:coverage` - Run tests with coverage report
- `npm run lint` - Lint code
- `npm run lint:fix` - Lint and fix code

## Project Structure

```
pegasus.shipping.microservice/
├── src/
│   ├── config/           # Configuration files
│   ├── controllers/      # Request handlers
│   ├── middleware/       # Express middleware
│   ├── routes/           # API routes
│   ├── services/         # Business logic
│   ├── types/            # TypeScript type definitions
│   ├── utils/            # Utility functions
│   ├── validators/       # Request validation schemas
│   └── index.ts          # Application entry point
├── logs/                 # Log files (generated)
├── dist/                 # Compiled JavaScript (generated)
├── .env.example          # Environment variables template
├── .eslintrc.json        # ESLint configuration
├── .gitignore            # Git ignore rules
├── docker-compose.yml    # Docker Compose configuration
├── Dockerfile            # Docker build instructions
├── jest.config.js        # Jest test configuration
├── package.json          # Project dependencies
├── tsconfig.json         # TypeScript configuration
└── README.md             # This file
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `PORT` | Server port | `3000` |
| `NODE_ENV` | Environment (development/production) | `development` |
| `LOG_LEVEL` | Logging level | `info` |

## Error Handling

All endpoints return consistent error responses:

```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Validation error details"]
}
```

## Security Features

- **Helmet**: Sets security-related HTTP headers
- **CORS**: Configurable cross-origin resource sharing
- **Input Validation**: All inputs validated with Joi
- **Error Sanitization**: Sensitive error details hidden in production
- **Non-root User**: Docker container runs as non-root user

## Testing

Run the test suite:
```bash
npm test
```

Run with coverage:
```bash
npm run test:coverage
```

## Logging

Logs are written to both console and files:
- `logs/all.log` - All logs
- `logs/error.log` - Error logs only

Log format: `YYYY-MM-DD HH:mm:ss:ms [level]: message`

## Performance Considerations

- **In-Memory Storage**: Current implementation uses in-memory storage for simplicity. For production, integrate a database (PostgreSQL, MongoDB, etc.)
- **Rate Limiting**: Consider adding rate limiting middleware for production use
- **Caching**: Implement caching for frequently accessed data
- **Database**: Replace in-memory storage with persistent database

## Future Enhancements

- [ ] Database integration (PostgreSQL/MongoDB)
- [ ] Authentication and authorization
- [ ] Rate limiting
- [ ] Caching layer (Redis)
- [ ] Integration with real carrier APIs
- [ ] Webhook notifications
- [ ] API documentation with Swagger/OpenAPI
- [ ] Monitoring and observability (Prometheus, Grafana)
- [ ] CI/CD pipeline

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

ISC

## Support

For issues and questions, please open an issue on GitHub.