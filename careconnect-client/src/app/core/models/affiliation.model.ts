import { SpecialtyOption } from './specialty.model';

/** Mirrors CareConnect.Domain.Enums.AffiliationStatus. */
export type AffiliationStatus =
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Cancelled'
  | 'Removed';

export const AFFILIATION_STATUSES: readonly AffiliationStatus[] = [
  'Pending',
  'Approved',
  'Rejected',
  'Cancelled',
  'Removed',
] as const;

/** One row on the doctor's "my requests" screen. */
export interface DoctorHospitalRequest {
  id: string;
  hospitalProfileId: string;
  hospitalName: string;
  governorate: string | null;
  city: string | null;
  status: number;
  statusName: AffiliationStatus;
  requestedAt: string;
  reviewedAt: string | null;
  rejectionReason: string | null;
  isPrimary: boolean;
}

/** A hospital the doctor has been approved at. */
export interface AffiliatedHospital {
  id: string;
  hospitalName: string;
  address: string | null;
  governorate: string | null;
  city: string | null;
  phoneNumber: string | null;
  status: number;
  statusName: AffiliationStatus;
  isPrimary: boolean;
}

/** One row on the hospital's incoming requests screen. */
export interface HospitalDoctorRequest {
  id: string;
  doctorProfileId: string;
  doctorName: string;
  specialty: SpecialtyOption | null;
  licenseNumber: string | null;
  yearsOfExperience: number | null;
  biography: string | null;
  profileImageUrl: string | null;
  status: number;
  statusName: AffiliationStatus;
  requestedAt: string;
  reviewedAt: string | null;
  rejectionReason: string | null;
  isPrimary: boolean;
}

/** A doctor currently approved at the hospital. */
export interface HospitalDoctor {
  affiliationId: string;
  doctorProfileId: string;
  doctorName: string;
  specialty: SpecialtyOption | null;
  licenseNumber: string | null;
  yearsOfExperience: number | null;
  consultationPrice: number | null;
  profileImageUrl: string | null;
  approvedAt: string | null;
  isPrimary: boolean;
}

export interface DoctorAffiliationQuery {
  status?: AffiliationStatus | null;
  hospitalName?: string | null;
  page: number;
  pageSize: number;
}

export interface HospitalAffiliationQuery {
  status?: AffiliationStatus | null;
  search?: string | null;
  specialtyId?: string | null;
  page: number;
  pageSize: number;
}
