import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  AcceptMedicalServiceRequest,
  CancelMedicalServiceRequest,
  CreateMedicalServiceRequest,
  MedicalServiceRequestDashboardSummary,
  MedicalServiceRequestDetails,
  MedicalServiceRequestPage,
  PatientMedicalServiceRequestFilter,
  ProviderMedicalServiceRequestFilter,
  RejectMedicalServiceRequest,
} from '../models/medical-service-request.model';

@Injectable({ providedIn: 'root' })
export class MedicalServiceRequestService {
  private readonly http = inject(HttpClient);
  private readonly patientUrl = `${environment.apiBaseUrl}/patient/medical-service-requests`;
  private readonly providerUrl = `${environment.apiBaseUrl}/medical-service-provider/requests`;

  create(
    request: CreateMedicalServiceRequest,
  ): Observable<ApiResponse<MedicalServiceRequestDetails>> {
    return this.http.post<ApiResponse<MedicalServiceRequestDetails>>(this.patientUrl, request);
  }

  getPatientRequests(
    filter: PatientMedicalServiceRequestFilter,
  ): Observable<MedicalServiceRequestPage> {
    return this.http
      .get<ApiResponse<MedicalServiceRequestPage>>(this.patientUrl, {
        params: this.buildParams(filter),
      })
      .pipe(map((response) => response.data!));
  }

  getPatientRequest(id: string): Observable<MedicalServiceRequestDetails> {
    return this.http
      .get<ApiResponse<MedicalServiceRequestDetails>>(`${this.patientUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  cancelByPatient(
    id: string,
    request: CancelMedicalServiceRequest,
  ): Observable<ApiResponse<MedicalServiceRequestDetails>> {
    return this.http.post<ApiResponse<MedicalServiceRequestDetails>>(
      `${this.patientUrl}/${id}/cancel`,
      request,
    );
  }

  getProviderRequests(
    filter: ProviderMedicalServiceRequestFilter,
  ): Observable<MedicalServiceRequestPage> {
    return this.http
      .get<ApiResponse<MedicalServiceRequestPage>>(this.providerUrl, {
        params: this.buildParams(filter),
      })
      .pipe(map((response) => response.data!));
  }

  getProviderRequest(id: string): Observable<MedicalServiceRequestDetails> {
    return this.http
      .get<ApiResponse<MedicalServiceRequestDetails>>(`${this.providerUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  getProviderSummary(): Observable<MedicalServiceRequestDashboardSummary> {
    return this.http
      .get<ApiResponse<MedicalServiceRequestDashboardSummary>>(`${this.providerUrl}/summary`)
      .pipe(map((response) => response.data!));
  }

  accept(
    id: string,
    request: AcceptMedicalServiceRequest,
  ): Observable<ApiResponse<MedicalServiceRequestDetails>> {
    return this.http.post<ApiResponse<MedicalServiceRequestDetails>>(
      `${this.providerUrl}/${id}/accept`,
      request,
    );
  }

  reject(
    id: string,
    request: RejectMedicalServiceRequest,
  ): Observable<ApiResponse<MedicalServiceRequestDetails>> {
    return this.http.post<ApiResponse<MedicalServiceRequestDetails>>(
      `${this.providerUrl}/${id}/reject`,
      request,
    );
  }

  cancelByProvider(
    id: string,
    request: CancelMedicalServiceRequest,
  ): Observable<ApiResponse<MedicalServiceRequestDetails>> {
    return this.http.post<ApiResponse<MedicalServiceRequestDetails>>(
      `${this.providerUrl}/${id}/cancel`,
      request,
    );
  }

  complete(id: string): Observable<ApiResponse<MedicalServiceRequestDetails>> {
    return this.http.post<ApiResponse<MedicalServiceRequestDetails>>(
      `${this.providerUrl}/${id}/complete`,
      {},
    );
  }

  private buildParams(
    filter: PatientMedicalServiceRequestFilter | ProviderMedicalServiceRequestFilter,
  ): HttpParams {
    let params = new HttpParams()
      .set('page', filter.page)
      .set('pageSize', filter.pageSize);
    for (const [key, value] of Object.entries(filter)) {
      if (
        key !== 'page' &&
        key !== 'pageSize' &&
        value !== null &&
        value !== undefined &&
        value !== ''
      ) {
        params = params.set(key, String(value));
      }
    }
    return params;
  }
}
