import { BloodGroup } from './blood-group.model';

export interface BloodStock {
  id: string;
  hospitalProfileId: string;
  bloodGroup: BloodGroup;
  bloodGroupDisplayName: string;
  availableUnits: number;
  minimumRequiredUnits: number;
  notes: string | null;
  isAvailable: boolean;
  isBelowMinimum: boolean;
  lastUpdatedByName: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateBloodStockRequest {
  bloodGroup: BloodGroup;
  availableUnits: number;
  minimumRequiredUnits: number;
  notes: string | null;
}

export interface UpdateBloodStockRequest {
  availableUnits: number;
  minimumRequiredUnits: number;
  notes: string | null;
}

export interface IncreaseBloodStockRequest {
  units: number;
  notes: string | null;
}

export interface DecreaseBloodStockRequest {
  units: number;
  notes: string | null;
}

export interface BloodStockQuery {
  bloodGroup?: BloodGroup | null;
  isAvailable?: boolean | null;
  isBelowMinimum?: boolean | null;
}
