import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  APPOINTMENT_STATUSES,
  AppointmentStatus,
  PatientAppointment,
} from '../../../core/models/appointment.model';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ReasonDialog, ReasonDialogData } from '../../../shared/reason-dialog/reason-dialog';

type ViewMode = 'upcoming' | 'previous';

@Component({
  selector: 'app-patient-appointments',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
  ],
  templateUrl: './patient-appointments.html',
  styleUrl: './patient-appointments.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientAppointments implements OnInit {
  private readonly appointmentService = inject(AppointmentService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly statuses = APPOINTMENT_STATUSES;
  protected readonly view = signal<ViewMode>('upcoming');

  protected readonly statusControl = new FormControl<AppointmentStatus | ''>('', { nonNullable: true });

  protected readonly appointments = signal<PatientAppointment[]>([]);
  protected readonly loading = signal(true);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
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

  protected canCancel(appointment: PatientAppointment): boolean {
    return appointment.statusName === 'Pending' || appointment.statusName === 'Confirmed';
  }

  protected cancel(appointment: PatientAppointment): void {
    const data: ReasonDialogData = {
      title: 'Cancel appointment',
      message: `Tell us why you are cancelling your appointment with ${appointment.doctorName}.`,
      fieldLabel: 'Cancellation reason',
      confirmLabel: 'Cancel appointment',
    };

    this.dialog
      .open<ReasonDialog, ReasonDialogData, string>(ReasonDialog, { data })
      .afterClosed()
      .subscribe((reason) => {
        if (!reason) {
          return;
        }

        this.busyId.set(appointment.appointmentId);

        this.appointmentService
          .cancelByPatient(appointment.appointmentId, { cancellationReason: reason })
          .subscribe({
            next: (response) => {
              this.busyId.set(null);
              this.notify.success(response.message);
              this.load();
            },
            error: (error: unknown) => {
              this.busyId.set(null);
              this.notify.error(friendlyMessageOf(error, 'Could not cancel this appointment.'));
            },
          });
      });
  }

  private load(): void {
    this.loading.set(true);

    const today = new Date().toISOString().slice(0, 10);
    const yesterday = new Date(Date.now() - 86_400_000).toISOString().slice(0, 10);

    this.appointmentService
      .getPatientAppointments({
        status: this.statusControl.value || null,
        dateFrom: this.view() === 'upcoming' ? today : null,
        dateTo: this.view() === 'previous' ? yesterday : null,
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
