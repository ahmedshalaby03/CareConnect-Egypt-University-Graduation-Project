import { PagedResult } from './api-response.model';
import { ServiceDeliveryModeAvailability } from './medical-service-request.model';

export type MedicalServiceProviderType =
  | 'MedicalCenter'
  | 'Laboratory'
  | 'RadiologyCenter'
  | 'PhysiotherapyCenter'
  | 'HomeCareProvider'
  | 'NursingCenter'
  | 'Pharmacy'
  | 'Other';

export const MEDICAL_SERVICE_PROVIDER_TYPES: MedicalServiceProviderType[] = [
  'MedicalCenter',
  'Laboratory',
  'RadiologyCenter',
  'PhysiotherapyCenter',
  'HomeCareProvider',
  'NursingCenter',
  'Pharmacy',
  'Other',
];

export const PROVIDER_TYPE_LABELS: Record<MedicalServiceProviderType, string> = {
  MedicalCenter: 'Medical center',
  Laboratory: 'Laboratory',
  RadiologyCenter: 'Radiology center',
  PhysiotherapyCenter: 'Physiotherapy center',
  HomeCareProvider: 'Home healthcare provider',
  NursingCenter: 'Nursing center',
  Pharmacy: 'Pharmacy',
  Other: 'Other',
};

export interface MedicalServiceCategoryOption {
  id: string;
  name: string;
}

export interface MedicalServiceCategory extends MedicalServiceCategoryOption {
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  serviceUsageCount: number;
}

export interface MedicalServiceCategoryRequest {
  name: string;
  description: string | null;
}

export interface MedicalServiceCategoryQuery {
  search?: string;
  isActive?: boolean | null;
  page: number;
  pageSize: number;
}

export interface MedicalServiceOffering {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description: string | null;
  price: number;
  estimatedDurationMinutes: number | null;
  preparationInstructions: string | null;
  deliveryModeAvailability: ServiceDeliveryModeAvailability;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface MedicalServiceOfferingRequest {
  categoryId: string;
  name: string;
  description: string | null;
  price: number;
  estimatedDurationMinutes: number | null;
  preparationInstructions: string | null;
  deliveryModeAvailability: ServiceDeliveryModeAvailability;
  isActive: boolean;
}

export interface ProviderWorkingHour {
  id: string;
  dayOfWeek: string;
  dayName: string;
  openTime: string | null;
  closeTime: string | null;
  isClosed: boolean;
}

export interface UpdateProviderWorkingHoursRequest {
  workingHours: Array<{
    dayOfWeek: string;
    openTime: string | null;
    closeTime: string | null;
    isClosed: boolean;
  }>;
}

export interface MedicalServiceProviderProfile {
  id: string;
  businessName: string | null;
  providerType: MedicalServiceProviderType | null;
  providerTypeName: string | null;
  description: string | null;
  phoneNumber: string | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  isPublished: boolean;
  isReadyToPublish: boolean;
  missingRequirements: string[];
  activeServicesCount: number;
  inactiveServicesCount: number;
  serviceCategoriesCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateMedicalServiceProviderProfileRequest {
  businessName: string;
  providerType: MedicalServiceProviderType | null;
  description: string | null;
  phoneNumber: string | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
}

export interface MedicalServiceProviderPreview {
  profile: MedicalServiceProviderProfile;
  services: MedicalServiceOffering[];
  workingHours: ProviderWorkingHour[];
  directionsUrl: string | null;
}

export interface MedicalServiceProviderFilter {
  search?: string;
  providerType?: MedicalServiceProviderType | null;
  categoryId?: string | null;
  governorate?: string;
  city?: string;
  latitude?: number | null;
  longitude?: number | null;
  radiusKm?: number;
  sortBy?: 'name' | 'distance' | 'minimumPrice';
  page: number;
  pageSize: number;
}

export interface MedicalServiceProviderSummary {
  id: string;
  businessName: string;
  providerType: MedicalServiceProviderType;
  providerTypeName: string;
  description: string | null;
  governorate: string | null;
  city: string | null;
  categories: MedicalServiceCategoryOption[];
  minimumServicePrice: number | null;
  distanceKm: number | null;
}

export interface MedicalServiceProviderDetails {
  id: string;
  businessName: string;
  providerType: MedicalServiceProviderType;
  providerTypeName: string;
  description: string;
  phoneNumber: string;
  address: string;
  governorate: string;
  city: string;
  latitude: number;
  longitude: number;
  directionsUrl: string;
  distanceKm: number | null;
  services: MedicalServiceOffering[];
  workingHours: ProviderWorkingHour[];
}

export type MedicalServiceProviderPage = PagedResult<MedicalServiceProviderSummary>;
