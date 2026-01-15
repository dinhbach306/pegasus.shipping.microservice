import { Request, Response, NextFunction } from 'express';
import { Schema } from 'joi';
import logger from '../utils/logger';

export const validateParams = (schema: Schema) => {
  return (req: Request, res: Response, next: NextFunction): void => {
    const { error } = schema.validate(req.params, { abortEarly: false });
    
    if (error) {
      const errors = error.details.map((detail) => detail.message);
      logger.warn(`Parameter validation error: ${errors.join(', ')}`);
      res.status(400).json({
        success: false,
        message: 'Invalid parameter',
        errors,
      });
      return;
    }
    
    next();
  };
};
