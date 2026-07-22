/** Must stay in step with CareConnect.Domain.Constants.AppRoles. */
export type UserRole =
  | 'Patient'
  | 'Doctor'
  | 'Hospital'
  | 'MedicalServiceProvider'
  | 'SuperAdmin';

/** The four roles a visitor may choose on the registration form. */
export const PUBLIC_ROLES: readonly UserRole[] = [
  'Patient',
  'Doctor',
  'Hospital',
  'MedicalServiceProvider',
] as const;

export const ALL_ROLES: readonly UserRole[] = [...PUBLIC_ROLES, 'SuperAdmin'] as const;

/** Human-readable labels, kept apart from the wire values the API expects. */
export const ROLE_LABELS: Record<UserRole, string> = {
  Patient: 'Patient',
  Doctor: 'Doctor',
  Hospital: 'Hospital',
  MedicalServiceProvider: 'Medical Service Provider',
  SuperAdmin: 'Super Admin',
};

export const ROLE_DESCRIPTIONS: Record<UserRole, string> = {
  Patient: 'Book care and manage your own health records.',
  Doctor: 'Offer consultations and manage your practice.',
  Hospital: 'Represent a hospital or clinic on the platform.',
  MedicalServiceProvider: 'Pharmacy, laboratory, radiology centre or similar.',
  SuperAdmin: 'Platform administration.',
};

export interface User {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

/**
 * Where each role lands after signing in. The router uses this, and the API enforces the
 * same boundaries independently - this map is convenience, not security.
 */
export const ROLE_HOME_ROUTE: Record<UserRole, string> = {
  Patient: '/dashboard/patient',
  Doctor: '/dashboard/doctor',
  Hospital: '/dashboard/hospital',
  MedicalServiceProvider: '/dashboard/service-provider',
  SuperAdmin: '/super-admin',
};

export function homeRouteForRole(role: UserRole | null | undefined): string {
  return role ? (ROLE_HOME_ROUTE[role] ?? '/login') : '/login';
}
