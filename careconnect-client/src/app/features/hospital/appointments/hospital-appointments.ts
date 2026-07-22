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
import { MatTableModule } from '@angular/material/table';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  APPOINTMENT_STATUSES,
  AppointmentStatus,
  HospitalAppointment,
} from '../../../core/models/appointment.model';
import { HospitalDoctor } from '../../../core/models/affiliation.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-hospital-appointments',
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
    MatProgressBarModule,
  ],
  templateUrl: './hospital-appointments.html',
  styleUrl: './hospital-appointments.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalAppointments implements OnInit {
  private readonly appointmentService = inject(AppointmentService);
  private readonly affiliations = inject(AffiliationService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayedColumns = ['patient', 'doctor', 'when', 'status'];
  protected readonly statuses = APPOINTMENT_STATUSES;

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<AppointmentStatus | ''>('', { nonNullable: true });
  protected readonly doctorControl = new FormControl<string>('', { nonNullable: true });
  protected readonly dateControl = new FormControl<string>('', { nonNullable: true });

  protected readonly doctors = signal<HospitalDoctor[]>([]);
  protected readonly appointments = signal<HospitalAppointment[]>([]);
  protected readonly loading = signal(true);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(15);

  ngOnInit(): void {
    // A generous page size: this only feeds a filter dropdown, not a paginated view.
    this.affiliations.getHospitalDoctors({ page: 1, pageSize: 200 }).subscribe({
      next: (result) => this.doctors.set(result.items),
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

  protected statusClass(status: AppointmentStatus): string {
    switch (status) {
      case 'Confirmed':
      case 'Completed':
        return 'cc-status-chip--active';
      case 'Pending':
        return 'cc-status-chip--pending';
      default:
        return 'cc-status-chip--inactive';
    }
  }

  /**
   * Read-only detail popup. Reuses the generic ConfirmDialog as a message box - both of its
   * buttons simply close it, since a hospital cannot change anything about an appointment.
   */
  protected viewDetails(appointment: HospitalAppointment): void {
    const data: ConfirmDialogData = {
      title: `${appointment.patientName} with ${appointment.doctorName}`,
      message:
        `${appointment.appointmentDate} at ${appointment.startTime.slice(0, 5)}-${appointment.endTime.slice(0, 5)}. ` +
        `Status: ${appointment.statusName}. Reason: ${appointment.reason ?? 'not given'}.`,
      confirmLabel: 'Close',
    };

    this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data });
  }

  private load(): void {
    this.loading.set(true);

    this.appointmentService
      .getHospitalAppointments({
        status: this.statusControl.value || null,
        date: this.dateControl.value || null,
        doctorProfileId: this.doctorControl.value || null,
        patientName: this.searchControl.value,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.appointments.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load appointments.'));
        },
      });
  }
}
