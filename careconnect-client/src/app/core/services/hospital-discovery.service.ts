import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { HospitalDirectoryItem } from '../models/directory.model';
import {
  HospitalLocationDetails,
  HospitalLocationOptions,
  NearbyHospitalQuery,
  SuperAdminHospitalLocationStats,
} from '../models/hospital-discovery.model';

/**
 * Location-aware hospital discovery: nearby search, single-hospital location details, and
 * the governorate/city option list. Distance is always calculated server-side.
 */
@Injectable({ providedIn: 'root' })
export class HospitalDiscoveryService {
  private readonly http = inject(HttpClient);
  private readonly hospitalsUrl = `${environment.apiBaseUrl}/hospitals`;

  searchNearby(query: NearbyHospitalQuery): Observable<PagedResult<HospitalDirectoryItem>> {
    let params = new HttpParams()
      .set('latitude', query.latitude)
      .set('longitude', query.longitude)
      .set('radiusKm', query.radiusKm)
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.specialtyId) params = params.set('specialtyId', query.specialtyId);
    if (query.governorate) params = params.set('governorate', query.governorate);
    if (query.city) params = params.set('city', query.city);
    if (query.searchTerm?.trim()) params = params.set('searchTerm', query.searchTerm.trim());
    if (query.bloodGroup) params = params.set('bloodGroup', query.bloodGroup);
    if (query.hasAvailableAppointments !== null && query.hasAvailableAppointments !== undefined) {
      params = params.set('hasAvailableAppointments', query.hasAvailableAppointments);
    }
    if (query.hasAvailableBlood !== null && query.hasAvailableBlood !== undefined) {
      params = params.set('hasAvailableBlood', query.hasAvailableBlood);
    }

    return this.http
      .get<ApiResponse<PagedResult<HospitalDirectoryItem>>>(`${this.hospitalsUrl}/nearby`, { params })
      .pipe(map((response) => response.data!));
  }

  getLocationDetails(
    hospitalProfileId: string,
    userLatitude?: number | null,
    userLongitude?: number | null,
  ): Observable<HospitalLocationDetails> {
    let params = new HttpParams();

    if (userLatitude !== null && userLatitude !== undefined) {
      params = params.set('userLatitude', userLatitude);
    }
    if (userLongitude !== null && userLongitude !== undefined) {
      params = params.set('userLongitude', userLongitude);
    }

    return this.http
      .get<ApiResponse<HospitalLocationDetails>>(`${this.hospitalsUrl}/${hospitalProfileId}/location`, { params })
      .pipe(map((response) => response.data!));
  }

  getLocationOptions(): Observable<HospitalLocationOptions> {
    return this.http
      .get<ApiResponse<HospitalLocationOptions>>(`${this.hospitalsUrl}/location-options`)
      .pipe(map((response) => response.data!));
  }

  getSuperAdminDashboardStats(): Observable<SuperAdminHospitalLocationStats> {
    return this.http
      .get<ApiResponse<SuperAdminHospitalLocationStats>>(
        `${environment.apiBaseUrl}/super-admin/hospitals/dashboard-stats`,
      )
      .pipe(map((response) => response.data!));
  }
}
