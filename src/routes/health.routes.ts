import { Router } from 'express';
import shippingController from '../controllers/shipping.controller';

const router = Router();

/**
 * @route GET /health
 * @desc Health check endpoint
 */
router.get('/', shippingController.healthCheck.bind(shippingController));

export default router;
