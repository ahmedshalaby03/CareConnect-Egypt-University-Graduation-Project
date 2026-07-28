import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  AppNotification,
  NotificationFilter,
  NotificationUnreadCount,
} from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class InAppNotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/notifications`;

  readonly unreadCount = signal(0);

  getAll(filter: NotificationFilter): Observable<PagedResult<AppNotification>> {
    let params = new HttpParams()
      .set('page', filter.page)
      .set('pageSize', filter.pageSize)
      .set('sortDirection', filter.sortDirection ?? 'desc');

    if (filter.isRead !== null && filter.isRead !== undefined) {
      params = params.set('isRead', filter.isRead);
    }
    if (filter.category) {
      params = params.set('category', filter.category);
    }
    if (filter.search?.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.dateFrom) {
      params = params.set('dateFrom', filter.dateFrom);
    }
    if (filter.dateTo) {
      params = params.set('dateTo', filter.dateTo);
    }

    return this.http
      .get<ApiResponse<PagedResult<AppNotification>>>(this.baseUrl, { params })
      .pipe(map((response) => response.data!));
  }

  getRecent(): Observable<AppNotification[]> {
    return this.http
      .get<ApiResponse<AppNotification[]>>(`${this.baseUrl}/recent`)
      .pipe(map((response) => response.data ?? []));
  }

  refreshUnreadCount(): Observable<number> {
    return this.http
      .get<ApiResponse<NotificationUnreadCount>>(`${this.baseUrl}/unread-count`)
      .pipe(
        map((response) => response.data?.unreadCount ?? 0),
        tap((count) => this.unreadCount.set(count)),
      );
  }

  markAsRead(id: string): Observable<AppNotification> {
    return this.http
      .patch<ApiResponse<AppNotification>>(`${this.baseUrl}/${id}/read`, {})
      .pipe(map((response) => response.data!));
  }

  markAsUnread(id: string): Observable<AppNotification> {
    return this.http
      .patch<ApiResponse<AppNotification>>(`${this.baseUrl}/${id}/unread`, {})
      .pipe(map((response) => response.data!));
  }

  markAllAsRead(): Observable<number> {
    return this.http
      .post<ApiResponse<NotificationUnreadCount>>(`${this.baseUrl}/mark-all-read`, {})
      .pipe(
        map((response) => response.data?.unreadCount ?? 0),
        tap((count) => this.unreadCount.set(count)),
      );
  }

  dismiss(id: string): Observable<boolean> {
    return this.http
      .delete<ApiResponse<boolean>>(`${this.baseUrl}/${id}`)
      .pipe(map((response) => response.data ?? false));
  }
}
