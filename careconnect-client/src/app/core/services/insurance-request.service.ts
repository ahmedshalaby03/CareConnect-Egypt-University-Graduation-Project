import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  ApproveInsuranceRequestRequest,
  CreateInsuranceRequestRequest,
  HospitalInsuranceDashboardStats,
  HospitalInsuranceRequest,
  HospitalInsuranceRequestQuery,
  InsuranceHospitalNotesRequest,
  PatientInsuranceDashboardStats,
  PatientInsuranceRequest,
  PatientInsuranceRequestQuery,
  RejectInsuranceRequestRequest,
} from '../models/insurance-request.model';

/**
 * Both sides of a digital insurance request - Patient and Hospital. The API decides who
 * may call what; these methods are simply grouped by the role that uses them, matching
 * AppointmentService.
 */
@Injectable({ providedIn: 'root' })
export class InsuranceRequestService {
  private readonly http = inject(HttpClient);
  private readonly patientUrl = `${environment.apiBaseUrl}/patient/insurance-requests`;
  private readonly hospitalUrl = `${environment.apiBaseUrl}/hospital/insurance-requests`;

  // ------------------------------------------------------------- Patient side

  getPatientRequests(query: PatientInsuranceRequestQuery): Observable<PagedResult<PatientInsuranceRequest>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) params = params.set('status', query.status);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.hospitalName?.trim()) params = params.set('hospitalName', query.hospitalName.trim());
    if (query.insuranceCompanyId) params = params.set('insuranceCompanyId', query.insuranceCompanyId);

    return this.http
      .get<ApiResponse<PagedResult<PatientInsuranceRequest>>>(this.patientUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getPatientRequest(id: string): Observable<PatientInsuranceRequest> {
    return this.http
      .get<ApiResponse<PatientInsuranceRequest>>(`${this.patientUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  createRequest(request: CreateInsuranceRequestRequest): Observable<ApiResponse<PatientInsuranceRequest>> {
    return this.http.post<ApiResponse<PatientInsuranceRequest>>(this.patientUrl, request);
  }

  cancelRequest(id: string): Observable<ApiResponse<PatientInsuranceRequest>> {
    return this.http.patch<ApiResponse<PatientInsuranceRequest>>(`${this.patientUrl}/${id}/cancel`, {});
  }

  getPatientDashboardStats(): Observable<PatientInsuranceDashboardStats> {
    return this.http
      .get<ApiResponse<PatientInsuranceDashboardStats>>(`${this.patientUrl}/dashboard-stats`)
      .pipe(map((response) => response.data!));
  }

  // ------------------------------------------------------------ Hospital side

  getHospitalRequests(query: HospitalInsuranceRequestQuery): Observable<PagedResult<HospitalInsuranceRequest>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) params = params.set('status', query.status);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.patientName?.trim()) params = params.set('patientName', query.patientName.trim());
    if (query.doctorName?.trim()) params = params.set('doctorName', query.doctorName.trim());
    if (query.insuranceCompanyId) params = params.set('insuranceCompanyId', query.insuranceCompanyId);

    return this.http
      .get<ApiResponse<PagedResult<HospitalInsuranceRequest>>>(this.hospitalUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getHospitalRequest(id: string): Observable<HospitalInsuranceRequest> {
    return this.http
      .get<ApiResponse<HospitalInsuranceRequest>>(`${this.hospitalUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  startReview(id: string): Observable<ApiResponse<HospitalInsuranceRequest>> {
    return this.http.patch<ApiResponse<HospitalInsuranceRequest>>(`${this.hospitalUrl}/${id}/start-review`, {});
  }

  approve(id: string, request: ApproveInsuranceRequestRequest): Observable<ApiResponse<HospitalInsuranceRequest>> {
    return this.http.patch<ApiResponse<HospitalInsuranceRequest>>(`${this.hospitalUrl}/${id}/approve`, request);
  }

  reject(id: string, request: RejectInsuranceRequestRequest): Observable<ApiResponse<HospitalInsuranceRequest>> {
    return this.http.patch<ApiResponse<HospitalInsuranceRequest>>(`${this.hospitalUrl}/${id}/reject`, request);
  }

  updateHospitalNotes(
    id: string,
    request: InsuranceHospitalNotesRequest,
  ): Observable<ApiResponse<HospitalInsuranceRequest>> {
    return this.http.put<ApiResponse<HospitalInsuranceRequest>>(`${this.hospitalUrl}/${id}/notes`, request);
  }

  getHospitalDashboardStats(): Observable<HospitalInsuranceDashboardStats> {
    return this.http
      .get<ApiResponse<HospitalInsuranceDashboardStats>>(`${this.hospitalUrl}/dashboard-stats`)
      .pipe(map((response) => response.data!));
  }
}
