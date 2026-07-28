import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },

  // Public authentication screens, wrapped in the split-screen brand layout.
  {
    path: '',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./layouts/auth-layout/auth-layout').then((m) => m.AuthLayout),
    children: [
      {
        path: 'login',
        title: 'Sign in - CareConnect Egypt',
        loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
      },
      {
        path: 'register',
        title: 'Create account - CareConnect Egypt',
        loadComponent: () =>
          import('./features/auth/register/register').then((m) => m.Register),
      },
    ],
  },

  // Everything below requires a signed-in user and uses the application chrome.
  // `data: { role }` reaches the RoleDashboard input through withComponentInputBinding.
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layouts/main-layout/main-layout').then((m) => m.MainLayout),
    children: [
      {
        path: 'notifications',
        title: 'Notifications - CareConnect Egypt',
        loadComponent: () =>
          import('./features/notifications/notification-center').then(
            (m) => m.NotificationCenter,
          ),
      },
      {
        path: 'dashboard/patient',
        title: 'Patient dashboard - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/dashboard/role-dashboard/role-dashboard').then((m) => m.RoleDashboard),
        data: { role: 'Patient' },
      },
      {
        path: 'dashboard/patient/appointments',
        title: 'My appointments - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/appointments/patient-appointments').then(
            (m) => m.PatientAppointments,
          ),
      },
      {
        path: 'dashboard/patient/appointments/:id',
        title: 'Appointment - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/appointments/patient-appointment-details').then(
            (m) => m.PatientAppointmentDetails,
          ),
      },
      {
        path: 'dashboard/patient/insurance-requests',
        title: 'My insurance requests - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/insurance-requests/insurance-requests').then(
            (m) => m.PatientInsuranceRequests,
          ),
      },
      {
        path: 'dashboard/patient/insurance-requests/new',
        title: 'New insurance request - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/insurance-requests/new-insurance-request').then(
            (m) => m.NewInsuranceRequest,
          ),
      },
      {
        path: 'dashboard/patient/insurance-requests/:id',
        title: 'Insurance request - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/insurance-requests/insurance-request-details').then(
            (m) => m.InsuranceRequestDetails,
          ),
      },
      {
        path: 'dashboard/patient/blood-requests',
        title: 'My blood requests - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/blood-requests/blood-requests').then(
            (m) => m.PatientBloodRequests,
          ),
      },
      {
        path: 'dashboard/patient/blood-requests/new',
        title: 'New blood request - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/blood-requests/new-blood-request').then(
            (m) => m.NewBloodRequest,
          ),
      },
      {
        path: 'dashboard/patient/blood-requests/:id',
        title: 'Blood request - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/blood-requests/blood-request-details').then(
            (m) => m.BloodRequestDetails,
          ),
      },
      {
        path: 'dashboard/patient/ai-assistant',
        title: 'AI Medical Assistant - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import(
            './features/patient/ai-medical-assistant/ai-medical-assistant'
          ).then((m) => m.AiMedicalAssistantPage),
      },
      {
        path: 'dashboard/patient/service-requests',
        title: 'My service requests - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/service-requests/patient-service-requests').then(
            (m) => m.PatientServiceRequests,
          ),
      },
      {
        path: 'dashboard/patient/service-requests/:id',
        title: 'Service request - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/service-requests/patient-service-request-details').then(
            (m) => m.PatientServiceRequestDetails,
          ),
      },
      {
        path: 'dashboard/patient/reviews',
        title: 'My reviews - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/reviews/patient-reviews').then((m) => m.PatientReviews),
      },

      // ------------------------------------------------------------- Doctor
      {
        path: 'dashboard/doctor',
        title: 'Doctor dashboard - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/dashboard/role-dashboard/role-dashboard').then((m) => m.RoleDashboard),
        data: { role: 'Doctor' },
      },
      {
        path: 'dashboard/doctor/profile',
        title: 'My doctor profile - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/doctor/profile/doctor-profile').then((m) => m.DoctorProfilePage),
      },
      {
        path: 'dashboard/doctor/hospitals',
        title: 'Find hospitals - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/doctor/hospitals/doctor-hospitals').then((m) => m.DoctorHospitals),
      },
      {
        path: 'dashboard/doctor/hospital-requests',
        title: 'My hospital requests - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/doctor/hospital-requests/doctor-hospital-requests').then(
            (m) => m.DoctorHospitalRequests,
          ),
      },
      {
        path: 'dashboard/doctor/availability',
        title: 'My availability - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/doctor/availability/availability').then((m) => m.DoctorAvailability),
      },
      {
        path: 'dashboard/doctor/unavailable-periods',
        title: 'Unavailable periods - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/doctor/unavailable-periods/unavailable-periods').then(
            (m) => m.DoctorUnavailablePeriods,
          ),
      },
      {
        path: 'dashboard/doctor/appointments',
        title: 'My appointments - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/doctor/appointments/doctor-appointments').then(
            (m) => m.DoctorAppointments,
          ),
      },
      {
        path: 'dashboard/doctor/appointments/:id',
        title: 'Appointment - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/doctor/appointments/doctor-appointment-details').then(
            (m) => m.DoctorAppointmentDetails,
          ),
      },
      {
        path: 'dashboard/doctor/reviews',
        title: 'Patient reviews - CareConnect Egypt',
        canActivate: [roleGuard('Doctor')],
        loadComponent: () =>
          import('./features/reviews/owner-reviews').then((m) => m.OwnerReviews),
        data: { ownerPath: 'doctor' },
      },

      // ----------------------------------------------------------- Hospital
      {
        path: 'dashboard/hospital',
        title: 'Hospital dashboard - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/dashboard/role-dashboard/role-dashboard').then((m) => m.RoleDashboard),
        data: { role: 'Hospital' },
      },
      {
        path: 'dashboard/hospital/profile',
        title: 'Hospital profile - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/profile/hospital-profile').then((m) => m.HospitalProfilePage),
      },
      {
        path: 'dashboard/hospital/location',
        title: 'Hospital location - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/location/hospital-location').then((m) => m.HospitalLocationPage),
      },
      {
        path: 'dashboard/hospital/doctor-requests',
        title: 'Doctor requests - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/doctor-requests/hospital-doctor-requests').then(
            (m) => m.HospitalDoctorRequests,
          ),
      },
      {
        path: 'dashboard/hospital/doctors',
        title: 'Our doctors - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/doctors/hospital-doctors').then((m) => m.HospitalDoctors),
      },
      {
        path: 'dashboard/hospital/appointments',
        title: 'Appointments - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/appointments/hospital-appointments').then(
            (m) => m.HospitalAppointments,
          ),
      },
      {
        path: 'dashboard/hospital/insurance-requests',
        title: 'Insurance requests - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/insurance-requests/hospital-insurance-requests').then(
            (m) => m.HospitalInsuranceRequests,
          ),
      },
      {
        path: 'dashboard/hospital/insurance-requests/:id',
        title: 'Insurance request - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/insurance-requests/hospital-insurance-request-details').then(
            (m) => m.HospitalInsuranceRequestDetails,
          ),
      },
      {
        path: 'dashboard/hospital/blood-stock',
        title: 'Blood stock - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/blood-stock/blood-stock').then((m) => m.HospitalBloodStock),
      },
      {
        path: 'dashboard/hospital/blood-requests',
        title: 'Blood requests - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/blood-requests/hospital-blood-requests').then(
            (m) => m.HospitalBloodRequests,
          ),
      },
      {
        path: 'dashboard/hospital/blood-requests/:id',
        title: 'Blood request - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/hospital/blood-requests/hospital-blood-request-details').then(
            (m) => m.HospitalBloodRequestDetails,
          ),
      },
      {
        path: 'dashboard/hospital/reviews',
        title: 'Patient reviews - CareConnect Egypt',
        canActivate: [roleGuard('Hospital')],
        loadComponent: () =>
          import('./features/reviews/owner-reviews').then((m) => m.OwnerReviews),
        data: { ownerPath: 'hospital' },
      },

      {
        path: 'dashboard/service-provider',
        title: 'Service provider dashboard - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/service-provider/dashboard/service-provider-dashboard').then(
            (m) => m.ServiceProviderDashboard,
          ),
      },
      {
        path: 'dashboard/service-provider/profile',
        title: 'Business profile - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/service-provider/profile/service-provider-profile').then(
            (m) => m.ServiceProviderProfilePage,
          ),
      },
      {
        path: 'dashboard/service-provider/services',
        title: 'My services - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/service-provider/services/service-provider-services').then(
            (m) => m.ServiceProviderServicesPage,
          ),
      },
      {
        path: 'dashboard/service-provider/working-hours',
        title: 'Working hours - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/service-provider/working-hours/service-provider-working-hours').then(
            (m) => m.ServiceProviderWorkingHoursPage,
          ),
      },
      {
        path: 'dashboard/service-provider/preview',
        title: 'Provider preview - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/service-provider/preview/service-provider-preview').then(
            (m) => m.ServiceProviderPreviewPage,
          ),
      },
      {
        path: 'dashboard/service-provider/requests',
        title: 'Service requests - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/service-provider/requests/provider-service-requests').then(
            (m) => m.ProviderServiceRequests,
          ),
      },
      {
        path: 'dashboard/service-provider/requests/:id',
        title: 'Service request - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/service-provider/requests/provider-service-request-details').then(
            (m) => m.ProviderServiceRequestDetails,
          ),
      },
      {
        path: 'dashboard/service-provider/reviews',
        title: 'Patient reviews - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/reviews/owner-reviews').then((m) => m.OwnerReviews),
        data: { ownerPath: 'medical-service-provider' },
      },

      // --------------------------------------------------------- SuperAdmin
      {
        path: 'super-admin',
        title: 'User management - CareConnect Egypt',
        canActivate: [roleGuard('SuperAdmin')],
        loadComponent: () =>
          import('./features/super-admin/users/users').then((m) => m.SuperAdminUsers),
      },
      {
        path: 'super-admin/specialties',
        title: 'Medical specialties - CareConnect Egypt',
        canActivate: [roleGuard('SuperAdmin')],
        loadComponent: () =>
          import('./features/super-admin/specialties/specialties').then(
            (m) => m.SuperAdminSpecialties,
          ),
      },
      {
        path: 'super-admin/insurance-companies',
        title: 'Insurance companies - CareConnect Egypt',
        canActivate: [roleGuard('SuperAdmin')],
        loadComponent: () =>
          import('./features/super-admin/insurance-companies/insurance-companies').then(
            (m) => m.SuperAdminInsuranceCompanies,
          ),
      },
      {
        path: 'super-admin/medical-service-categories',
        title: 'Medical service categories - CareConnect Egypt',
        canActivate: [roleGuard('SuperAdmin')],
        loadComponent: () =>
          import(
            './features/super-admin/medical-service-categories/medical-service-categories'
          ).then((m) => m.SuperAdminMedicalServiceCategories),
      },
      {
        path: 'super-admin/reviews',
        title: 'Review moderation - CareConnect Egypt',
        canActivate: [roleGuard('SuperAdmin')],
        loadComponent: () =>
          import('./features/super-admin/reviews/review-moderation').then(
            (m) => m.ReviewModeration,
          ),
      },

      // ---------------------------------------------- Directories (any role)
      // Deliberately not role-guarded: patients, doctors and hospitals all browse these.
      {
        path: 'hospitals',
        title: 'Hospitals - CareConnect Egypt',
        loadComponent: () =>
          import('./features/directory/hospitals/hospital-list').then((m) => m.HospitalList),
      },
      {
        path: 'hospitals/:id',
        title: 'Hospital - CareConnect Egypt',
        loadComponent: () =>
          import('./features/directory/hospitals/hospital-details').then((m) => m.HospitalDetails),
      },
      {
        path: 'hospitals/:id/location',
        title: 'Hospital location - CareConnect Egypt',
        loadComponent: () =>
          import('./features/directory/hospitals/hospital-location-details').then(
            (m) => m.HospitalLocationDetailsPage,
          ),
      },
      {
        path: 'doctors',
        title: 'Doctors - CareConnect Egypt',
        loadComponent: () =>
          import('./features/directory/doctors/doctor-list').then((m) => m.DoctorList),
      },
      {
        path: 'doctors/:id',
        title: 'Doctor - CareConnect Egypt',
        loadComponent: () =>
          import('./features/directory/doctors/doctor-details').then((m) => m.DoctorDetails),
      },
      {
        path: 'doctors/:id/book',
        title: 'Book an appointment - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/booking/book-appointment/book-appointment').then(
            (m) => m.BookAppointment,
          ),
      },
      {
        path: 'blood-bank',
        title: 'Blood bank - CareConnect Egypt',
        loadComponent: () => import('./features/blood-bank/blood-bank').then((m) => m.BloodBank),
      },
      {
        path: 'blood-bank/hospitals/:id',
        title: 'Hospital blood bank - CareConnect Egypt',
        loadComponent: () =>
          import('./features/blood-bank/blood-bank-hospital-details').then(
            (m) => m.BloodBankHospitalDetails,
          ),
      },
      {
        path: 'medical-service-providers',
        title: 'Medical services - CareConnect Egypt',
        loadComponent: () =>
          import(
            './features/directory/medical-service-providers/medical-service-provider-list'
          ).then((m) => m.MedicalServiceProviderListPage),
      },
      {
        path: 'medical-service-providers/:providerId/services/:serviceId/request',
        title: 'Request medical service - CareConnect Egypt',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient/service-requests/request-medical-service').then(
            (m) => m.RequestMedicalService,
          ),
      },
      {
        path: 'medical-service-providers/:id',
        title: 'Medical service provider - CareConnect Egypt',
        loadComponent: () =>
          import(
            './features/directory/medical-service-providers/medical-service-provider-details'
          ).then((m) => m.MedicalServiceProviderDetailsPage),
      },
    ],
  },

  {
    path: '**',
    redirectTo: 'login',
  },
];
