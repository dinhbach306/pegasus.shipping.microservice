export interface ShippingAddress {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

export interface Package {
  weight: number; // in kg
  length: number; // in cm
  width: number; // in cm
  height: number; // in cm
}

export interface ShippingRate {
  carrier: string;
  service: string;
  cost: number;
  currency: string;
  estimatedDays: number;
}

export interface ShipmentRequest {
  origin: ShippingAddress;
  destination: ShippingAddress;
  package: Package;
}

export interface Shipment {
  id: string;
  trackingNumber: string;
  status: ShipmentStatus;
  origin: ShippingAddress;
  destination: ShippingAddress;
  package: Package;
  createdAt: Date;
  updatedAt: Date;
  estimatedDelivery?: Date;
}

export enum ShipmentStatus {
  PENDING = 'PENDING',
  PICKED_UP = 'PICKED_UP',
  IN_TRANSIT = 'IN_TRANSIT',
  OUT_FOR_DELIVERY = 'OUT_FOR_DELIVERY',
  DELIVERED = 'DELIVERED',
  FAILED = 'FAILED'
}

export interface TrackingInfo {
  trackingNumber: string;
  status: ShipmentStatus;
  events: TrackingEvent[];
  estimatedDelivery?: Date;
}

export interface TrackingEvent {
  timestamp: Date;
  location: string;
  status: ShipmentStatus;
  description: string;
}
