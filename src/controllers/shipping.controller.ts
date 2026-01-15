import { Request, Response } from 'express';
import shippingService from '../services/shipping.service';
import { ShipmentRequest, ShipmentStatus } from '../types/shipping.types';
import logger from '../utils/logger';

export class ShippingController {
  /**
   * Calculate shipping rates
   */
  calculateRates(req: Request, res: Response): void {
    try {
      const request: ShipmentRequest = req.body;
      const rates = shippingService.calculateRates(request);
      
      res.status(200).json({
        success: true,
        data: rates,
      });
    } catch (error) {
      logger.error(`Error calculating rates: ${error}`);
      res.status(500).json({
        success: false,
        message: 'Failed to calculate shipping rates',
      });
    }
  }

  /**
   * Create a new shipment
   */
  createShipment(req: Request, res: Response): void {
    try {
      const request: ShipmentRequest = req.body;
      const shipment = shippingService.createShipment(request);
      
      res.status(201).json({
        success: true,
        data: shipment,
      });
    } catch (error) {
      logger.error(`Error creating shipment: ${error}`);
      res.status(500).json({
        success: false,
        message: 'Failed to create shipment',
      });
    }
  }

  /**
   * Get tracking information
   */
  getTracking(req: Request, res: Response): void {
    try {
      const { trackingNumber } = req.params;
      const trackingInfo = shippingService.getTrackingInfo(trackingNumber);
      
      if (!trackingInfo) {
        res.status(404).json({
          success: false,
          message: 'Tracking number not found',
        });
        return;
      }
      
      res.status(200).json({
        success: true,
        data: trackingInfo,
      });
    } catch (error) {
      logger.error(`Error fetching tracking info: ${error}`);
      res.status(500).json({
        success: false,
        message: 'Failed to fetch tracking information',
      });
    }
  }

  /**
   * Update shipment status
   */
  updateStatus(req: Request, res: Response): void {
    try {
      const { trackingNumber } = req.params;
      const { status } = req.body;
      
      if (!Object.values(ShipmentStatus).includes(status)) {
        res.status(400).json({
          success: false,
          message: 'Invalid shipment status',
        });
        return;
      }
      
      const shipment = shippingService.updateShipmentStatus(
        trackingNumber,
        status
      );
      
      if (!shipment) {
        res.status(404).json({
          success: false,
          message: 'Shipment not found',
        });
        return;
      }
      
      res.status(200).json({
        success: true,
        data: shipment,
      });
    } catch (error) {
      logger.error(`Error updating shipment status: ${error}`);
      res.status(500).json({
        success: false,
        message: 'Failed to update shipment status',
      });
    }
  }

  /**
   * Health check endpoint
   */
  healthCheck(_req: Request, res: Response): void {
    res.status(200).json({
      success: true,
      message: 'Shipping microservice is running',
      timestamp: new Date().toISOString(),
    });
  }
}

export default new ShippingController();
