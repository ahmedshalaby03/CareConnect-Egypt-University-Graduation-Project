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
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ApiResponse } from '../../../core/models/api-response.model';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  APPOINTMENT_STATUSES,
  AppointmentStatus,
  DoctorAppointment,
} from '../../../core/models/appointment.model';
import { AffiliatedHospital } from '../../../core/models/affiliation.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { ReasonDialog, ReasonDialogData } from '../../../shared/reason-dialog/reason-dialog';

type ViewMode = 'today' | 'upcoming' | 'all';

@Component({
  selector: 'app-doctor-appointments',
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
  ],
  templateUrl: './doctor-appointments.html',
  styleUrl: './doctor-appointments.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorAppointments implements OnInit {
  private readonly appointmentService = inject(AppointmentService);
  private readonly affiliations = inject(AffiliationService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly statuses = APPOINTMENT_STATUSES;
  protected readonly view = signal<ViewMode>('today');

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<AppointmentStatus | ''>('', { nonNullable: true });
  protected readonly hospitalControl = new FormControl<string>('', { nonNullable: true });

  protected readonly hospitals = signal<AffiliatedHospital[]>([]);
  protected readonly appointments = signal<DoctorAppointment[]>([]);
  protected readonly loading = signal(true);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
    this.affiliations.getDoctorHospitals().subscribe({
      next: (hospitals) => this.hospitals.set(hospitals),
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

  protected setView(view: ViewMode): void {
    this.view.set(view);
    this.pageIndex.set(0);
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

  protected confirm(appointment: DoctorAppointment): void {
    const data: ConfirmDialogData = {
      title: 'Confirm this appointment?',
      message: `Confirm the booking with ${appointment.patientName} on ${appointment.appointmentDate} at ${appointment.startTime.slice(0, 5)}?`,
      confirmLabel: 'Confirm',
    };

    this.runConfirmed(appointment, data, () =>
      this.appointmentService.confirmAppointment(appointment.appointmentId));
  }

  protected reject(appointment: DoctorAppointment): void {
    const data: ReasonDialogData = {
      title: 'Reject appointment',
      message: `Tell ${appointment.patientName} why this request is being declined.`,
      fieldLabel: 'Rejection reason',
      confirmLabel: 'Reject',
    };

    this.runWithReason(appointment, data, (reason) =>
      this.appointmentService.rejectAppointment(appointment.appointmentId, { rejectionReason: reason }),
    );
  }

  protected cancel(appointment: DoctorAppointment): void {
    const data: ReasonDialogData = {
      title: 'Cancel appointment',
      message: `Tell ${appointment.patientName} why this appointment is being cancelled.`,
      fieldLabel: 'Cancellation reason',
      confirmLabel: 'Cancel appointment',
    };

    this.runWithReason(appointment, data, (reason) =>
      this.appointmentService.cancelByDoctor(appointment.appointmentId, { cancellationReason: reason }),
    );
  }

  protected complete(appointment: DoctorAppointment): void {
    const data: ConfirmDialogData = {
      title: 'Mark as completed?',
      message: `Mark the visit with ${appointment.patientName} as completed.`,
      confirmLabel: 'Mark completed',
    };

    this.runConfirmed(appointment, data, () =>
      this.appointmentService.completeAppointment(appointment.appointmentId));
  }

  protected markNoShow(appointment: DoctorAppointment): void {
    const data: ConfirmDialogData = {
      title: 'Mark as no-show?',
      message: `Record that ${appointment.patientName} did not show up for this appointment.`,
      confirmLabel: 'Mark no-show',
      destructive: true,
    };

    this.runConfirmed(appointment, data, () =>
      this.appointmentService.markNoShow(appointment.appointmentId));
  }

  private runConfirmed(
    appointment: DoctorAppointment,
    data: ConfirmDialogData,
    action: () => Observable<ApiResponse<DoctorAppointment>>,
  ): void {
    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.execute(appointment.appointmentId, action());
        }
      });
  }

  private runWithReason(
    appointment: DoctorAppointment,
    data: ReasonDialogData,
    action: (reason: string) => Observable<ApiResponse<DoctorAppointment>>,
  ): void {
    this.dialog
      .open<ReasonDialog, ReasonDialogData, string>(ReasonDialog, { data })
      .afterClosed()
      .subscribe((reason) => {
        if (reason) {
          this.execute(appointment.appointmentId, action(reason));
        }
      });
  }

  private execute(appointmentId: string, request$: Observable<ApiResponse<DoctorAppointment>>): void {
    this.busyId.set(appointmentId);

    request$.subscribe({
      next: (response) => {
        this.busyId.set(null);
        this.notify.success(response.message);
        this.load();
      },
      error: (error: unknown) => {
        this.busyId.set(null);
        this.notify.error(friendlyMessageOf(error, 'Could not update this appointment.'));
      },
    });
  }

  private load(): void {
    this.loading.set(true);

    const today = new Date().toISOString().slice(0, 10);

    this.appointmentService
      .getDoctorAppointments({
        status: this.statusControl.value || null,
        date: this.view() === 'today' ? today : null,
        dateFrom: this.view() === 'upcoming' ? today : null,
        hospitalProfileId: this.hospitalControl.value || null,
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
          this.notify.error(friendlyMessageOf(error, 'Could not load your appointments.'));
        },
      });
  }
}
