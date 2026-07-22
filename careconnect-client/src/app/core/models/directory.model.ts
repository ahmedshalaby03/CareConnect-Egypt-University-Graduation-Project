import { SpecialtyOption } from './specialty.model';

// ------------------------------------------------------------------- Hospitals

export interface HospitalDirectoryItem {
  id: string;
  hospitalName: string;
  address: string | null;
  governorate: string | null;
  city: string | null;
  phoneNumber: string | null;
  description: string | null;
  logoUrl: string | null;
  latitude: number | null;
  longitude: number | null;
  specialties: SpecialtyOption[];
  numberOfApprovedDoctors: number;
}

export interface HospitalDirectoryDetails extends HospitalDirectoryItem {
  websiteUrl: string | null;
  openingTime: string | null;
  closingTime: string | null;
  /** Approved doctors only. */
  doctors: DirectoryDoctorSummary[];
}

export interface DirectoryDoctorSummary {
  doctorProfileId: string;
  fullName: string;
  specialty: SpecialtyOption | null;
  yearsOfExperience: number | null;
  consultationPrice: number | null;
  profileImageUrl: string | null;
}

export interface HospitalDirectoryQuery {
  search?: string | null;
  governorate?: string | null;
  city?: string | null;
  specialtyId?: string | null;
  page: number;
  pageSize: number;
}

// --------------------------------------------------------------------- Doctors

export interface DoctorDirectoryItem {
  doctorProfileId: string;
  fullName: string;
  specialty: SpecialtyOption | null;
  yearsOfExperience: number | null;
  biography: string | null;
  consultationPrice: number | null;
  governorate: string | null;
  city: string | null;
  profileImageUrl: string | null;
  /** Approved affiliations only. */
  hospitals: DirectoryHospitalSummary[];
}

export interface DoctorDirectoryDetails extends DoctorDirectoryItem {
  licenseNumber: string | null;
  address: string | null;
}

export interface DirectoryHospitalSummary {
  id: string;
  hospitalName: string;
  governorate: string | null;
  city: string | null;
  isPrimary: boolean;
}

export interface DoctorDirectoryQuery {
  search?: string | null;
  specialtyId?: string | null;
  hospitalId?: string | null;
  governorate?: string | null;
  city?: string | null;
  page: number;
  pageSize: number;
}

/**
 * Egypt's governorates, used to populate location filters. Kept client-side because it is
 * static reference data, unlike specialties which the SuperAdmin manages.
 */
export const EGYPT_GOVERNORATES: readonly string[] = [
  'Cairo',
  'Giza',
  'Alexandria',
  'Dakahlia',
  'Red Sea',
  'Beheira',
  'Fayoum',
  'Gharbia',
  'Ismailia',
  'Menofia',
  'Minya',
  'Qaliubiya',
  'New Valley',
  'Suez',
  'Aswan',
  'Assiut',
  'Beni Suef',
  'Port Said',
  'Damietta',
  'Sharkia',
  'South Sinai',
  'Kafr El Sheikh',
  'Matrouh',
  'Luxor',
  'Qena',
  'North Sinai',
  'Sohag',
] as const;
