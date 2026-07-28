import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  DoctorDashboard,
  HospitalDashboard,
  MedicalServiceProviderDashboard,
  PatientDashboard,
  SuperAdminDashboard,
} from '../models/dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/dashboard`;

  getPatient(): Observable<PatientDashboard> {
    return this.get<PatientDashboard>('patient');
  }

  getDoctor(): Observable<DoctorDashboard> {
    return this.get<DoctorDashboard>('doctor');
  }

  getHospital(): Observable<HospitalDashboard> {
    return this.get<HospitalDashboard>('hospital');
  }

  getMedicalServiceProvider(): Observable<MedicalServiceProviderDashboard> {
    return this.get<MedicalServiceProviderDashboard>('medical-service-provider');
  }

  getSuperAdmin(): Observable<SuperAdminDashboard> {
    return this.get<SuperAdminDashboard>('super-admin');
  }

  private get<T>(rolePath: string): Observable<T> {
    return this.http
      .get<ApiResponse<T>>(`${this.baseUrl}/${rolePath}`)
      .pipe(map((response) => response.data!));
  }
}
