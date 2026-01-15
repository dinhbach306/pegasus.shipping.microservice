import { ShippingService } from '../services/shipping.service';
import { ShipmentRequest, ShipmentStatus } from '../types/shipping.types';

describe('ShippingService', () => {
  let service: ShippingService;

  beforeEach(() => {
    service = new ShippingService();
  });

  describe('calculateRates', () => {
    it('should return shipping rates for a valid request', () => {
      const request: ShipmentRequest = {
        origin: {
          street: '123 Main St',
          city: 'New York',
          state: 'NY',
          zipCode: '10001',
          country: 'US',
        },
        destination: {
          street: '456 Oak Ave',
          city: 'Los Angeles',
          state: 'CA',
          zipCode: '90001',
          country: 'US',
        },
        package: {
          weight: 5,
          length: 30,
          width: 20,
          height: 15,
        },
      };

      const rates = service.calculateRates(request);

      expect(rates).toHaveLength(4);
      expect(rates[0]).toHaveProperty('carrier');
      expect(rates[0]).toHaveProperty('service');
      expect(rates[0]).toHaveProperty('cost');
      expect(rates[0]).toHaveProperty('currency', 'USD');
      expect(rates[0]).toHaveProperty('estimatedDays');
      expect(rates[0].cost).toBeGreaterThan(0);
    });

    it('should return rates sorted by speed', () => {
      const request: ShipmentRequest = {
        origin: {
          street: '123 Main St',
          city: 'Boston',
          state: 'MA',
          zipCode: '02101',
          country: 'US',
        },
        destination: {
          street: '789 Pine Rd',
          city: 'Miami',
          state: 'FL',
          zipCode: '33101',
          country: 'US',
        },
        package: {
          weight: 10,
          length: 40,
          width: 30,
          height: 25,
        },
      };

      const rates = service.calculateRates(request);

      expect(rates[0].estimatedDays).toBeLessThan(rates[rates.length - 1].estimatedDays);
      expect(rates[0].cost).toBeGreaterThan(rates[rates.length - 1].cost);
    });
  });

  describe('createShipment', () => {
    it('should create a new shipment with tracking number', () => {
      const request: ShipmentRequest = {
        origin: {
          street: '123 Main St',
          city: 'Chicago',
          state: 'IL',
          zipCode: '60601',
          country: 'US',
        },
        destination: {
          street: '456 Elm St',
          city: 'Houston',
          state: 'TX',
          zipCode: '77001',
          country: 'US',
        },
        package: {
          weight: 3,
          length: 25,
          width: 15,
          height: 10,
        },
      };

      const shipment = service.createShipment(request);

      expect(shipment).toHaveProperty('id');
      expect(shipment).toHaveProperty('trackingNumber');
      expect(shipment.trackingNumber).toMatch(/^[A-Z0-9]{12}$/);
      expect(shipment.status).toBe(ShipmentStatus.PENDING);
      expect(shipment.origin).toEqual(request.origin);
      expect(shipment.destination).toEqual(request.destination);
      expect(shipment.package).toEqual(request.package);
    });

    it('should generate unique tracking numbers', () => {
      const request: ShipmentRequest = {
        origin: {
          street: '123 Main St',
          city: 'Seattle',
          state: 'WA',
          zipCode: '98101',
          country: 'US',
        },
        destination: {
          street: '789 Maple Dr',
          city: 'Portland',
          state: 'OR',
          zipCode: '97201',
          country: 'US',
        },
        package: {
          weight: 2,
          length: 20,
          width: 15,
          height: 10,
        },
      };

      const shipment1 = service.createShipment(request);
      const shipment2 = service.createShipment(request);

      expect(shipment1.trackingNumber).not.toBe(shipment2.trackingNumber);
    });
  });

  describe('getTrackingInfo', () => {
    it('should return tracking info for existing shipment', () => {
      const request: ShipmentRequest = {
        origin: {
          street: '123 Main St',
          city: 'Denver',
          state: 'CO',
          zipCode: '80201',
          country: 'US',
        },
        destination: {
          street: '456 Cedar Ln',
          city: 'Phoenix',
          state: 'AZ',
          zipCode: '85001',
          country: 'US',
        },
        package: {
          weight: 7,
          length: 35,
          width: 25,
          height: 20,
        },
      };

      const shipment = service.createShipment(request);
      const trackingInfo = service.getTrackingInfo(shipment.trackingNumber);

      expect(trackingInfo).not.toBeNull();
      expect(trackingInfo?.trackingNumber).toBe(shipment.trackingNumber);
      expect(trackingInfo?.status).toBe(ShipmentStatus.PENDING);
      expect(trackingInfo?.events).toHaveLength(1);
      expect(trackingInfo?.events[0].status).toBe(ShipmentStatus.PENDING);
    });

    it('should return null for non-existent tracking number', () => {
      const trackingInfo = service.getTrackingInfo('NONEXISTENT123');

      expect(trackingInfo).toBeNull();
    });
  });

  describe('updateShipmentStatus', () => {
    it('should update shipment status successfully', () => {
      const request: ShipmentRequest = {
        origin: {
          street: '123 Main St',
          city: 'Atlanta',
          state: 'GA',
          zipCode: '30301',
          country: 'US',
        },
        destination: {
          street: '789 Birch Blvd',
          city: 'Dallas',
          state: 'TX',
          zipCode: '75201',
          country: 'US',
        },
        package: {
          weight: 4,
          length: 28,
          width: 18,
          height: 12,
        },
      };

      const shipment = service.createShipment(request);
      const updatedShipment = service.updateShipmentStatus(
        shipment.trackingNumber,
        ShipmentStatus.IN_TRANSIT
      );

      expect(updatedShipment).not.toBeNull();
      expect(updatedShipment?.status).toBe(ShipmentStatus.IN_TRANSIT);
      expect(updatedShipment?.updatedAt.getTime()).toBeGreaterThanOrEqual(
        shipment.createdAt.getTime()
      );
    });

    it('should return null for non-existent tracking number', () => {
      const updatedShipment = service.updateShipmentStatus(
        'NONEXISTENT123',
        ShipmentStatus.DELIVERED
      );

      expect(updatedShipment).toBeNull();
    });
  });
});
