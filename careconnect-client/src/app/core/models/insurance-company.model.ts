/** Compact shape returned by GET /api/insurance-companies, used by the patient's request form. */
export interface InsuranceCompanyOption {
  id: string;
  name: string;
  arabicName: string | null;
  logoUrl: string | null;
}

/** Full shape returned by the SuperAdmin management endpoints. */
export interface InsuranceCompany extends InsuranceCompanyOption {
  description: string | null;
  phoneNumber: string | null;
  websiteUrl: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  requestCount: number;
}

export interface InsuranceCompanyRequest {
  name: string;
  arabicName: string | null;
  description: string | null;
  phoneNumber: string | null;
  websiteUrl: string | null;
  logoUrl: string | null;
}

export interface InsuranceCompanyQuery {
  searchTerm?: string | null;
  isActive?: boolean | null;
  page: number;
  pageSize: number;
}

export interface ToggleInsuranceCompanyStatusResult {
  id: string;
  name: string;
  isActive: boolean;
}
