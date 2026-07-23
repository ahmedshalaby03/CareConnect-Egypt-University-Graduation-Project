import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { HospitalLocation, UpdateHospitalLocationRequest } from '../models/hospital-location.model';

/** The signed-in hospital's own location. Ownership is resolved server-side from the token. */
@Injectable({ providedIn: 'root' })
export class HospitalLocationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/hospital/profile/location`;

  get(): Observable<HospitalLocation> {
    return this.http
      .get<ApiResponse<HospitalLocation>>(this.baseUrl)
      .pipe(map((response) => response.data!));
  }

  update(request: UpdateHospitalLocationRequest): Observable<ApiResponse<HospitalLocation>> {
    return this.http.put<ApiResponse<HospitalLocation>>(this.baseUrl, request);
  }
}
