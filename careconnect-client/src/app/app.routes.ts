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
    ],
  },

  {
    path: '**',
    redirectTo: 'login',
  },
];
