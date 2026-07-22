import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  DoctorDirectoryDetails,
  DoctorDirectoryItem,
  DoctorDirectoryQuery,
  HospitalDirectoryDetails,
  HospitalDirectoryItem,
  HospitalDirectoryQuery,
} from '../models/directory.model';
import { AvailableSlotsResponse } from '../models/slot.model';

/**
 * Browse endpoints shared by every signed-in role. The API only ever returns completed
 * profiles and approved affiliations, so nothing here needs client-side filtering.
 */
@Injectable({ providedIn: 'root' })
export class DirectoryService {
  private readonly http = inject(HttpClient);
  private readonly hospitalsUrl = `${environment.apiBaseUrl}/hospitals`;
  private readonly doctorsUrl = `${environment.apiBaseUrl}/doctors`;

  searchHospitals(query: HospitalDirectoryQuery): Observable<PagedResult<HospitalDirectoryItem>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    params = this.appendIfSet(params, 'search', query.search);
    params = this.appendIfSet(params, 'governorate', query.governorate);
    params = this.appendIfSet(params, 'city', query.city);
    params = this.appendIfSet(params, 'specialtyId', query.specialtyId);

    return this.http
      .get<ApiResponse<PagedResult<HospitalDirectoryItem>>>(this.hospitalsUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getHospital(id: string): Observable<HospitalDirectoryDetails> {
    return this.http
      .get<ApiResponse<HospitalDirectoryDetails>>(`${this.hospitalsUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  searchDoctors(query: DoctorDirectoryQuery): Observable<PagedResult<DoctorDirectoryItem>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    params = this.appendIfSet(params, 'search', query.search);
    params = this.appendIfSet(params, 'specialtyId', query.specialtyId);
    params = this.appendIfSet(params, 'hospitalId', query.hospitalId);
    params = this.appendIfSet(params, 'governorate', query.governorate);
    params = this.appendIfSet(params, 'city', query.city);

    return this.http
      .get<ApiResponse<PagedResult<DoctorDirectoryItem>>>(this.doctorsUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getDoctor(id: string): Observable<DoctorDirectoryDetails> {
    return this.http
      .get<ApiResponse<DoctorDirectoryDetails>>(`${this.doctorsUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  /** Slot generation always happens server-side; this just asks for the result. */
  getAvailableSlots(
    doctorProfileId: string,
    hospitalProfileId: string,
    date: string,
  ): Observable<AvailableSlotsResponse> {
    const params = new HttpParams()
      .set('hospitalProfileId', hospitalProfileId)
      .set('date', date);

    return this.http
      .get<ApiResponse<AvailableSlotsResponse>>(`${this.doctorsUrl}/${doctorProfileId}/available-slots`, {
        params,
      })
      .pipe(map((response) => response.data!));
  }

  private appendIfSet(params: HttpParams, key: string, value: string | null | undefined): HttpParams {
    const trimmed = value?.trim();
    return trimmed ? params.set(key, trimmed) : params;
  }
}
