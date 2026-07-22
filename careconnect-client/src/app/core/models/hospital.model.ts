import { SpecialtyOption } from './specialty.model';

/** The hospital's own profile, from GET/PUT /api/hospital/profile. */
export interface HospitalProfile {
  id: string;
  fullName: string;
  email: string;
  accountPhoneNumber: string | null;
  hospitalName: string | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  phoneNumber: string | null;
  description: string | null;
  logoUrl: string | null;
  websiteUrl: string | null;
  /** "HH:mm" or null. */
  openingTime: string | null;
  closingTime: string | null;
  isProfileCompleted: boolean;
  missingFields: string[];
  specialties: SpecialtyOption[];
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateHospitalProfileRequest {
  fullName: string | null;
  hospitalName: string | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  phoneNumber: string | null;
  description: string | null;
  logoUrl: string | null;
  websiteUrl: string | null;
  openingTime: string | null;
  closingTime: string | null;
}
