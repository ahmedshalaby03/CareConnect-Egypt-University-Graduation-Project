import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { AppointmentStatus, PatientAppointment } from '../../../core/models/appointment.model';
import { AppointmentService } from '../../../core/services/appointment.service';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ReasonDialog, ReasonDialogData } from '../../../shared/reason-dialog/reason-dialog';

/** Statuses that block a second active insurance request for the same appointment. */
const BLOCKING_INSURANCE_STATUSES = new Set(['Pending', 'UnderReview', 'Approved']);

@Component({
  selector: 'app-patient-appointment-details',
  imports: [RouterLink, DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './patient-appointment-details.html',
  styleUrl: './patient-appointment-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientAppointmentDetails implements OnInit {
  private readonly appointments = inject(AppointmentService);
  private readonly insuranceRequests = inject(InsuranceRequestService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly appointment = signal<PatientAppointment | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);

  /**
   * Client-side hint only, used to show/hide the "Apply for insurance" button - the create
   * endpoint independently re-checks eligibility server-side.
   */
  protected readonly hasActiveInsuranceRequest = signal(false);

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

  protected canCancel(appointment: PatientAppointment): boolean {
    return appointment.statusName === 'Pending' || appointment.statusName === 'Confirmed';
  }

  protected canApplyForInsurance(appointment: PatientAppointment): boolean {
    return (
      (appointment.statusName === 'Pending' || appointment.statusName === 'Confirmed') &&
      !this.hasActiveInsuranceRequest()
    );
  }

  protected cancel(): void {
    const current = this.appointment();
    if (!current) {
      return;
    }

    const data: ReasonDialogData = {
      title: 'Cancel appointment',
      message: `Tell us why you are cancelling your appointment with ${current.doctorName}.`,
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

        this.busy.set(true);

        this.appointments.cancelByPatient(current.appointmentId, { cancellationReason: reason }).subscribe({
          next: (response) => {
            this.busy.set(false);
            this.appointment.set(response.data!);
            this.notify.success(response.message);
          },
          error: (error: unknown) => {
            this.busy.set(false);
            this.notify.error(friendlyMessageOf(error, 'Could not cancel this appointment.'));
          },
        });
      });
  }

  private load(): void {
    this.loading.set(true);

    this.appointments.getPatientAppointment(this.id()).subscribe({
      next: (appointment) => {
        this.loading.set(false);
        this.appointment.set(appointment);
        this.loadInsuranceEligibility();
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this appointment.'));
      },
    });
  }

  private loadInsuranceEligibility(): void {
    // A generous page size: this only checks for an existing request against this one
    // appointment, not a paginated view.
    this.insuranceRequests.getPatientRequests({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        const blocked = result.items.some(
          (r) => r.appointmentId === this.id() && BLOCKING_INSURANCE_STATUSES.has(r.statusName),
        );
        this.hasActiveInsuranceRequest.set(blocked);
      },
      error: () => undefined,
    });
  }
}
