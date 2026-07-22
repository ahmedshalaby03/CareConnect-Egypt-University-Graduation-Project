import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { AppointmentStatus, DoctorAppointment } from '../../../core/models/appointment.model';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { ReasonDialog, ReasonDialogData } from '../../../shared/reason-dialog/reason-dialog';

@Component({
  selector: 'app-doctor-appointment-details',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './doctor-appointment-details.html',
  styleUrl: './doctor-appointment-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorAppointmentDetails implements OnInit {
  private readonly appointments = inject(AppointmentService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly appointment = signal<DoctorAppointment | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly savingNotes = signal(false);

  protected readonly notesForm = this.fb.group({
    doctorNotes: ['', [Validators.maxLength(4000)]],
  });

  ngOnInit(): void {
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

  protected saveNotes(): void {
    const current = this.appointment();
    if (!current || this.notesForm.invalid) {
      this.notesForm.markAllAsTouched();
      return;
    }

    this.savingNotes.set(true);

    this.appointments
      .updateDoctorNotes(current.appointmentId, { doctorNotes: this.notesForm.getRawValue().doctorNotes.trim() || null })
      .subscribe({
        next: (response) => {
          this.savingNotes.set(false);
          this.appointment.set(response.data!);
          this.notify.success(response.message);
        },
        error: (error: unknown) => {
          this.savingNotes.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not save notes.'));
        },
      });
  }

  protected confirm(): void {
    const current = this.appointment();
    if (!current) {
      return;
    }

    this.runConfirmed(
      {
        title: 'Confirm this appointment?',
        message: `Confirm the booking with ${current.patientName}?`,
        confirmLabel: 'Confirm',
      },
      () => this.appointments.confirmAppointment(current.appointmentId),
    );
  }

  protected reject(): void {
    const current = this.appointment();
    if (!current) {
      return;
    }

    this.runWithReason(
      {
        title: 'Reject appointment',
        message: `Tell ${current.patientName} why this request is being declined.`,
        fieldLabel: 'Rejection reason',
        confirmLabel: 'Reject',
      },
      (reason) => this.appointments.rejectAppointment(current.appointmentId, { rejectionReason: reason }),
    );
  }

  protected cancel(): void {
    const current = this.appointment();
    if (!current) {
      return;
    }

    this.runWithReason(
      {
        title: 'Cancel appointment',
        message: `Tell ${current.patientName} why this appointment is being cancelled.`,
        fieldLabel: 'Cancellation reason',
        confirmLabel: 'Cancel appointment',
      },
      (reason) => this.appointments.cancelByDoctor(current.appointmentId, { cancellationReason: reason }),
    );
  }

  protected complete(): void {
    const current = this.appointment();
    if (!current) {
      return;
    }

    this.runConfirmed(
      { title: 'Mark as completed?', message: `Mark the visit with ${current.patientName} as completed.`, confirmLabel: 'Mark completed' },
      () => this.appointments.completeAppointment(current.appointmentId),
    );
  }

  protected markNoShow(): void {
    const current = this.appointment();
    if (!current) {
      return;
    }

    this.runConfirmed(
      {
        title: 'Mark as no-show?',
        message: `Record that ${current.patientName} did not show up.`,
        confirmLabel: 'Mark no-show',
        destructive: true,
      },
      () => this.appointments.markNoShow(current.appointmentId),
    );
  }

  private runConfirmed(data: ConfirmDialogData, action: () => ReturnType<AppointmentService['confirmAppointment']>): void {
    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.execute(action());
        }
      });
  }

  private runWithReason(
    data: ReasonDialogData,
    action: (reason: string) => ReturnType<AppointmentService['rejectAppointment']>,
  ): void {
    this.dialog
      .open<ReasonDialog, ReasonDialogData, string>(ReasonDialog, { data })
      .afterClosed()
      .subscribe((reason) => {
        if (reason) {
          this.execute(action(reason));
        }
      });
  }

  private execute(request$: ReturnType<AppointmentService['confirmAppointment']>): void {
    this.busy.set(true);

    request$.subscribe({
      next: (response) => {
        this.busy.set(false);
        this.appointment.set(response.data!);
        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.notify.error(friendlyMessageOf(error, 'Could not update this appointment.'));
      },
    });
  }

  private load(): void {
    this.loading.set(true);

    this.appointments.getDoctorAppointment(this.id()).subscribe({
      next: (appointment) => {
        this.loading.set(false);
        this.appointment.set(appointment);
        this.notesForm.patchValue({ doctorNotes: appointment.doctorNotes ?? '' });
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this appointment.'));
      },
    });
  }
}
