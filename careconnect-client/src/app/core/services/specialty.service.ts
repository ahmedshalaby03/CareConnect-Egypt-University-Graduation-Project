import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  Specialty,
  SpecialtyOption,
  SpecialtyQuery,
  SpecialtyRequest,
  ToggleSpecialtyStatusResult,
} from '../models/specialty.model';

@Injectable({ providedIn: 'root' })
export class SpecialtyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/specialties`;
  private readonly adminUrl = `${environment.apiBaseUrl}/super-admin/specialties`;

  /**
   * Cached because the active list is read on nearly every screen and only changes when a
   * SuperAdmin edits it. Admin mutations clear it below.
   */
  private readonly cache = signal<SpecialtyOption[] | null>(null);

  /** Active specialties, alphabetical. Never hard-coded in a component. */
  getActive(forceReload = false): Observable<SpecialtyOption[]> {
    const cached = this.cache();
    if (cached && !forceReload) {
      return of(cached);
    }

    return this.http.get<ApiResponse<SpecialtyOption[]>>(this.baseUrl).pipe(
      map((response) => response.data ?? []),
      tap((items) => this.cache.set(items)),
    );
  }

  // ------------------------------------------------------- SuperAdmin only

  getAll(query: SpecialtyQuery): Observable<PagedResult<Specialty>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    // Explicit null check: `false` is a meaningful filter value.
    if (query.isActive !== null && query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }

    return this.http
      .get<ApiResponse<PagedResult<Specialty>>>(this.adminUrl, { params })
      .pipe(map((response) => response.data!));
  }

  create(request: SpecialtyRequest): Observable<ApiResponse<Specialty>> {
    return this.http
      .post<ApiResponse<Specialty>>(this.adminUrl, request)
      .pipe(tap(() => this.invalidate()));
  }

  update(id: string, request: SpecialtyRequest): Observable<ApiResponse<Specialty>> {
    return this.http
      .put<ApiResponse<Specialty>>(`${this.adminUrl}/${id}`, request)
      .pipe(tap(() => this.invalidate()));
  }

  toggleStatus(id: string): Observable<ApiResponse<ToggleSpecialtyStatusResult>> {
    return this.http
      .patch<ApiResponse<ToggleSpecialtyStatusResult>>(`${this.adminUrl}/${id}/toggle-status`, {})
      .pipe(tap(() => this.invalidate()));
  }

  /** Drops the cached option list so the next read reflects an admin change. */
  private invalidate(): void {
    this.cache.set(null);
  }
}
