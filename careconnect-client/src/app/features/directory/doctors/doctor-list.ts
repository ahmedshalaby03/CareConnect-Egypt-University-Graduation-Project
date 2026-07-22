import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { DoctorDirectoryItem, EGYPT_GOVERNORATES } from '../../../core/models/directory.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { DirectoryService } from '../../../core/services/directory.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';

/** Public doctor directory. Open to every signed-in role. */
@Component({
  selector: 'app-doctor-list',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
  ],
  templateUrl: './doctor-list.html',
  styleUrl: './doctor-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorList implements OnInit {
  private readonly directory = inject(DirectoryService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly governorates = EGYPT_GOVERNORATES;

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly specialtyControl = new FormControl<string>('', { nonNullable: true });
  protected readonly governorateControl = new FormControl<string>('', { nonNullable: true });
  protected readonly cityControl = new FormControl<string>('', { nonNullable: true });

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly doctors = signal<DoctorDirectoryItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(9);

  ngOnInit(): void {
    this.specialtyService.getActive().subscribe({
      next: (items) => this.specialties.set(items),
      error: () => undefined,
    });

    for (const control of [this.searchControl, this.cityControl]) {
      control.valueChanges
        .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.resetAndLoad());
    }

    this.load();
  }

  protected onFilterChange(): void {
    this.resetAndLoad();
  }

  protected clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.specialtyControl.setValue('', { emitEvent: false });
    this.governorateControl.setValue('', { emitEvent: false });
    this.cityControl.setValue('', { emitEvent: false });
    this.resetAndLoad();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected initials(fullName: string): string {
    const parts = fullName.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) {
      return '?';
    }

    const letters =
      parts.length > 1 ? `${parts[0][0]}${parts[parts.length - 1][0]}` : parts[0][0];

    return letters.toUpperCase();
  }

  private resetAndLoad(): void {
    this.pageIndex.set(0);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.directory
      .searchDoctors({
        search: this.searchControl.value,
        specialtyId: this.specialtyControl.value || null,
        governorate: this.governorateControl.value || null,
        city: this.cityControl.value,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.doctors.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          const message = friendlyMessageOf(error, 'Could not load doctors.');
          this.loadError.set(message);
          this.notify.error(message);
        },
      });
  }
}
