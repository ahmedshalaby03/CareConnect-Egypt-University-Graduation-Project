import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { BloodGroup } from '../models/blood-group.model';
import {
  BloodStock,
  BloodStockQuery,
  CreateBloodStockRequest,
  DecreaseBloodStockRequest,
  IncreaseBloodStockRequest,
  UpdateBloodStockRequest,
} from '../models/blood-stock.model';
import { SuperAdminBloodDashboardStats } from '../models/blood-request.model';

@Injectable({ providedIn: 'root' })
export class BloodStockService {
  private readonly http = inject(HttpClient);
  private readonly hospitalUrl = `${environment.apiBaseUrl}/hospital/blood-stock`;

  getHospitalStock(query: BloodStockQuery): Observable<BloodStock[]> {
    let params = new HttpParams();

    if (query.bloodGroup) params = params.set('bloodGroup', query.bloodGroup);
    if (query.isAvailable !== null && query.isAvailable !== undefined) {
      params = params.set('isAvailable', query.isAvailable);
    }
    if (query.isBelowMinimum !== null && query.isBelowMinimum !== undefined) {
      params = params.set('isBelowMinimum', query.isBelowMinimum);
    }

    return this.http
      .get<ApiResponse<BloodStock[]>>(this.hospitalUrl, { params })
      .pipe(map((response) => response.data ?? []));
  }

  getHospitalStockByBloodGroup(bloodGroup: BloodGroup): Observable<BloodStock> {
    return this.http
      .get<ApiResponse<BloodStock>>(`${this.hospitalUrl}/${bloodGroup}`)
      .pipe(map((response) => response.data!));
  }

  create(request: CreateBloodStockRequest): Observable<ApiResponse<BloodStock>> {
    return this.http.post<ApiResponse<BloodStock>>(this.hospitalUrl, request);
  }

  update(id: string, request: UpdateBloodStockRequest): Observable<ApiResponse<BloodStock>> {
    return this.http.put<ApiResponse<BloodStock>>(`${this.hospitalUrl}/${id}`, request);
  }

  increase(id: string, request: IncreaseBloodStockRequest): Observable<ApiResponse<BloodStock>> {
    return this.http.patch<ApiResponse<BloodStock>>(`${this.hospitalUrl}/${id}/increase`, request);
  }

  decrease(id: string, request: DecreaseBloodStockRequest): Observable<ApiResponse<BloodStock>> {
    return this.http.patch<ApiResponse<BloodStock>>(`${this.hospitalUrl}/${id}/decrease`, request);
  }

  getSuperAdminDashboardStats(): Observable<SuperAdminBloodDashboardStats> {
    return this.http
      .get<ApiResponse<SuperAdminBloodDashboardStats>>(
        `${environment.apiBaseUrl}/super-admin/blood-bank/dashboard-stats`,
      )
      .pipe(map((response) => response.data!));
  }
}
