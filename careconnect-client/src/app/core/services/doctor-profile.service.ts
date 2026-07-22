import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { DoctorProfile, UpdateDoctorProfileRequest } from '../models/doctor.model';

/**
 * The signed-in doctor's own profile. There is no id in any URL: the API resolves the
 * profile from the bearer token, so this service cannot address another doctor's record.
 */
@Injectable({ providedIn: 'root' })
export class DoctorProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/doctor/profile`;

  get(): Observable<DoctorProfile> {
    return this.http
      .get<ApiResponse<DoctorProfile>>(this.baseUrl)
      .pipe(map((response) => response.data!));
  }

  update(request: UpdateDoctorProfileRequest): Observable<ApiResponse<DoctorProfile>> {
    return this.http.put<ApiResponse<DoctorProfile>>(this.baseUrl, request);
  }
}
