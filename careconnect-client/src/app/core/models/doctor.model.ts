import { SpecialtyOption } from './specialty.model';

/** The doctor's own profile, from GET/PUT /api/doctor/profile. */
export interface DoctorProfile {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  specialty: SpecialtyOption | null;
  licenseNumber: string | null;
  yearsOfExperience: number | null;
  biography: string | null;
  consultationPrice: number | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  profileImageUrl: string | null;
  isProfileCompleted: boolean;
  /** Required fields still blank. Computed by the API, never by the client. */
  missingFields: string[];
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateDoctorProfileRequest {
  fullName: string | null;
  phoneNumber: string | null;
  specialtyId: string | null;
  licenseNumber: string | null;
  yearsOfExperience: number | null;
  biography: string | null;
  consultationPrice: number | null;
  address: string | null;
  governorate: string | null;
  city: string | null;
  profileImageUrl: string | null;
}
