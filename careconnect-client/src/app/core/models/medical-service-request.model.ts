import { PagedResult } from './api-response.model';

/** Numeric wire values mirror the explicit C# enums. */
export type ServiceDeliveryMode = 1 | 2;
export type ServiceDeliveryModeAvailability = 1 | 2 | 3;
export type MedicalServiceRequestStatus = 1 | 2 | 3 | 4 | 5 | 6;

export const MEDICAL_SERVICE_REQUEST_STATUSES: MedicalServiceRequestStatus[] = [
  1, 2, 3, 4, 5, 6,
];

export const MEDICAL_SERVICE_REQUEST_STATUS_LABELS: Record<
  MedicalServiceRequestStatus,
  string
> = {
  1: 'Pending Review',
  2: 'Accepted',
  3: 'Rejected',
  4: 'Cancelled by Patient',
  5: 'Cancelled by Provider',
  6: 'Completed',
};

export const DELIVERY_MODE_LABELS: Record<ServiceDeliveryMode, string> = {
  1: 'At provider location',
  2: 'Home visit',
};

export interface CreateMedicalServiceRequest {
  medicalServiceOfferingId: string;
  requestedDate: string;
  preferredStartTime: string;
  deliveryMode: ServiceDeliveryMode;
  patientNotes: string | null;
  homeVisitAddress: string | null;
}

export interface AcceptMedicalServiceRequest {
  scheduledDate: string;
  scheduledStartTime: string;
  providerResponseNote: string | null;
}

export interface RejectMedicalServiceRequest {
  rejectionReason: string;
  providerResponseNote: string | null;
}

export interface CancelMedicalServiceRequest {
  cancellationReason: string;
}

export interface MedicalServiceRequestStatusHistory {
  previousStatus: MedicalServiceRequestStatus | null;
  newStatus: MedicalServiceRequestStatus;
  newStatusName: string;
  actorLabel: string;
  reason: string | null;
  createdAt: string;
}

export interface MedicalServiceRequestSummary {
  id: string;
  requestNumber: string;
  providerName: string;
  providerId: string;
  patientName: string;
  serviceId: string;
  serviceName: string;
  categoryName: string;
  deliveryMode: ServiceDeliveryMode;
  deliveryModeName: string;
  requestedDate: string;
  preferredStartTime: string;
  scheduledAt: string | null;
  priceSnapshot: number;
  status: MedicalServiceRequestStatus;
  statusName: string;
  createdAt: string;
}

export interface MedicalServiceRequestDetails extends MedicalServiceRequestSummary {
  providerTypeName: string | null;
  providerPhoneNumber: string | null;
  providerAddress: string | null;
  patientPhoneNumber: string | null;
  durationMinutesSnapshot: number | null;
  patientNotes: string | null;
  homeVisitAddress: string | null;
  providerResponseNote: string | null;
  rejectionReason: string | null;
  cancellationReason: string | null;
  completedAt: string | null;
  cancelledAt: string | null;
  statusHistory: MedicalServiceRequestStatusHistory[];
}

export interface PatientMedicalServiceRequestFilter {
  search?: string;
  status?: MedicalServiceRequestStatus | null;
  providerId?: string | null;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  page: number;
  pageSize: number;
}

export interface ProviderMedicalServiceRequestFilter {
  search?: string;
  status?: MedicalServiceRequestStatus | null;
  serviceId?: string | null;
  deliveryMode?: ServiceDeliveryMode | null;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  page: number;
  pageSize: number;
}

export interface MedicalServiceRequestDashboardSummary {
  pendingCount: number;
  acceptedUpcomingCount: number;
  completedCount: number;
  cancelledOrRejectedCount: number;
}

export type MedicalServiceRequestPage = PagedResult<MedicalServiceRequestSummary>;
