import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { map, Observable, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  MedicalServiceCategory,
  MedicalServiceCategoryOption,
  MedicalServiceCategoryQuery,
  MedicalServiceCategoryRequest,
  MedicalServiceOffering,
  MedicalServiceOfferingRequest,
  MedicalServiceProviderDetails,
  MedicalServiceProviderFilter,
  MedicalServiceProviderPreview,
  MedicalServiceProviderProfile,
  MedicalServiceProviderSummary,
  ProviderWorkingHour,
  UpdateMedicalServiceProviderProfileRequest,
  UpdateProviderWorkingHoursRequest,
} from '../models/medical-service-provider.model';

@Injectable({ providedIn: 'root' })
export class MedicalServiceProviderService {
  private readonly http = inject(HttpClient);
  private readonly managementUrl = `${environment.apiBaseUrl}/medical-service-provider`;
  private readonly directoryUrl = `${environment.apiBaseUrl}/medical-service-providers`;
  private readonly categoriesUrl = `${environment.apiBaseUrl}/medical-service-categories`;
  private readonly adminCategoriesUrl =
    `${environment.apiBaseUrl}/super-admin/medical-service-categories`;
  private readonly categoryCache = signal<MedicalServiceCategoryOption[] | null>(null);

  getProfile(): Observable<MedicalServiceProviderProfile> {
    return this.http
      .get<ApiResponse<MedicalServiceProviderProfile>>(`${this.managementUrl}/profile`)
      .pipe(map((response) => response.data!));
  }

  updateProfile(
    request: UpdateMedicalServiceProviderProfileRequest,
  ): Observable<ApiResponse<MedicalServiceProviderProfile>> {
    return this.http.put<ApiResponse<MedicalServiceProviderProfile>>(
      `${this.managementUrl}/profile`,
      request,
    );
  }

  setPublication(isPublished: boolean): Observable<ApiResponse<MedicalServiceProviderProfile>> {
    return this.http.patch<ApiResponse<MedicalServiceProviderProfile>>(
      `${this.managementUrl}/profile/publication`,
      { isPublished },
    );
  }

  getPreview(): Observable<MedicalServiceProviderPreview> {
    return this.http
      .get<ApiResponse<MedicalServiceProviderPreview>>(`${this.managementUrl}/preview`)
      .pipe(map((response) => response.data!));
  }

  getServices(): Observable<MedicalServiceOffering[]> {
    return this.http
      .get<ApiResponse<MedicalServiceOffering[]>>(`${this.managementUrl}/services`)
      .pipe(map((response) => response.data ?? []));
  }

  createService(
    request: MedicalServiceOfferingRequest,
  ): Observable<ApiResponse<MedicalServiceOffering>> {
    return this.http.post<ApiResponse<MedicalServiceOffering>>(
      `${this.managementUrl}/services`,
      request,
    );
  }

  updateService(
    id: string,
    request: MedicalServiceOfferingRequest,
  ): Observable<ApiResponse<MedicalServiceOffering>> {
    return this.http.put<ApiResponse<MedicalServiceOffering>>(
      `${this.managementUrl}/services/${id}`,
      request,
    );
  }

  setServiceStatus(
    id: string,
    isActive: boolean,
  ): Observable<ApiResponse<MedicalServiceOffering>> {
    return this.http.patch<ApiResponse<MedicalServiceOffering>>(
      `${this.managementUrl}/services/${id}/status`,
      { isActive },
    );
  }

  getWorkingHours(): Observable<ProviderWorkingHour[]> {
    return this.http
      .get<ApiResponse<ProviderWorkingHour[]>>(`${this.managementUrl}/working-hours`)
      .pipe(map((response) => response.data ?? []));
  }

  updateWorkingHours(
    request: UpdateProviderWorkingHoursRequest,
  ): Observable<ApiResponse<ProviderWorkingHour[]>> {
    return this.http.put<ApiResponse<ProviderWorkingHour[]>>(
      `${this.managementUrl}/working-hours`,
      request,
    );
  }

  getActiveCategories(forceReload = false): Observable<MedicalServiceCategoryOption[]> {
    const cached = this.categoryCache();
    if (cached && !forceReload) {
      return of(cached);
    }

    return this.http
      .get<ApiResponse<MedicalServiceCategoryOption[]>>(this.categoriesUrl)
      .pipe(
        map((response) => response.data ?? []),
        tap((items) => this.categoryCache.set(items)),
      );
  }

  search(filter: MedicalServiceProviderFilter): Observable<PagedResult<MedicalServiceProviderSummary>> {
    let params = new HttpParams()
      .set('page', filter.page)
      .set('pageSize', filter.pageSize)
      .set('radiusKm', filter.radiusKm ?? 25)
      .set('sortBy', filter.sortBy ?? 'name');

    const values: Record<string, string | number | null | undefined> = {
      search: filter.search?.trim(),
      providerType: filter.providerType,
      categoryId: filter.categoryId,
      governorate: filter.governorate?.trim(),
      city: filter.city?.trim(),
      latitude: filter.latitude,
      longitude: filter.longitude,
    };
    for (const [key, value] of Object.entries(values)) {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, value);
      }
    }

    return this.http
      .get<ApiResponse<PagedResult<MedicalServiceProviderSummary>>>(this.directoryUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getDetails(
    id: string,
    latitude?: number | null,
    longitude?: number | null,
  ): Observable<MedicalServiceProviderDetails> {
    let params = new HttpParams();
    if (latitude !== null && latitude !== undefined && longitude !== null && longitude !== undefined) {
      params = params.set('latitude', latitude).set('longitude', longitude);
    }
    return this.http
      .get<ApiResponse<MedicalServiceProviderDetails>>(`${this.directoryUrl}/${id}`, { params })
      .pipe(map((response) => response.data!));
  }

  getCategories(query: MedicalServiceCategoryQuery): Observable<PagedResult<MedicalServiceCategory>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search?.trim()) params = params.set('search', query.search.trim());
    if (query.isActive !== null && query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }
    return this.http
      .get<ApiResponse<PagedResult<MedicalServiceCategory>>>(this.adminCategoriesUrl, { params })
      .pipe(map((response) => response.data!));
  }

  createCategory(
    request: MedicalServiceCategoryRequest,
  ): Observable<ApiResponse<MedicalServiceCategory>> {
    return this.http
      .post<ApiResponse<MedicalServiceCategory>>(this.adminCategoriesUrl, request)
      .pipe(tap(() => this.categoryCache.set(null)));
  }

  updateCategory(
    id: string,
    request: MedicalServiceCategoryRequest,
  ): Observable<ApiResponse<MedicalServiceCategory>> {
    return this.http
      .put<ApiResponse<MedicalServiceCategory>>(`${this.adminCategoriesUrl}/${id}`, request)
      .pipe(tap(() => this.categoryCache.set(null)));
  }

  setCategoryStatus(
    id: string,
    isActive: boolean,
  ): Observable<ApiResponse<MedicalServiceCategory>> {
    return this.http
      .patch<ApiResponse<MedicalServiceCategory>>(
        `${this.adminCategoriesUrl}/${id}/status`,
        { isActive },
      )
      .pipe(tap(() => this.categoryCache.set(null)));
  }
}
