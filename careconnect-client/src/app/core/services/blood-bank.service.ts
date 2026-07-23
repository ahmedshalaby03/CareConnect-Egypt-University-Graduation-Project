import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { BloodAvailability, BloodAvailabilityQuery, HospitalBloodBankDetails } from '../models/blood-bank.model';

@Injectable({ providedIn: 'root' })
export class BloodBankService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/blood-bank`;

  searchAvailability(query: BloodAvailabilityQuery): Observable<PagedResult<BloodAvailability>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.bloodGroup) params = params.set('bloodGroup', query.bloodGroup);
    if (query.governorate) params = params.set('governorate', query.governorate);
    if (query.city) params = params.set('city', query.city);
    if (query.hospitalName?.trim()) params = params.set('hospitalName', query.hospitalName.trim());
    if (query.availableOnly !== null && query.availableOnly !== undefined) {
      params = params.set('availableOnly', query.availableOnly);
    }

    return this.http
      .get<ApiResponse<PagedResult<BloodAvailability>>>(`${this.baseUrl}/availability`, { params })
      .pipe(map((response) => response.data!));
  }

  getHospitalBloodBank(hospitalProfileId: string): Observable<HospitalBloodBankDetails> {
    return this.http
      .get<ApiResponse<HospitalBloodBankDetails>>(`${this.baseUrl}/hospitals/${hospitalProfileId}`)
      .pipe(map((response) => response.data!));
  }
}
