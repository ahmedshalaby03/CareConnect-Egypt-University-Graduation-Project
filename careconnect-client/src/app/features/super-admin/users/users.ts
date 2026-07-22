import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { PUBLIC_ROLES, ROLE_LABELS, User, UserRole } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { UserManagementService } from '../../../core/services/user-management.service';

interface StatusFilter {
  label: string;
  value: boolean | null;
}

@Component({
  selector: 'app-super-admin-users',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './users.html',
  styleUrl: './users.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuperAdminUsers implements OnInit {
  private readonly userService = inject(UserManagementService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayedColumns = ['user', 'role', 'status', 'created', 'actions'];

  protected readonly roleOptions: readonly UserRole[] = PUBLIC_ROLES;
  protected readonly roleLabels = ROLE_LABELS;
  protected readonly statusFilters: StatusFilter[] = [
    { label: 'All statuses', value: null },
    { label: 'Active only', value: true },
    { label: 'Inactive only', value: false },
  ];

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly roleControl = new FormControl<UserRole | ''>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<boolean | null>(null);

  protected readonly users = signal<User[]>([]);
  protected readonly loading = signal(false);
  protected readonly togglingId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  /** The signed-in admin, so the UI can disable the toggle on their own row. */
  protected readonly currentUserId = this.auth.currentUser()?.id ?? null;

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
  }

  protected onRoleChange(): void {
    this.pageIndex.set(0);
    this.load();
  }

  protected onStatusChange(): void {
    this.pageIndex.set(0);
    this.load();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected roleLabel(role: string): string {
    return this.roleLabels[role as UserRole] ?? role;
  }

  protected toggleStatus(user: User): void {
    if (user.id === this.currentUserId || this.togglingId()) {
      return;
    }

    this.togglingId.set(user.id);

    this.userService.toggleStatus(user.id).subscribe({
      next: (response) => {
        this.togglingId.set(null);

        const updated = response.data;
        if (updated) {
          this.users.update((list) =>
            list.map((u) => (u.id === updated.userId ? { ...u, isActive: updated.isActive } : u)),
          );
        }

        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.togglingId.set(null);
        this.notify.error(friendlyMessageOf(error, 'Could not update the user.'));
      },
    });
  }

  private load(): void {
    this.loading.set(true);

    this.userService
      .getUsers({
        search: this.searchControl.value,
        role: this.roleControl.value || null,
        isActive: this.statusControl.value,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.users.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load users.'));
        },
      });
  }
}
