import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  BookAppointmentRequest,
  CancelAppointmentRequest,
  DoctorAppointment,
  DoctorAppointmentQuery,
  DoctorDashboardStats,
  DoctorNotesRequest,
  HospitalAppointment,
  HospitalAppointmentQuery,
  HospitalDashboardStats,
  PatientAppointment,
  PatientAppointmentQuery,
  PatientDashboardStats,
  RejectAppointmentRequest,
} from '../models/appointment.model';

/**
 * All three sides of an appointment - Patient, Doctor and Hospital. The API decides who may
 * call what; these methods are simply grouped by the role that uses them, matching
 * AffiliationService.
 */
@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private readonly http = inject(HttpClient);
  private readonly patientUrl = `${environment.apiBaseUrl}/patient/appointments`;
  private readonly doctorUrl = `${environment.apiBaseUrl}/doctor/appointments`;
  private readonly hospitalUrl = `${environment.apiBaseUrl}/hospital/appointments`;

  // ------------------------------------------------------------- Patient side

  getPatientAppointments(query: PatientAppointmentQuery): Observable<PagedResult<PatientAppointment>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) params = params.set('status', query.status);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.doctorName?.trim()) params = params.set('doctorName', query.doctorName.trim());
    if (query.hospitalName?.trim()) params = params.set('hospitalName', query.hospitalName.trim());

    return this.http
      .get<ApiResponse<PagedResult<PatientAppointment>>>(this.patientUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getPatientAppointment(id: string): Observable<PatientAppointment> {
    return this.http
      .get<ApiResponse<PatientAppointment>>(`${this.patientUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  bookAppointment(request: BookAppointmentRequest): Observable<ApiResponse<PatientAppointment>> {
    return this.http.post<ApiResponse<PatientAppointment>>(this.patientUrl, request);
  }

  cancelByPatient(
    id: string,
    request: CancelAppointmentRequest,
  ): Observable<ApiResponse<PatientAppointment>> {
    return this.http.patch<ApiResponse<PatientAppointment>>(`${this.patientUrl}/${id}/cancel`, request);
  }

  getPatientDashboardStats(): Observable<PatientDashboardStats> {
    return this.http
      .get<ApiResponse<PatientDashboardStats>>(`${this.patientUrl}/dashboard-stats`)
      .pipe(map((response) => response.data!));
  }

  // -------------------------------------------------------------- Doctor side

  getDoctorAppointments(query: DoctorAppointmentQuery): Observable<PagedResult<DoctorAppointment>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) params = params.set('status', query.status);
    if (query.date) params = params.set('date', query.date);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.hospitalProfileId) params = params.set('hospitalProfileId', query.hospitalProfileId);
    if (query.patientName?.trim()) params = params.set('patientName', query.patientName.trim());

    return this.http
      .get<ApiResponse<PagedResult<DoctorAppointment>>>(this.doctorUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getDoctorAppointment(id: string): Observable<DoctorAppointment> {
    return this.http
      .get<ApiResponse<DoctorAppointment>>(`${this.doctorUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  confirmAppointment(id: string): Observable<ApiResponse<DoctorAppointment>> {
    return this.http.patch<ApiResponse<DoctorAppointment>>(`${this.doctorUrl}/${id}/confirm`, {});
  }

  rejectAppointment(
    id: string,
    request: RejectAppointmentRequest,
  ): Observable<ApiResponse<DoctorAppointment>> {
    return this.http.patch<ApiResponse<DoctorAppointment>>(`${this.doctorUrl}/${id}/reject`, request);
  }

  cancelByDoctor(
    id: string,
    request: CancelAppointmentRequest,
  ): Observable<ApiResponse<DoctorAppointment>> {
    return this.http.patch<ApiResponse<DoctorAppointment>>(`${this.doctorUrl}/${id}/cancel`, request);
  }

  completeAppointment(id: string): Observable<ApiResponse<DoctorAppointment>> {
    return this.http.patch<ApiResponse<DoctorAppointment>>(`${this.doctorUrl}/${id}/complete`, {});
  }

  markNoShow(id: string): Observable<ApiResponse<DoctorAppointment>> {
    return this.http.patch<ApiResponse<DoctorAppointment>>(`${this.doctorUrl}/${id}/no-show`, {});
  }

  updateDoctorNotes(
    id: string,
    request: DoctorNotesRequest,
  ): Observable<ApiResponse<DoctorAppointment>> {
    return this.http.put<ApiResponse<DoctorAppointment>>(`${this.doctorUrl}/${id}/notes`, request);
  }

  getDoctorDashboardStats(): Observable<DoctorDashboardStats> {
    return this.http
      .get<ApiResponse<DoctorDashboardStats>>(`${this.doctorUrl}/dashboard-stats`)
      .pipe(map((response) => response.data!));
  }

  // ------------------------------------------------------------ Hospital side

  getHospitalAppointments(query: HospitalAppointmentQuery): Observable<PagedResult<HospitalAppointment>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) params = params.set('status', query.status);
    if (query.date) params = params.set('date', query.date);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.doctorProfileId) params = params.set('doctorProfileId', query.doctorProfileId);
    if (query.doctorName?.trim()) params = params.set('doctorName', query.doctorName.trim());
    if (query.patientName?.trim()) params = params.set('patientName', query.patientName.trim());

    return this.http
      .get<ApiResponse<PagedResult<HospitalAppointment>>>(this.hospitalUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getHospitalAppointment(id: string): Observable<HospitalAppointment> {
    return this.http
      .get<ApiResponse<HospitalAppointment>>(`${this.hospitalUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  getHospitalDashboardStats(): Observable<HospitalDashboardStats> {
    return this.http
      .get<ApiResponse<HospitalDashboardStats>>(`${this.hospitalUrl}/dashboard-stats`)
      .pipe(map((response) => response.data!));
  }
}
