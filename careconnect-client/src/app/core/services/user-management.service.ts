import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { User, UserRole } from '../models/user.model';

export interface UserQuery {
  search?: string | null;
  role?: UserRole | null;
  isActive?: boolean | null;
  page: number;
  pageSize: number;
}

export interface ToggleStatusResult {
  userId: string;
  fullName: string;
  isActive: boolean;
}

/** SuperAdmin-only endpoints. The API enforces the role; this service just calls it. */
@Injectable({ providedIn: 'root' })
export class UserManagementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/super-admin`;

  getUsers(query: UserQuery): Observable<PagedResult<User>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.role) {
      params = params.set('role', query.role);
    }

    // Explicit null check: `false` is a meaningful filter value here.
    if (query.isActive !== null && query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }

    return this.http
      .get<ApiResponse<PagedResult<User>>>(`${this.baseUrl}/users`, { params })
      .pipe(map((response) => response.data!));
  }

  toggleStatus(userId: string): Observable<ApiResponse<ToggleStatusResult>> {
    return this.http.patch<ApiResponse<ToggleStatusResult>>(
      `${this.baseUrl}/users/${userId}/toggle-status`,
      {},
    );
  }
}
