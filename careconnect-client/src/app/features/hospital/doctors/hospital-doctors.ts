import { DatePipe } from '@angular/common';
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
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { HospitalDoctor } from '../../../core/models/affiliation.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-hospital-doctors',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './hospital-doctors.html',
  styleUrl: './hospital-doctors.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalDoctors implements OnInit {
  private readonly affiliations = inject(AffiliationService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly specialtyControl = new FormControl<string>('', { nonNullable: true });

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly doctors = signal<HospitalDoctor[]>([]);
  protected readonly loading = signal(true);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(12);

  ngOnInit(): void {
    this.specialtyService.getActive().subscribe({
      next: (items) => this.specialties.set(items),
      error: () => undefined,
    });

    this.searchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
  }

  protected onFilterChange(): void {
    this.pageIndex.set(0);
    this.load();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  /**
   * Removing ends the working relationship. The record is kept as history rather than
   * deleted, and the doctor can apply again later.
   */
  protected remove(doctor: HospitalDoctor): void {
    const data: ConfirmDialogData = {
      title: 'Remove this doctor?',
      message:
        `${doctor.doctorName} will no longer appear on your hospital's public page. ` +
        'The affiliation record is kept for your records, and they can apply again later.',
      confirmLabel: 'Remove doctor',
      destructive: true,
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.busyId.set(doctor.doctorProfileId);

        this.affiliations.removeDoctor(doctor.doctorProfileId).subscribe({
          next: (response) => {
            this.busyId.set(null);
            this.notify.success(response.message);
            this.load();
          },
          error: (error: unknown) => {
            this.busyId.set(null);
            this.notify.error(friendlyMessageOf(error, 'Could not remove the doctor.'));
          },
        });
      });
  }

  private load(): void {
    this.loading.set(true);

    this.affiliations
      .getHospitalDoctors({
        search: this.searchControl.value,
        specialtyId: this.specialtyControl.value || null,
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
          this.notify.error(friendlyMessageOf(error, 'Could not load your doctors.'));
        },
      });
  }
}
