import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  AffiliatedHospital,
  DoctorAffiliationQuery,
  DoctorHospitalRequest,
  HospitalAffiliationQuery,
  HospitalDoctor,
  HospitalDoctorRequest,
} from '../models/affiliation.model';

/**
 * Both sides of the doctor-hospital relationship. The API decides who may call what; these
 * methods are simply grouped by the role that uses them.
 */
@Injectable({ providedIn: 'root' })
export class AffiliationService {
  private readonly http = inject(HttpClient);
  private readonly doctorUrl = `${environment.apiBaseUrl}/doctor`;
  private readonly hospitalUrl = `${environment.apiBaseUrl}/hospital`;

  // -------------------------------------------------------------- Doctor side

  getDoctorRequests(query: DoctorAffiliationQuery): Observable<PagedResult<DoctorHospitalRequest>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) {
      params = params.set('status', query.status);
    }

    if (query.hospitalName?.trim()) {
      params = params.set('hospitalName', query.hospitalName.trim());
    }

    return this.http
      .get<ApiResponse<PagedResult<DoctorHospitalRequest>>>(`${this.doctorUrl}/hospital-requests`, {
        params,
      })
      .pipe(map((response) => response.data!));
  }

  requestAffiliation(hospitalProfileId: string): Observable<ApiResponse<DoctorHospitalRequest>> {
    return this.http.post<ApiResponse<DoctorHospitalRequest>>(
      `${this.doctorUrl}/hospital-requests`,
      { hospitalProfileId },
    );
  }

  cancelRequest(requestId: string): Observable<ApiResponse<DoctorHospitalRequest>> {
    return this.http.patch<ApiResponse<DoctorHospitalRequest>>(
      `${this.doctorUrl}/hospital-requests/${requestId}/cancel`,
      {},
    );
  }

  setPrimaryHospital(hospitalId: string): Observable<ApiResponse<DoctorHospitalRequest>> {
    return this.http.patch<ApiResponse<DoctorHospitalRequest>>(
      `${this.doctorUrl}/hospitals/${hospitalId}/set-primary`,
      {},
    );
  }

  getDoctorHospitals(): Observable<AffiliatedHospital[]> {
    return this.http
      .get<ApiResponse<AffiliatedHospital[]>>(`${this.doctorUrl}/hospitals`)
      .pipe(map((response) => response.data ?? []));
  }

  // ------------------------------------------------------------ Hospital side

  getHospitalRequests(
    query: HospitalAffiliationQuery,
  ): Observable<PagedResult<HospitalDoctorRequest>> {
    return this.http
      .get<ApiResponse<PagedResult<HospitalDoctorRequest>>>(
        `${this.hospitalUrl}/doctor-requests`,
        { params: this.hospitalParams(query) },
      )
      .pipe(map((response) => response.data!));
  }

  approveRequest(requestId: string): Observable<ApiResponse<HospitalDoctorRequest>> {
    return this.http.patch<ApiResponse<HospitalDoctorRequest>>(
      `${this.hospitalUrl}/doctor-requests/${requestId}/approve`,
      {},
    );
  }

  rejectRequest(
    requestId: string,
    rejectionReason: string,
  ): Observable<ApiResponse<HospitalDoctorRequest>> {
    return this.http.patch<ApiResponse<HospitalDoctorRequest>>(
      `${this.hospitalUrl}/doctor-requests/${requestId}/reject`,
      { rejectionReason },
    );
  }

  getHospitalDoctors(query: HospitalAffiliationQuery): Observable<PagedResult<HospitalDoctor>> {
    return this.http
      .get<ApiResponse<PagedResult<HospitalDoctor>>>(`${this.hospitalUrl}/doctors`, {
        params: this.hospitalParams(query),
      })
      .pipe(map((response) => response.data!));
  }

  removeDoctor(doctorProfileId: string): Observable<ApiResponse<HospitalDoctorRequest>> {
    return this.http.patch<ApiResponse<HospitalDoctorRequest>>(
      `${this.hospitalUrl}/doctors/${doctorProfileId}/remove`,
      {},
    );
  }

  private hospitalParams(query: HospitalAffiliationQuery): HttpParams {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) {
      params = params.set('status', query.status);
    }

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.specialtyId) {
      params = params.set('specialtyId', query.specialtyId);
    }

    return params;
  }
}
