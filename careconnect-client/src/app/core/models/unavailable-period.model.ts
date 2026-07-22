export interface UnavailablePeriod {
  id: string;
  hospitalProfileId: string;
  hospitalName: string;
  /** ISO datetime strings. */
  startDateTime: string;
  endDateTime: string;
  reason: string | null;
  createdAt: string;
}

export interface CreateUnavailablePeriodRequest {
  hospitalProfileId: string;
  startDateTime: string;
  endDateTime: string;
  reason: string | null;
}

export interface UnavailablePeriodQuery {
  dateFrom?: string | null;
  dateTo?: string | null;
  hospitalProfileId?: string | null;
}
