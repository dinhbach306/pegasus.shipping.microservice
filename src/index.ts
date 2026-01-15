import express, { Application } from 'express';
import cors from 'cors';
import helmet from 'helmet';
import config from './config/config';
import logger from './utils/logger';
import shippingRoutes from './routes/shipping.routes';
import healthRoutes from './routes/health.routes';
import { errorHandler } from './middleware/error.middleware';

const app: Application = express();

// Security middleware
app.use(helmet());
app.use(cors());

// Body parsing middleware
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Request logging middleware
app.use((req, _res, next) => {
  logger.http(`${req.method} ${req.path}`);
  next();
});

// Routes
app.use('/health', healthRoutes);
app.use('/api/shipping', shippingRoutes);

// Error handling middleware (must be last)
app.use(errorHandler);

// Start server
const PORT = config.port;

app.listen(PORT, () => {
  logger.info(`🚀 Shipping microservice running on port ${PORT}`);
  logger.info(`Environment: ${config.nodeEnv}`);
});

export default app;
