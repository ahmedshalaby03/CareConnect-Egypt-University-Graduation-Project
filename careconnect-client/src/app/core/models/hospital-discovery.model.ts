import { BloodGroup } from './blood-group.model';

export interface NearbyHospitalQuery {
  latitude: number;
  longitude: number;
  radiusKm: number;
  specialtyId?: string | null;
  governorate?: string | null;
  city?: string | null;
  searchTerm?: string | null;
  hasAvailableAppointments?: boolean | null;
  hasAvailableBlood?: boolean | null;
  bloodGroup?: BloodGroup | null;
  page: number;
  pageSize: number;
}

export const NEARBY_RADIUS_OPTIONS_KM: readonly number[] = [5, 10, 25, 50, 100] as const;
export const DEFAULT_NEARBY_RADIUS_KM = 25;

export interface HospitalLocationDetails {
  hospitalProfileId: string;
  hospitalName: string;
  address: string | null;
  governorate: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  locationDescription: string | null;
  nearbyLandmark: string | null;
  phoneNumber: string | null;
  directionsUrl: string | null;
  isLocationCompleted: boolean;
  distanceKm: number | null;
}

export interface HospitalLocationOptions {
  governorates: string[];
  citiesByGovernorate: GovernorateCities[];
}

export interface GovernorateCities {
  governorate: string;
  cities: string[];
}

export interface SuperAdminHospitalLocationStats {
  activeHospitalsWithCompletedLocationCount: number;
  activeHospitalsMissingCoordinatesCount: number;
  governoratesCovered: string[];
}
