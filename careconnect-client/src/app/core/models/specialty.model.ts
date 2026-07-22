/** Compact shape returned by GET /api/specialties, used for every dropdown. */
export interface SpecialtyOption {
  id: string;
  name: string;
  arabicName: string | null;
}

/** Full shape returned by the SuperAdmin management endpoints. */
export interface Specialty extends SpecialtyOption {
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  doctorCount: number;
  hospitalCount: number;
}

export interface SpecialtyRequest {
  name: string;
  arabicName: string | null;
  description: string | null;
}

export interface SpecialtyQuery {
  search?: string | null;
  isActive?: boolean | null;
  page: number;
  pageSize: number;
}

export interface ToggleSpecialtyStatusResult {
  id: string;
  name: string;
  isActive: boolean;
}
