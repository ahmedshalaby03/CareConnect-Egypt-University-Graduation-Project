import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  ApproveBloodRequestRequest,
  BloodRequestHospitalNotesRequest,
  CreateBloodRequestRequest,
  HospitalBloodDashboardStats,
  HospitalBloodRequest,
  HospitalBloodRequestQuery,
  PatientBloodDashboardStats,
  PatientBloodRequest,
  PatientBloodRequestQuery,
  RejectBloodRequestRequest,
} from '../models/blood-request.model';

/**
 * Both sides of a patient blood request - Patient and Hospital. The API decides who may
 * call what; these methods are simply grouped by the role that uses them, matching
 * InsuranceRequestService.
 */
@Injectable({ providedIn: 'root' })
export class BloodRequestService {
  private readonly http = inject(HttpClient);
  private readonly patientUrl = `${environment.apiBaseUrl}/patient/blood-requests`;
  private readonly hospitalUrl = `${environment.apiBaseUrl}/hospital/blood-requests`;

  // ------------------------------------------------------------- Patient side

  getPatientRequests(query: PatientBloodRequestQuery): Observable<PagedResult<PatientBloodRequest>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) params = params.set('status', query.status);
    if (query.bloodGroup) params = params.set('bloodGroup', query.bloodGroup);
    if (query.urgency) params = params.set('urgency', query.urgency);
    if (query.hospitalName?.trim()) params = params.set('hospitalName', query.hospitalName.trim());
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);

    return this.http
      .get<ApiResponse<PagedResult<PatientBloodRequest>>>(this.patientUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getPatientRequest(id: string): Observable<PatientBloodRequest> {
    return this.http
      .get<ApiResponse<PatientBloodRequest>>(`${this.patientUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  createRequest(request: CreateBloodRequestRequest): Observable<ApiResponse<PatientBloodRequest>> {
    return this.http.post<ApiResponse<PatientBloodRequest>>(this.patientUrl, request);
  }

  cancelRequest(id: string): Observable<ApiResponse<PatientBloodRequest>> {
    return this.http.patch<ApiResponse<PatientBloodRequest>>(`${this.patientUrl}/${id}/cancel`, {});
  }

  getPatientDashboardStats(): Observable<PatientBloodDashboardStats> {
    return this.http
      .get<ApiResponse<PatientBloodDashboardStats>>(`${this.patientUrl}/dashboard-stats`)
      .pipe(map((response) => response.data!));
  }

  // ------------------------------------------------------------ Hospital side

  getHospitalRequests(query: HospitalBloodRequestQuery): Observable<PagedResult<HospitalBloodRequest>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) params = params.set('status', query.status);
    if (query.bloodGroup) params = params.set('bloodGroup', query.bloodGroup);
    if (query.urgency) params = params.set('urgency', query.urgency);
    if (query.patientName?.trim()) params = params.set('patientName', query.patientName.trim());
    if (query.beneficiaryName?.trim()) params = params.set('beneficiaryName', query.beneficiaryName.trim());
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);

    return this.http
      .get<ApiResponse<PagedResult<HospitalBloodRequest>>>(this.hospitalUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getHospitalRequest(id: string): Observable<HospitalBloodRequest> {
    return this.http
      .get<ApiResponse<HospitalBloodRequest>>(`${this.hospitalUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  approve(id: string, request: ApproveBloodRequestRequest): Observable<ApiResponse<HospitalBloodRequest>> {
    return this.http.patch<ApiResponse<HospitalBloodRequest>>(`${this.hospitalUrl}/${id}/approve`, request);
  }

  reject(id: string, request: RejectBloodRequestRequest): Observable<ApiResponse<HospitalBloodRequest>> {
    return this.http.patch<ApiResponse<HospitalBloodRequest>>(`${this.hospitalUrl}/${id}/reject`, request);
  }

  fulfill(id: string): Observable<ApiResponse<HospitalBloodRequest>> {
    return this.http.patch<ApiResponse<HospitalBloodRequest>>(`${this.hospitalUrl}/${id}/fulfill`, {});
  }

  updateHospitalNotes(
    id: string,
    request: BloodRequestHospitalNotesRequest,
  ): Observable<ApiResponse<HospitalBloodRequest>> {
    return this.http.put<ApiResponse<HospitalBloodRequest>>(`${this.hospitalUrl}/${id}/notes`, request);
  }

  getHospitalDashboardStats(): Observable<HospitalBloodDashboardStats> {
    return this.http
      .get<ApiResponse<HospitalBloodDashboardStats>>(`${this.hospitalUrl}/dashboard-stats`)
      .pipe(map((response) => response.data!));
  }
}
