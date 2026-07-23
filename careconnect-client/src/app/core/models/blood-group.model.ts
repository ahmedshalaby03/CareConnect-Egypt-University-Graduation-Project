/** Mirrors CareConnect.Domain.Enums.BloodGroup. */
export type BloodGroup =
  | 'APositive'
  | 'ANegative'
  | 'BPositive'
  | 'BNegative'
  | 'ABPositive'
  | 'ABNegative'
  | 'OPositive'
  | 'ONegative';

export const BLOOD_GROUPS: readonly BloodGroup[] = [
  'APositive',
  'ANegative',
  'BPositive',
  'BNegative',
  'ABPositive',
  'ABNegative',
  'OPositive',
  'ONegative',
] as const;

/**
 * Local convenience copy of the same mapping the API sends back as
 * BloodGroupDisplayName on every response - matches the InsuranceRequestStatus/
 * AppointmentStatus label pattern, not a blind guess at the numeric value.
 */
export const BLOOD_GROUP_LABELS: Record<BloodGroup, string> = {
  APositive: 'A+',
  ANegative: 'A-',
  BPositive: 'B+',
  BNegative: 'B-',
  ABPositive: 'AB+',
  ABNegative: 'AB-',
  OPositive: 'O+',
  ONegative: 'O-',
};
