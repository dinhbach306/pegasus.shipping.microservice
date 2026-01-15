import { Router } from 'express';
import shippingController from '../controllers/shipping.controller';
import { validate } from '../middleware/validator.middleware';
import { validateParams } from '../middleware/paramValidator.middleware';
import {
  shipmentRequestSchema,
  trackingNumberSchema,
} from '../validators/shipping.validator';
import { ShipmentStatus } from '../types/shipping.types';
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
  validateParams(
    Joi.object({
      trackingNumber: trackingNumberSchema,
    })
  ),
  shippingController.getTracking.bind(shippingController)
);

/**
 * @route PUT /api/shipping/shipments/:trackingNumber/status
 * @desc Update shipment status
 */
router.put(
  '/shipments/:trackingNumber/status',
  validateParams(
    Joi.object({
      trackingNumber: trackingNumberSchema,
    })
  ),
  validate(
    Joi.object({
      status: Joi.string()
        .valid(...Object.values(ShipmentStatus))
        .required(),
    })
  ),
  shippingController.updateStatus.bind(shippingController)
);

export default router;
