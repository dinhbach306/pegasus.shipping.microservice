import {
  ShipmentRequest,
  Shipment,
  ShippingRate,
  TrackingInfo,
  ShipmentStatus,
  TrackingEvent,
} from '../types/shipping.types';
import logger from '../utils/logger';

export class ShippingService {
  private shipments: Map<string, Shipment> = new Map();

  /**
   * Calculate shipping rates based on origin, destination, and package details
   */
  calculateRates(request: ShipmentRequest): ShippingRate[] {
    logger.info('Calculating shipping rates');
    
    const volumetricWeight = this.calculateVolumetricWeight(request.package);
    const chargeableWeight = Math.max(request.package.weight, volumetricWeight);
    const distance = this.calculateDistance(request.origin, request.destination);
    
    // Base rates from different carriers
    const rates: ShippingRate[] = [
      {
        carrier: 'FastShip Express',
        service: 'Next Day',
        cost: this.calculateCost(chargeableWeight, distance, 1.5),
        currency: 'USD',
        estimatedDays: 1,
      },
      {
        carrier: 'FastShip Express',
        service: '2-Day',
        cost: this.calculateCost(chargeableWeight, distance, 1.2),
        currency: 'USD',
        estimatedDays: 2,
      },
      {
        carrier: 'Standard Logistics',
        service: 'Ground',
        cost: this.calculateCost(chargeableWeight, distance, 0.8),
        currency: 'USD',
        estimatedDays: 5,
      },
      {
        carrier: 'Economy Shipping',
        service: 'Standard',
        cost: this.calculateCost(chargeableWeight, distance, 0.5),
        currency: 'USD',
        estimatedDays: 7,
      },
    ];

    return rates;
  }

  /**
   * Create a new shipment
   */
  createShipment(request: ShipmentRequest): Shipment {
    logger.info('Creating new shipment');
    
    const id = this.generateId();
    const trackingNumber = this.generateTrackingNumber();
    const now = new Date();
    
    const shipment: Shipment = {
      id,
      trackingNumber,
      status: ShipmentStatus.PENDING,
      origin: request.origin,
      destination: request.destination,
      package: request.package,
      createdAt: now,
      updatedAt: now,
      estimatedDelivery: this.calculateEstimatedDelivery(5),
    };

    this.shipments.set(trackingNumber, shipment);
    logger.info(`Shipment created with tracking number: ${trackingNumber}`);
    
    return shipment;
  }

  /**
   * Get tracking information for a shipment
   */
  getTrackingInfo(trackingNumber: string): TrackingInfo | null {
    logger.info(`Fetching tracking info for: ${trackingNumber}`);
    
    const shipment = this.shipments.get(trackingNumber);
    
    if (!shipment) {
      logger.warn(`Shipment not found: ${trackingNumber}`);
      return null;
    }

    const events = this.generateTrackingEvents(shipment);
    
    return {
      trackingNumber: shipment.trackingNumber,
      status: shipment.status,
      events,
      estimatedDelivery: shipment.estimatedDelivery,
    };
  }

  /**
   * Update shipment status
   */
  updateShipmentStatus(
    trackingNumber: string,
    status: ShipmentStatus
  ): Shipment | null {
    logger.info(`Updating shipment status: ${trackingNumber} to ${status}`);
    
    const shipment = this.shipments.get(trackingNumber);
    
    if (!shipment) {
      logger.warn(`Shipment not found: ${trackingNumber}`);
      return null;
    }

    shipment.status = status;
    shipment.updatedAt = new Date();
    
    this.shipments.set(trackingNumber, shipment);
    
    return shipment;
  }

  /**
   * Calculate volumetric weight (length × width × height / 5000)
   */
  private calculateVolumetricWeight(pkg: {
    length: number;
    width: number;
    height: number;
  }): number {
    return (pkg.length * pkg.width * pkg.height) / 5000;
  }

  /**
   * Calculate approximate distance between two addresses (simplified)
   */
  private calculateDistance(
    origin: { zipCode: string },
    destination: { zipCode: string }
  ): number {
    // Simplified distance calculation based on zip code difference
    const originZip = parseInt(origin.zipCode.substring(0, 5));
    const destZip = parseInt(destination.zipCode.substring(0, 5));
    const zipDiff = Math.abs(originZip - destZip);
    
    // Approximate: each zip code unit represents ~10km
    return zipDiff * 10;
  }

  /**
   * Calculate shipping cost based on weight, distance, and rate multiplier
   */
  private calculateCost(
    weight: number,
    distance: number,
    rateMultiplier: number
  ): number {
    const baseCost = 5.0;
    const weightCost = weight * 0.5;
    const distanceCost = distance * 0.01;
    
    const totalCost = (baseCost + weightCost + distanceCost) * rateMultiplier;
    
    return Math.round(totalCost * 100) / 100;
  }

  /**
   * Generate a unique shipment ID
   */
  private generateId(): string {
    return `SHP-${Date.now()}-${Math.random().toString(36).substring(7)}`;
  }

  /**
   * Generate a tracking number
   */
  private generateTrackingNumber(): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let trackingNumber = '';
    
    for (let i = 0; i < 12; i++) {
      trackingNumber += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    
    return trackingNumber;
  }

  /**
   * Calculate estimated delivery date
   */
  private calculateEstimatedDelivery(daysToAdd: number): Date {
    const date = new Date();
    date.setDate(date.getDate() + daysToAdd);
    return date;
  }

  /**
   * Generate tracking events for a shipment
   */
  private generateTrackingEvents(shipment: Shipment): TrackingEvent[] {
    const events: TrackingEvent[] = [
      {
        timestamp: shipment.createdAt,
        location: `${shipment.origin.city}, ${shipment.origin.state}`,
        status: ShipmentStatus.PENDING,
        description: 'Shipment information received',
      },
    ];

    if (shipment.status !== ShipmentStatus.PENDING) {
      events.push({
        timestamp: new Date(shipment.createdAt.getTime() + 3600000),
        location: `${shipment.origin.city}, ${shipment.origin.state}`,
        status: ShipmentStatus.PICKED_UP,
        description: 'Package picked up',
      });
    }

    if (
      shipment.status === ShipmentStatus.IN_TRANSIT ||
      shipment.status === ShipmentStatus.OUT_FOR_DELIVERY ||
      shipment.status === ShipmentStatus.DELIVERED
    ) {
      events.push({
        timestamp: new Date(shipment.createdAt.getTime() + 7200000),
        location: 'Distribution Center',
        status: ShipmentStatus.IN_TRANSIT,
        description: 'Package in transit',
      });
    }

    if (
      shipment.status === ShipmentStatus.OUT_FOR_DELIVERY ||
      shipment.status === ShipmentStatus.DELIVERED
    ) {
      events.push({
        timestamp: new Date(shipment.createdAt.getTime() + 86400000),
        location: `${shipment.destination.city}, ${shipment.destination.state}`,
        status: ShipmentStatus.OUT_FOR_DELIVERY,
        description: 'Out for delivery',
      });
    }

    if (shipment.status === ShipmentStatus.DELIVERED) {
      events.push({
        timestamp: shipment.updatedAt,
        location: `${shipment.destination.street}, ${shipment.destination.city}`,
        status: ShipmentStatus.DELIVERED,
        description: 'Package delivered',
      });
    }

    return events;
  }
}

export default new ShippingService();
