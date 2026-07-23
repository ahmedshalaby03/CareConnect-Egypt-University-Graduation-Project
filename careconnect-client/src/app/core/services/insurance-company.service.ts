import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  InsuranceCompany,
  InsuranceCompanyOption,
  InsuranceCompanyQuery,
  InsuranceCompanyRequest,
  ToggleInsuranceCompanyStatusResult,
} from '../models/insurance-company.model';

@Injectable({ providedIn: 'root' })
export class InsuranceCompanyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/insurance-companies`;
  private readonly adminUrl = `${environment.apiBaseUrl}/super-admin/insurance-companies`;

  /**
   * Cached because the active list is read on every insurance request form and only
   * changes when a SuperAdmin edits it. Admin mutations clear it below.
   */
  private readonly cache = signal<InsuranceCompanyOption[] | null>(null);

  /** Active insurance companies, alphabetical. Never hard-coded in a component. */
  getActive(forceReload = false): Observable<InsuranceCompanyOption[]> {
    const cached = this.cache();
    if (cached && !forceReload) {
      return of(cached);
    }

    return this.http.get<ApiResponse<InsuranceCompanyOption[]>>(this.baseUrl).pipe(
      map((response) => response.data ?? []),
      tap((items) => this.cache.set(items)),
    );
  }

  // ------------------------------------------------------- SuperAdmin only

  getAll(query: InsuranceCompanyQuery): Observable<PagedResult<InsuranceCompany>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.searchTerm?.trim()) {
      params = params.set('searchTerm', query.searchTerm.trim());
    }

    // Explicit null check: `false` is a meaningful filter value.
    if (query.isActive !== null && query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }

    return this.http
      .get<ApiResponse<PagedResult<InsuranceCompany>>>(this.adminUrl, { params })
      .pipe(map((response) => response.data!));
  }

  create(request: InsuranceCompanyRequest): Observable<ApiResponse<InsuranceCompany>> {
    return this.http
      .post<ApiResponse<InsuranceCompany>>(this.adminUrl, request)
      .pipe(tap(() => this.invalidate()));
  }

  update(id: string, request: InsuranceCompanyRequest): Observable<ApiResponse<InsuranceCompany>> {
    return this.http
      .put<ApiResponse<InsuranceCompany>>(`${this.adminUrl}/${id}`, request)
      .pipe(tap(() => this.invalidate()));
  }

  toggleStatus(id: string): Observable<ApiResponse<ToggleInsuranceCompanyStatusResult>> {
    return this.http
      .patch<ApiResponse<ToggleInsuranceCompanyStatusResult>>(`${this.adminUrl}/${id}/toggle-status`, {})
      .pipe(tap(() => this.invalidate()));
  }

  /** Drops the cached option list so the next read reflects an admin change. */
  private invalidate(): void {
    this.cache.set(null);
  }
}
