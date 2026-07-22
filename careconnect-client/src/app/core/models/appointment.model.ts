/** Mirrors CareConnect.Domain.Enums.AppointmentStatus. */
export type AppointmentStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Rejected'
  | 'Cancelled'
  | 'Completed'
  | 'NoShow';

export const APPOINTMENT_STATUSES: readonly AppointmentStatus[] = [
  'Pending',
  'Confirmed',
  'Rejected',
  'Cancelled',
  'Completed',
  'NoShow',
] as const;

export const APPOINTMENT_STATUS_LABELS: Record<AppointmentStatus, string> = {
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Completed: 'Completed',
  NoShow: 'No-show',
};

// -------------------------------------------------------------------- Requests

export interface BookAppointmentRequest {
  doctorProfileId: string;
  hospitalProfileId: string;
  /** "yyyy-MM-dd". */
  appointmentDate: string;
  /** "HH:mm:ss", taken verbatim from the selected Slot. */
  startTime: string;
  reason: string;
  patientNotes: string | null;
}

export interface RejectAppointmentRequest {
  rejectionReason: string;
}

export interface CancelAppointmentRequest {
  cancellationReason: string;
}

export interface DoctorNotesRequest {
  doctorNotes: string | null;
}

// -------------------------------------------------------------------- Queries

export interface PatientAppointmentQuery {
  status?: AppointmentStatus | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  doctorName?: string | null;
  hospitalName?: string | null;
  page: number;
  pageSize: number;
}

export interface DoctorAppointmentQuery {
  status?: AppointmentStatus | null;
  date?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  hospitalProfileId?: string | null;
  patientName?: string | null;
  page: number;
  pageSize: number;
}

export interface HospitalAppointmentQuery {
  status?: AppointmentStatus | null;
  date?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  doctorProfileId?: string | null;
  doctorName?: string | null;
  patientName?: string | null;
  page: number;
  pageSize: number;
}

// ----------------------------------------------------------------- Responses

export interface PatientAppointment {
  appointmentId: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: number;
  statusName: AppointmentStatus;
  reason: string | null;
  patientNotes: string | null;
  doctorProfileId: string;
  doctorName: string;
  doctorSpecialty: string | null;
  hospitalProfileId: string;
  hospitalName: string;
  hospitalAddress: string | null;
  rejectionReason: string | null;
  cancellationReason: string | null;
  createdAt: string;
}

export interface DoctorAppointment {
  appointmentId: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: number;
  statusName: AppointmentStatus;
  reason: string | null;
  patientNotes: string | null;
  doctorNotes: string | null;
  patientProfileId: string;
  patientName: string;
  patientPhoneNumber: string | null;
  hospitalProfileId: string;
  hospitalName: string;
  rejectionReason: string | null;
  cancellationReason: string | null;
  confirmedAt: string | null;
  rejectedAt: string | null;
  cancelledAt: string | null;
  completedAt: string | null;
  createdAt: string;
}

export interface HospitalAppointment {
  appointmentId: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: number;
  statusName: AppointmentStatus;
  reason: string | null;
  doctorProfileId: string;
  doctorName: string;
  doctorSpecialty: string | null;
  patientName: string;
  createdAt: string;
}

// -------------------------------------------------------------- Dashboard stats

export interface PatientDashboardStats {
  nextAppointment: PatientAppointment | null;
  upcomingCount: number;
  pendingCount: number;
}

export interface DoctorDashboardStats {
  todayCount: number;
  pendingCount: number;
  confirmedCount: number;
  completedThisMonthCount: number;
}

export interface HospitalDashboardStats {
  todayCount: number;
  pendingCount: number;
  activeApprovedDoctorsCount: number;
}
