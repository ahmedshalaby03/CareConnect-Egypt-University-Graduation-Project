/** Names in Sunday-first order, matching System.DayOfWeek used by the API. */
export const DAYS_OF_WEEK: readonly string[] = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
] as const;

export interface Availability {
  id: string;
  hospitalProfileId: string;
  hospitalName: string;
  dayOfWeek: number;
  dayOfWeekName: string;
  /** "HH:mm" format. */
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface AvailabilityRequest {
  hospitalProfileId: string;
  /** 0-6, Sunday-first - matches both System.DayOfWeek and JS Date.getDay(). */
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

export interface AvailabilityQuery {
  hospitalProfileId?: string | null;
  dayOfWeek?: number | null;
  isActive?: boolean | null;
}
