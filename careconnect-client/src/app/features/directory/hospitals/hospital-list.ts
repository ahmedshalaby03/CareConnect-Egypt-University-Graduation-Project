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
import { EGYPT_GOVERNORATES, HospitalDirectoryItem } from '../../../core/models/directory.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { DirectoryService } from '../../../core/services/directory.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';

/** Public hospital directory. Open to every signed-in role. */
@Component({
  selector: 'app-hospital-list',
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
  templateUrl: './hospital-list.html',
  styleUrl: './hospital-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalList implements OnInit {
  private readonly directory = inject(DirectoryService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly governorates = EGYPT_GOVERNORATES;

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly governorateControl = new FormControl<string>('', { nonNullable: true });
  protected readonly cityControl = new FormControl<string>('', { nonNullable: true });
  protected readonly specialtyControl = new FormControl<string>('', { nonNullable: true });

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly hospitals = signal<HospitalDirectoryItem[]>([]);
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
    this.governorateControl.setValue('', { emitEvent: false });
    this.cityControl.setValue('', { emitEvent: false });
    this.specialtyControl.setValue('', { emitEvent: false });
    this.resetAndLoad();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  private resetAndLoad(): void {
    this.pageIndex.set(0);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.directory
      .searchHospitals({
        search: this.searchControl.value,
        governorate: this.governorateControl.value || null,
        city: this.cityControl.value,
        specialtyId: this.specialtyControl.value || null,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.hospitals.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          const message = friendlyMessageOf(error, 'Could not load hospitals.');
          this.loadError.set(message);
          this.notify.error(message);
        },
      });
  }
}
