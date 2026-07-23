import { BloodGroup } from './blood-group.model';

export interface BloodAvailabilityQuery {
  bloodGroup?: BloodGroup | null;
  governorate?: string | null;
  city?: string | null;
  hospitalName?: string | null;
  availableOnly?: boolean | null;
  page: number;
  pageSize: number;
}

export interface BloodAvailability {
  hospitalProfileId: string;
  hospitalName: string;
  hospitalLogoUrl: string | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  phoneNumber: string | null;
  latitude: number | null;
  longitude: number | null;
  bloodGroup: BloodGroup;
  bloodGroupDisplayName: string;
  availableUnits: number;
  isAvailable: boolean;
  lastUpdatedAt: string;
}

export interface HospitalBloodBankDetails {
  hospitalProfileId: string;
  hospitalName: string;
  hospitalLogoUrl: string | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  phoneNumber: string | null;
  latitude: number | null;
  longitude: number | null;
  bloodGroups: BloodGroupAvailability[];
}

export interface BloodGroupAvailability {
  bloodGroup: BloodGroup;
  bloodGroupDisplayName: string;
  availableUnits: number;
  isAvailable: boolean;
  lastUpdatedAt: string;
}
