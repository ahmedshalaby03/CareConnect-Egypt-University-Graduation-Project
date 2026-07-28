export interface DashboardAppointmentItem {
  id: string;
  appointmentDate: string;
  startTime: string;
  primaryName: string;
  secondaryName: string;
  status: string;
}

export interface DashboardRequestItem {
  id: string;
  category: string;
  title: string;
  status: string;
  submittedAt: string;
  actionRoute: string;
}

export interface PatientDashboard {
  nextAppointment: DashboardAppointmentItem | null;
  upcomingAppointmentsCount: number;
  pendingAppointmentsCount: number;
  pendingInsuranceRequestsCount: number;
  pendingBloodRequestsCount: number;
  activeMedicalServiceRequestsCount: number;
  unreadNotificationsCount: number;
  eligibleReviewsCount: number;
  recentRequests: DashboardRequestItem[];
}

export interface DoctorDashboard {
  todayAppointmentsCount: number;
  upcomingConfirmedAppointmentsCount: number;
  pendingAppointmentRequestsCount: number;
  completedAppointmentsCount: number;
  currentHospitalAffiliationsCount: number;
  pendingHospitalAffiliationRequestsCount: number;
  averageVisibleRating: number | null;
  visibleReviewsCount: number;
  unreadNotificationsCount: number;
  recentAppointments: DashboardAppointmentItem[];
}

export interface HospitalDashboard {
  activeAffiliatedDoctorsCount: number;
  pendingDoctorAffiliationRequestsCount: number;
  todayAppointmentsCount: number;
  pendingInsuranceRequestsCount: number;
  pendingBloodRequestsCount: number;
  lowBloodStockGroupsCount: number;
  averageVisibleRating: number | null;
  visibleReviewsCount: number;
  unreadNotificationsCount: number;
}

export interface MedicalServiceProviderDashboard {
  businessName: string;
  isPublished: boolean;
  activeServicesCount: number;
  inactiveServicesCount: number;
  pendingRequestsCount: number;
  acceptedUpcomingRequestsCount: number;
  completedRequestsCount: number;
  averageVisibleRating: number | null;
  visibleReviewsCount: number;
  unreadNotificationsCount: number;
  upcomingRequests: DashboardRequestItem[];
}

export interface RecentRegistration {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface SuperAdminDashboard {
  totalUsersCount: number;
  activeUsersCount: number;
  inactiveUsersCount: number;
  patientsCount: number;
  doctorsCount: number;
  hospitalsCount: number;
  medicalServiceProvidersCount: number;
  medicalSpecialtiesCount: number;
  insuranceCompaniesCount: number;
  medicalServiceCategoriesCount: number;
  visibleReviewsCount: number;
  hiddenReviewsCount: number;
  recentRegistrations: RecentRegistration[];
}
