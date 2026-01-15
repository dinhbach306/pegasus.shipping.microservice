import Joi from 'joi';

export const addressSchema = Joi.object({
  street: Joi.string().required().min(1).max(200),
  city: Joi.string().required().min(1).max(100),
  state: Joi.string().required().min(2).max(100),
  zipCode: Joi.string().required().pattern(/^[0-9]{5}(-[0-9]{4})?$/),
  country: Joi.string().required().length(2).uppercase(),
});

export const packageSchema = Joi.object({
  weight: Joi.number().required().positive().max(1000),
  length: Joi.number().required().positive().max(500),
  width: Joi.number().required().positive().max(500),
  height: Joi.number().required().positive().max(500),
});

export const shipmentRequestSchema = Joi.object({
  origin: addressSchema.required(),
  destination: addressSchema.required(),
  package: packageSchema.required(),
});

export const trackingNumberSchema = Joi.string()
  .required()
  .pattern(/^[A-Z0-9]{10,20}$/);
