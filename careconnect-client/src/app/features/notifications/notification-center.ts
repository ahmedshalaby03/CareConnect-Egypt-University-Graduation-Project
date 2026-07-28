import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { friendlyMessageOf } from '../../core/interceptors/error.interceptor';
import {
  AppNotification,
  NOTIFICATION_CATEGORIES,
  NOTIFICATION_CATEGORY_LABELS,
  NotificationCategory,
} from '../../core/models/notification.model';
import { InAppNotificationService } from '../../core/services/in-app-notification.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-notification-center',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './notification-center.html',
  styleUrl: './notification-center.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationCenter implements OnInit {
  private readonly notifications = inject(InAppNotificationService);
  private readonly toast = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly categories = NOTIFICATION_CATEGORIES;
  protected readonly categoryLabels = NOTIFICATION_CATEGORY_LABELS;
  protected readonly unreadCount = this.notifications.unreadCount;
  protected readonly items = signal<AppNotification[]>([]);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  protected readonly filters = new FormGroup({
    isRead: new FormControl<boolean | null>(null),
    category: new FormControl<NotificationCategory | null>(null),
    search: new FormControl('', { nonNullable: true }),
    dateFrom: new FormControl('', { nonNullable: true }),
    dateTo: new FormControl('', { nonNullable: true }),
  });

  ngOnInit(): void {
    this.notifications.refreshUnreadCount().subscribe();
    this.load();
  }

  protected applyFilters(): void {
    this.pageIndex.set(0);
    this.load();
  }

  protected clearFilters(): void {
    this.filters.reset({
      isRead: null,
      category: null,
      search: '',
      dateFrom: '',
      dateTo: '',
    });
    this.applyFilters();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected open(item: AppNotification): void {
    const navigate = () => {
      const route = this.safeInternalRoute(item.actionRoute);
      void this.router.navigateByUrl(route);
    };

    if (item.isRead) {
      navigate();
      return;
    }

    this.busyId.set(item.id);
    this.notifications.markAsRead(item.id)
      .pipe(finalize(() => this.busyId.set(null)))
      .subscribe({
        next: () => {
          this.notifications.unreadCount.update((count) => Math.max(0, count - 1));
          navigate();
        },
        error: (error: unknown) =>
          this.toast.error(friendlyMessageOf(error, 'Could not open this notification.')),
      });
  }

  protected toggleRead(item: AppNotification): void {
    this.busyId.set(item.id);
    const request = item.isRead
      ? this.notifications.markAsUnread(item.id)
      : this.notifications.markAsRead(item.id);

    request.pipe(finalize(() => this.busyId.set(null))).subscribe({
      next: (updated) => {
        this.items.update((items) => items.map((value) => value.id === updated.id ? updated : value));
        this.notifications.unreadCount.update((count) =>
          updated.isRead ? Math.max(0, count - 1) : count + 1);
      },
      error: (error: unknown) =>
        this.toast.error(friendlyMessageOf(error, 'Could not update this notification.')),
    });
  }

  protected dismiss(item: AppNotification): void {
    this.busyId.set(item.id);
    this.notifications.dismiss(item.id)
      .pipe(finalize(() => this.busyId.set(null)))
      .subscribe({
        next: () => {
          this.notifications.refreshUnreadCount().subscribe();
          this.load();
        },
        error: (error: unknown) =>
          this.toast.error(friendlyMessageOf(error, 'Could not dismiss this notification.')),
      });
  }

  protected markAllRead(): void {
    this.notifications.markAllAsRead().subscribe({
      next: () => this.load(),
      error: (error: unknown) =>
        this.toast.error(friendlyMessageOf(error, 'Could not mark notifications as read.')),
    });
  }

  protected iconFor(item: AppNotification): string {
    switch (item.category) {
      case 1: return 'event';
      case 2: return 'fact_check';
      case 3: return 'bloodtype';
      case 4: return 'medical_services';
      case 5: return 'handshake';
      case 6: return 'star';
      case 7: return 'manage_accounts';
      default: return 'notifications';
    }
  }

  protected typeClass(item: AppNotification): string {
    switch (item.type) {
      case 2: return 'notification--success';
      case 3: return 'notification--warning';
      case 4: return 'notification--actionrequired';
      default: return 'notification--information';
    }
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    const value = this.filters.getRawValue();
    this.notifications.getAll({
      isRead: value.isRead,
      category: value.category,
      search: value.search,
      dateFrom: value.dateFrom || undefined,
      dateTo: value.dateTo || undefined,
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      sortDirection: 'desc',
    }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        const message = friendlyMessageOf(error, 'Could not load notifications.');
        this.errorMessage.set(message);
        this.toast.error(message);
      },
    });
  }

  private safeInternalRoute(route: string | null): string {
    if (!route || !route.startsWith('/') || route.includes('//') ||
        route.includes('://') || route.toLowerCase().startsWith('javascript:')) {
      return '/notifications';
    }
    return route;
  }
}
