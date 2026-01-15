import { Router } from 'express';
import shippingController from '../controllers/shipping.controller';
import { validate } from '../middleware/validator.middleware';
import { shipmentRequestSchema } from '../validators/shipping.validator';
import Joi from 'joi';

const router = Router();

/**
 * @route POST /api/shipping/rates
 * @desc Calculate shipping rates
 */
router.post(
  '/rates',
  validate(shipmentRequestSchema),
  shippingController.calculateRates.bind(shippingController)
);

/**
 * @route POST /api/shipping/shipments
 * @desc Create a new shipment
 */
router.post(
  '/shipments',
  validate(shipmentRequestSchema),
  shippingController.createShipment.bind(shippingController)
);

/**
 * @route GET /api/shipping/tracking/:trackingNumber
 * @desc Get tracking information
 */
router.get(
  '/tracking/:trackingNumber',
  shippingController.getTracking.bind(shippingController)
);

/**
 * @route PUT /api/shipping/shipments/:trackingNumber/status
 * @desc Update shipment status
 */
router.put(
  '/shipments/:trackingNumber/status',
  validate(
    Joi.object({
      status: Joi.string()
        .valid('PENDING', 'PICKED_UP', 'IN_TRANSIT', 'OUT_FOR_DELIVERY', 'DELIVERED', 'FAILED')
        .required(),
    })
  ),
  shippingController.updateStatus.bind(shippingController)
);

export default router;
