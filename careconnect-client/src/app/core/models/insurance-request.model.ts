/** Mirrors CareConnect.Domain.Enums.InsuranceRequestStatus. */
export type InsuranceRequestStatus = 'Pending' | 'UnderReview' | 'Approved' | 'Rejected' | 'Cancelled';

export const INSURANCE_REQUEST_STATUSES: readonly InsuranceRequestStatus[] = [
  'Pending',
  'UnderReview',
  'Approved',
  'Rejected',
  'Cancelled',
] as const;

export const INSURANCE_REQUEST_STATUS_LABELS: Record<InsuranceRequestStatus, string> = {
  Pending: 'Pending',
  UnderReview: 'Under review',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
};

// -------------------------------------------------------------------- Requests

export interface CreateInsuranceRequestRequest {
  appointmentId: string;
  insuranceCompanyId: string;
  memberNumber: string;
  policyNumber: string | null;
  serviceDescription: string;
  requestedAmount: number | null;
  patientNotes: string | null;
  insuranceCardImageUrl: string | null;
  supportingDocumentUrl: string | null;
}

export interface ApproveInsuranceRequestRequest {
  approvedAmount: number | null;
  approvalReferenceNumber: string | null;
  hospitalNotes: string | null;
}

export interface RejectInsuranceRequestRequest {
  rejectionReason: string;
  hospitalNotes: string | null;
}

export interface InsuranceHospitalNotesRequest {
  hospitalNotes: string | null;
}

// -------------------------------------------------------------------- Queries

export interface PatientInsuranceRequestQuery {
  status?: InsuranceRequestStatus | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  hospitalName?: string | null;
  insuranceCompanyId?: string | null;
  page: number;
  pageSize: number;
}

export interface HospitalInsuranceRequestQuery {
  status?: InsuranceRequestStatus | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  patientName?: string | null;
  doctorName?: string | null;
  insuranceCompanyId?: string | null;
  page: number;
  pageSize: number;
}

// ----------------------------------------------------------------- Responses

export interface PatientInsuranceRequest {
  insuranceRequestId: string;
  appointmentId: string;
  appointmentDate: string;
  appointmentStartTime: string;
  doctorName: string;
  doctorSpecialty: string | null;
  hospitalName: string;
  insuranceCompanyName: string;
  memberNumber: string;
  serviceDescription: string;
  requestedAmount: number | null;
  approvedAmount: number | null;
  status: number;
  statusName: InsuranceRequestStatus;
  rejectionReason: string | null;
  approvalReferenceNumber: string | null;
  submittedAt: string;
  reviewedAt: string | null;
  approvedAt: string | null;
  rejectedAt: string | null;
  cancelledAt: string | null;
}

export interface HospitalInsuranceRequest {
  insuranceRequestId: string;
  patientProfileId: string;
  patientName: string;
  patientPhoneNumber: string | null;
  appointmentId: string;
  appointmentDate: string;
  appointmentStartTime: string;
  doctorName: string;
  doctorSpecialty: string | null;
  insuranceCompanyId: string;
  insuranceCompany: string;
  memberNumber: string;
  policyNumber: string | null;
  serviceDescription: string;
  requestedAmount: number | null;
  approvedAmount: number | null;
  patientNotes: string | null;
  hospitalNotes: string | null;
  insuranceCardImageUrl: string | null;
  supportingDocumentUrl: string | null;
  status: number;
  statusName: InsuranceRequestStatus;
  rejectionReason: string | null;
  approvalReferenceNumber: string | null;
  submittedAt: string;
  reviewedAt: string | null;
  approvedAt: string | null;
  rejectedAt: string | null;
}

// -------------------------------------------------------------- Dashboard stats

export interface PatientInsuranceDashboardStats {
  pendingCount: number;
  approvedCount: number;
  latestStatus: InsuranceRequestStatus | null;
}

export interface HospitalInsuranceDashboardStats {
  pendingCount: number;
  underReviewCount: number;
  approvedThisMonthCount: number;
  rejectedThisMonthCount: number;
}
