import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { HospitalProfile, UpdateHospitalProfileRequest } from '../models/hospital.model';

/**
 * The signed-in hospital's own profile and specialty list. As with the doctor service, the
 * API resolves ownership from the token rather than from anything in the URL.
 */
@Injectable({ providedIn: 'root' })
export class HospitalProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/hospital/profile`;

  get(): Observable<HospitalProfile> {
    return this.http
      .get<ApiResponse<HospitalProfile>>(this.baseUrl)
      .pipe(map((response) => response.data!));
  }

  update(request: UpdateHospitalProfileRequest): Observable<ApiResponse<HospitalProfile>> {
    return this.http.put<ApiResponse<HospitalProfile>>(this.baseUrl, request);
  }

  /** Replaces the whole specialty set with the ids supplied. */
  updateSpecialties(specialtyIds: string[]): Observable<ApiResponse<HospitalProfile>> {
    return this.http.put<ApiResponse<HospitalProfile>>(`${this.baseUrl}/specialties`, {
      specialtyIds,
    });
  }
}
