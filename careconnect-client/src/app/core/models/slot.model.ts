export interface Slot {
  /** "HH:mm:ss". */
  startTime: string;
  endTime: string;
}

export interface AvailableSlotsResponse {
  doctorProfileId: string;
  doctorName: string;
  hospitalProfileId: string;
  hospitalName: string;
  /** "yyyy-MM-dd". */
  date: string;
  slotDurationMinutes: number;
  slots: Slot[];
}
