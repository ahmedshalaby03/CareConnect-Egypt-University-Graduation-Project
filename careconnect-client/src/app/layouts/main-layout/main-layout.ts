import { DatePipe, DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { fromEvent, merge, Observable, timer } from 'rxjs';
import { AppNotification } from '../../core/models/notification.model';
import { ROLE_LABELS, UserRole } from '../../core/models/user.model';
import { AuthService } from '../../core/services/auth.service';
import { InAppNotificationService } from '../../core/services/in-app-notification.service';

interface NavLink {
  label: string;
  route: string;
  icon: string;
  /** Match the whole URL rather than a prefix, for links that are a parent of others. */
  exact?: boolean;
}

/** Everything a role can reach from the top bar. The API authorises each call regardless. */
const NAV_BY_ROLE: Record<UserRole, NavLink[]> = {
  Patient: [
    { label: 'Dashboard', route: '/dashboard/patient', icon: 'dashboard', exact: true },
    { label: 'Doctors', route: '/doctors', icon: 'medical_information' },
    { label: 'Hospitals', route: '/hospitals', icon: 'local_hospital' },
    { label: 'Medical Services', route: '/medical-service-providers', icon: 'health_and_safety' },
    { label: 'Service Requests', route: '/dashboard/patient/service-requests', icon: 'assignment_turned_in' },
    { label: 'My Reviews', route: '/dashboard/patient/reviews', icon: 'rate_review' },
    { label: 'My appointments', route: '/dashboard/patient/appointments', icon: 'event_note' },
    { label: 'Insurance requests', route: '/dashboard/patient/insurance-requests', icon: 'fact_check' },
    { label: 'Blood bank', route: '/blood-bank', icon: 'bloodtype' },
    { label: 'Blood requests', route: '/dashboard/patient/blood-requests', icon: 'water_drop' },
    {
      label: 'AI Medical Assistant',
      route: '/dashboard/patient/ai-assistant',
      icon: 'smart_toy',
    },
    { label: 'Notifications', route: '/notifications', icon: 'notifications' },
  ],
  Doctor: [
    { label: 'Dashboard', route: '/dashboard/doctor', icon: 'dashboard', exact: true },
    { label: 'My profile', route: '/dashboard/doctor/profile', icon: 'badge' },
    { label: 'Find hospitals', route: '/dashboard/doctor/hospitals', icon: 'travel_explore' },
    { label: 'My requests', route: '/dashboard/doctor/hospital-requests', icon: 'assignment' },
    { label: 'Availability', route: '/dashboard/doctor/availability', icon: 'schedule' },
    { label: 'Unavailable periods', route: '/dashboard/doctor/unavailable-periods', icon: 'event_busy' },
    { label: 'Appointments', route: '/dashboard/doctor/appointments', icon: 'event_note' },
    { label: 'Patient Reviews', route: '/dashboard/doctor/reviews', icon: 'star' },
    { label: 'Blood bank', route: '/blood-bank', icon: 'bloodtype' },
    { label: 'Notifications', route: '/notifications', icon: 'notifications' },
  ],
  Hospital: [
    { label: 'Dashboard', route: '/dashboard/hospital', icon: 'dashboard', exact: true },
    { label: 'Profile', route: '/dashboard/hospital/profile', icon: 'domain' },
    { label: 'Location', route: '/dashboard/hospital/location', icon: 'near_me' },
    { label: 'Requests', route: '/dashboard/hospital/doctor-requests', icon: 'how_to_reg' },
    { label: 'Our doctors', route: '/dashboard/hospital/doctors', icon: 'groups' },
    { label: 'Appointments', route: '/dashboard/hospital/appointments', icon: 'event_note' },
    { label: 'Insurance requests', route: '/dashboard/hospital/insurance-requests', icon: 'fact_check' },
    { label: 'Blood stock', route: '/dashboard/hospital/blood-stock', icon: 'bloodtype' },
    { label: 'Blood requests', route: '/dashboard/hospital/blood-requests', icon: 'water_drop' },
    { label: 'Patient Reviews', route: '/dashboard/hospital/reviews', icon: 'star' },
    { label: 'Notifications', route: '/notifications', icon: 'notifications' },
  ],
  MedicalServiceProvider: [
    { label: 'Dashboard', route: '/dashboard/service-provider', icon: 'dashboard', exact: true },
    { label: 'Business Profile', route: '/dashboard/service-provider/profile', icon: 'storefront' },
    { label: 'My Services', route: '/dashboard/service-provider/services', icon: 'medical_services' },
    { label: 'Service Requests', route: '/dashboard/service-provider/requests', icon: 'assignment' },
    { label: 'Patient Reviews', route: '/dashboard/service-provider/reviews', icon: 'star' },
    { label: 'Working Hours', route: '/dashboard/service-provider/working-hours', icon: 'schedule' },
    { label: 'Public Preview', route: '/dashboard/service-provider/preview', icon: 'preview' },
    { label: 'Directory', route: '/medical-service-providers', icon: 'travel_explore' },
    { label: 'Notifications', route: '/notifications', icon: 'notifications' },
  ],
  SuperAdmin: [
    { label: 'Dashboard', route: '/super-admin/dashboard', icon: 'dashboard', exact: true },
    { label: 'Users', route: '/super-admin', icon: 'manage_accounts', exact: true },
    { label: 'Specialties', route: '/super-admin/specialties', icon: 'category' },
    { label: 'Insurance companies', route: '/super-admin/insurance-companies', icon: 'fact_check' },
    { label: 'Service categories', route: '/super-admin/medical-service-categories', icon: 'category' },
    { label: 'Reviews', route: '/super-admin/reviews', icon: 'policy' },
    { label: 'Doctors', route: '/doctors', icon: 'medical_information' },
    { label: 'Hospitals', route: '/hospitals', icon: 'local_hospital' },
    { label: 'Blood bank', route: '/blood-bank', icon: 'bloodtype' },
    { label: 'Notifications', route: '/notifications', icon: 'notifications' },
  ],
};

/** Application chrome for every signed-in screen: brand bar, navigation, account menu. */
@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    DatePipe,
    MatToolbarModule,
    MatBadgeModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayout {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(InAppNotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);

  protected readonly user = this.auth.currentUser;
  protected readonly unreadCount = this.notifications.unreadCount;
  protected readonly recentNotifications = signal<AppNotification[]>([]);
  protected readonly notificationsLoading = signal(false);
  protected readonly avatarLoadFailed = signal(false);
  protected readonly badgeText = computed(() => this.unreadCount() > 99 ? '99+' : `${this.unreadCount()}`);

  protected readonly navLinks = computed<NavLink[]>(() => {
    const role = this.user()?.role;
    return role ? (NAV_BY_ROLE[role] ?? []) : [];
  });

  protected readonly roleLabel = computed(() => {
    const role = this.user()?.role;
    return role ? ROLE_LABELS[role] : '';
  });

  protected readonly initials = computed(() => {
    const name = this.user()?.fullName?.trim();
    if (!name) {
      return '?';
    }

    const parts = name.split(/\s+/).filter(Boolean);
    const letters = parts.length > 1 ? `${parts[0][0]}${parts[parts.length - 1][0]}` : parts[0][0];

    return letters.toUpperCase();
  });

  constructor() {
    effect(() => {
      this.user()?.profileImageUrl;
      this.avatarLoadFailed.set(false);
    });

    const refreshes: Observable<unknown>[] = [timer(0, 60_000)];
    const browserWindow = this.document.defaultView;
    if (browserWindow) {
      refreshes.push(fromEvent(browserWindow, 'focus'));
    }

    merge(...refreshes)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.notifications.refreshUnreadCount().subscribe());
  }

  protected loadRecentNotifications(): void {
    this.notificationsLoading.set(true);
    this.notifications.getRecent().subscribe({
      next: (items) => {
        this.recentNotifications.set(items);
        this.notificationsLoading.set(false);
      },
      error: () => this.notificationsLoading.set(false),
    });
  }

  protected openNotification(item: AppNotification): void {
    const navigate = () => void this.router.navigateByUrl(this.safeInternalRoute(item.actionRoute));
    if (item.isRead) {
      navigate();
      return;
    }

    this.notifications.markAsRead(item.id).subscribe({
      next: () => {
        this.notifications.unreadCount.update((count) => Math.max(0, count - 1));
        navigate();
      },
    });
  }

  protected notificationIcon(item: AppNotification): string {
    switch (item.category) {
      case 1: return 'event';
      case 2: return 'fact_check';
      case 3: return 'bloodtype';
      case 4: return 'medical_services';
      case 5: return 'handshake';
      case 6: return 'star';
      case 7: return 'person';
      default: return 'notifications';
    }
  }

  protected markAllNotificationsRead(): void {
    this.notifications.markAllAsRead().subscribe({
      next: () => this.loadRecentNotifications(),
    });
  }

  protected logout(): void {
    this.auth.logout();
  }

  protected onAvatarError(): void {
    this.avatarLoadFailed.set(true);
  }

  private safeInternalRoute(route: string | null): string {
    if (!route || !route.startsWith('/') || route.includes('//') ||
        route.includes('://') || route.toLowerCase().startsWith('javascript:')) {
      return '/notifications';
    }
    return route;
  }
}
