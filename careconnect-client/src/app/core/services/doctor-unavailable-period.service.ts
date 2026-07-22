import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  CreateUnavailablePeriodRequest,
  UnavailablePeriod,
  UnavailablePeriodQuery,
} from '../models/unavailable-period.model';

@Injectable({ providedIn: 'root' })
export class DoctorUnavailablePeriodService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/doctor/unavailable-periods`;

  getAll(query: UnavailablePeriodQuery): Observable<UnavailablePeriod[]> {
    let params = new HttpParams();

    if (query.hospitalProfileId) {
      params = params.set('hospitalProfileId', query.hospitalProfileId);
    }

    if (query.dateFrom) {
      params = params.set('dateFrom', query.dateFrom);
    }

    if (query.dateTo) {
      params = params.set('dateTo', query.dateTo);
    }

    return this.http
      .get<ApiResponse<UnavailablePeriod[]>>(this.baseUrl, { params })
      .pipe(map((response) => response.data ?? []));
  }

  create(request: CreateUnavailablePeriodRequest): Observable<ApiResponse<UnavailablePeriod>> {
    return this.http.post<ApiResponse<UnavailablePeriod>>(this.baseUrl, request);
  }

  delete(id: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${id}`);
  }
}
