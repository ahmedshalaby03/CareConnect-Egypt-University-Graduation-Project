import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { Availability, AvailabilityQuery, AvailabilityRequest } from '../models/availability.model';

/** The signed-in doctor's own weekly schedule. Ownership is resolved server-side from the token. */
@Injectable({ providedIn: 'root' })
export class DoctorAvailabilityService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/doctor/availability`;

  getAll(query: AvailabilityQuery): Observable<Availability[]> {
    let params = new HttpParams();

    if (query.hospitalProfileId) {
      params = params.set('hospitalProfileId', query.hospitalProfileId);
    }

    if (query.dayOfWeek !== null && query.dayOfWeek !== undefined) {
      params = params.set('dayOfWeek', query.dayOfWeek);
    }

    if (query.isActive !== null && query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }

    return this.http
      .get<ApiResponse<Availability[]>>(this.baseUrl, { params })
      .pipe(map((response) => response.data ?? []));
  }

  create(request: AvailabilityRequest): Observable<ApiResponse<Availability>> {
    return this.http.post<ApiResponse<Availability>>(this.baseUrl, request);
  }

  update(id: string, request: AvailabilityRequest): Observable<ApiResponse<Availability>> {
    return this.http.put<ApiResponse<Availability>>(`${this.baseUrl}/${id}`, request);
  }

  toggleStatus(id: string): Observable<ApiResponse<Availability>> {
    return this.http.patch<ApiResponse<Availability>>(`${this.baseUrl}/${id}/toggle-status`, {});
  }
}
