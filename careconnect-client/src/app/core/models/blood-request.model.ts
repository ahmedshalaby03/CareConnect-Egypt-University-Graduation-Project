import { BloodGroup } from './blood-group.model';

/** Mirrors CareConnect.Domain.Enums.BloodRequestStatus. */
export type BloodRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Fulfilled' | 'Cancelled';

export const BLOOD_REQUEST_STATUSES: readonly BloodRequestStatus[] = [
  'Pending',
  'Approved',
  'Rejected',
  'Fulfilled',
  'Cancelled',
] as const;

/** Mirrors CareConnect.Domain.Enums.BloodRequestUrgency. A priority indicator only. */
export type BloodRequestUrgency = 'Normal' | 'Urgent' | 'Emergency';

export const BLOOD_REQUEST_URGENCIES: readonly BloodRequestUrgency[] = [
  'Normal',
  'Urgent',
  'Emergency',
] as const;

// -------------------------------------------------------------------- Requests

export interface CreateBloodRequestRequest {
  hospitalProfileId: string;
  bloodGroup: BloodGroup;
  unitsRequested: number;
  beneficiaryName: string;
  beneficiaryAge: number | null;
  contactPhoneNumber: string;
  medicalCondition: string | null;
  hospitalOrFacilityName: string | null;
  requestNotes: string | null;
  urgency: BloodRequestUrgency;
}

export interface ApproveBloodRequestRequest {
  hospitalNotes: string | null;
}

export interface RejectBloodRequestRequest {
  rejectionReason: string;
  hospitalNotes: string | null;
}

export interface BloodRequestHospitalNotesRequest {
  hospitalNotes: string | null;
}

// -------------------------------------------------------------------- Queries

export interface PatientBloodRequestQuery {
  status?: BloodRequestStatus | null;
  bloodGroup?: BloodGroup | null;
  urgency?: BloodRequestUrgency | null;
  hospitalName?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page: number;
  pageSize: number;
}

export interface HospitalBloodRequestQuery {
  status?: BloodRequestStatus | null;
  bloodGroup?: BloodGroup | null;
  urgency?: BloodRequestUrgency | null;
  patientName?: string | null;
  beneficiaryName?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page: number;
  pageSize: number;
}

// ----------------------------------------------------------------- Responses

export interface PatientBloodRequest {
  bloodRequestId: string;
  hospitalProfileId: string;
  hospitalName: string;
  hospitalAddress: string | null;
  hospitalPhoneNumber: string | null;
  bloodGroup: BloodGroup;
  bloodGroupDisplayName: string;
  unitsRequested: number;
  beneficiaryName: string;
  contactPhoneNumber: string;
  urgency: BloodRequestUrgency;
  urgencyName: BloodRequestUrgency;
  status: number;
  statusName: BloodRequestStatus;
  rejectionReason: string | null;
  hospitalNotes: string | null;
  submittedAt: string;
  approvedAt: string | null;
  rejectedAt: string | null;
  fulfilledAt: string | null;
  cancelledAt: string | null;
}

export interface HospitalBloodRequest {
  bloodRequestId: string;
  patientProfileId: string;
  patientName: string;
  patientPhoneNumber: string | null;
  beneficiaryName: string;
  beneficiaryAge: number | null;
  contactPhoneNumber: string;
  bloodGroup: BloodGroup;
  bloodGroupDisplayName: string;
  unitsRequested: number;
  currentAvailableUnits: number;
  medicalCondition: string | null;
  hospitalOrFacilityName: string | null;
  requestNotes: string | null;
  hospitalNotes: string | null;
  urgency: BloodRequestUrgency;
  urgencyName: BloodRequestUrgency;
  status: number;
  statusName: BloodRequestStatus;
  rejectionReason: string | null;
  submittedAt: string;
  approvedAt: string | null;
  rejectedAt: string | null;
  fulfilledAt: string | null;
  cancelledAt: string | null;
}

// -------------------------------------------------------------- Dashboard stats

export interface PatientBloodDashboardStats {
  pendingCount: number;
  approvedCount: number;
  latestStatus: BloodRequestStatus | null;
}

export interface HospitalBloodDashboardStats {
  totalAvailableUnits: number;
  bloodGroupsBelowMinimumCount: number;
  pendingRequestsCount: number;
  emergencyRequestsCount: number;
  approvedAwaitingFulfillmentCount: number;
}

export interface SuperAdminBloodDashboardStats {
  hospitalsWithStockCount: number;
  activeBloodStockRecordsCount: number;
}
