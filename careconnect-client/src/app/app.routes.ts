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
        path: 'dashboard/service-provider',
        title: 'Service provider dashboard - CareConnect Egypt',
        canActivate: [roleGuard('MedicalServiceProvider')],
        loadComponent: () =>
          import('./features/dashboard/role-dashboard/role-dashboard').then((m) => m.RoleDashboard),
        data: { role: 'MedicalServiceProvider' },
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
    ],
  },

  {
    path: '**',
    redirectTo: 'login',
  },
];
