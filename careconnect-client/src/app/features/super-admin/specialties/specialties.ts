import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
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
import { Specialty } from '../../../core/models/specialty.model';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';
import { SpecialtyFormDialog, SpecialtyFormDialogData } from './specialty-form-dialog';

@Component({
  selector: 'app-super-admin-specialties',
  imports: [
    ReactiveFormsModule,
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
  templateUrl: './specialties.html',
  styleUrl: './specialties.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuperAdminSpecialties implements OnInit {
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayedColumns = ['name', 'arabicName', 'usage', 'status', 'actions'];

  protected readonly statusFilters = [
    { label: 'All statuses', value: null },
    { label: 'Active only', value: true },
    { label: 'Inactive only', value: false },
  ];

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<boolean | null>(null);

  protected readonly specialties = signal<Specialty[]>([]);
  protected readonly loading = signal(false);
  protected readonly togglingId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

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

  protected openForm(specialty: Specialty | null): void {
    const ref = this.dialog.open<SpecialtyFormDialog, SpecialtyFormDialogData, string>(
      SpecialtyFormDialog,
      { data: { specialty }, autoFocus: 'first-tabbable' },
    );

    ref.afterClosed().subscribe((message) => {
      if (message) {
        this.notify.success(message);
        this.load();
      }
    });
  }

  /**
   * There is no delete: deactivating hides the specialty from selection lists while every
   * doctor and hospital already using it keeps its assignment.
   */
  protected toggleStatus(specialty: Specialty): void {
    if (this.togglingId()) {
      return;
    }

    this.togglingId.set(specialty.id);

    this.specialtyService.toggleStatus(specialty.id).subscribe({
      next: (response) => {
        this.togglingId.set(null);

        const updated = response.data;
        if (updated) {
          this.specialties.update((list) =>
            list.map((s) => (s.id === updated.id ? { ...s, isActive: updated.isActive } : s)),
          );
        }

        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.togglingId.set(null);
        this.notify.error(friendlyMessageOf(error, 'Could not update the specialty.'));
      },
    });
  }

  private load(): void {
    this.loading.set(true);

    this.specialtyService
      .getAll({
        search: this.searchControl.value,
        isActive: this.statusControl.value,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.specialties.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load specialties.'));
        },
      });
  }
}
