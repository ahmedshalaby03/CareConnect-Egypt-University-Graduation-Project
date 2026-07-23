/** The hospital's own view of its location, from GET/PUT /api/hospital/profile/location. */
export interface HospitalLocation {
  hospitalProfileId: string;
  address: string | null;
  governorate: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  locationDescription: string | null;
  nearbyLandmark: string | null;
  /** Address + Governorate + City + both coordinates. Distinct from the general profile-completion flag. */
  isLocationCompleted: boolean;
  updatedAt: string | null;
}

export interface UpdateHospitalLocationRequest {
  address: string | null;
  governorate: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  locationDescription: string | null;
  nearbyLandmark: string | null;
}
