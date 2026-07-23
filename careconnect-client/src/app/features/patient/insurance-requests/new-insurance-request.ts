import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { PatientAppointment } from '../../../core/models/appointment.model';
import { InsuranceCompanyOption } from '../../../core/models/insurance-company.model';
import { AppointmentService } from '../../../core/services/appointment.service';
import { InsuranceCompanyService } from '../../../core/services/insurance-company.service';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';
import { NotificationService } from '../../../core/services/notification.service';

/** Statuses that block a second active request for the same appointment. */
const BLOCKING_STATUSES = new Set(['Pending', 'UnderReview', 'Approved']);

@Component({
  selector: 'app-new-insurance-request',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './new-insurance-request.html',
  styleUrl: './new-insurance-request.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewInsuranceRequest implements OnInit {
  private readonly appointments = inject(AppointmentService);
  private readonly insuranceCompanies = inject(InsuranceCompanyService);
  private readonly insuranceRequests = inject(InsuranceRequestService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly eligibleAppointments = signal<PatientAppointment[]>([]);
  protected readonly companies = signal<InsuranceCompanyOption[]>([]);

  protected readonly form = this.fb.group({
    appointmentId: ['', [Validators.required]],
    insuranceCompanyId: ['', [Validators.required]],
    memberNumber: ['', [Validators.required, Validators.maxLength(100)]],
    policyNumber: ['', [Validators.maxLength(100)]],
    serviceDescription: ['', [Validators.required, Validators.maxLength(1000)]],
    requestedAmount: [null as number | null, [Validators.min(0)]],
    patientNotes: ['', [Validators.maxLength(2000)]],
    insuranceCardImageUrl: ['', [Validators.maxLength(500)]],
    supportingDocumentUrl: ['', [Validators.maxLength(500)]],
  });

  protected readonly selectedAppointment = computed(() => {
    const id = this.form.controls.appointmentId.value;
    return this.eligibleAppointments().find((a) => a.appointmentId === id) ?? null;
  });

  ngOnInit(): void {
    // Eligibility (Pending/Confirmed appointment, no active or approved request against it
    // yet) is computed client-side purely for a friendly picker; the backend re-checks
    // every one of these rules independently when the form is submitted.
    forkJoin({
      appointments: this.appointments.getPatientAppointments({ page: 1, pageSize: 100 }),
      requests: this.insuranceRequests.getPatientRequests({ page: 1, pageSize: 100 }),
      companies: this.insuranceCompanies.getActive(),
    }).subscribe({
      next: ({ appointments, requests, companies }) => {
        this.loading.set(false);

        const blockedAppointmentIds = new Set(
          requests.items
            .filter((r) => BLOCKING_STATUSES.has(r.statusName))
            .map((r) => r.appointmentId),
        );

        this.eligibleAppointments.set(
          appointments.items.filter(
            (a) =>
              (a.statusName === 'Pending' || a.statusName === 'Confirmed') &&
              !blockedAppointmentIds.has(a.appointmentId),
          ),
        );

        this.companies.set(companies);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load your appointments.'));
      },
    });
  }

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.submitting()) {
      return;
    }

    const raw = this.form.getRawValue();
    this.submitting.set(true);

    this.insuranceRequests
      .createRequest({
        appointmentId: raw.appointmentId,
        insuranceCompanyId: raw.insuranceCompanyId,
        memberNumber: raw.memberNumber.trim(),
        policyNumber: raw.policyNumber.trim() || null,
        serviceDescription: raw.serviceDescription.trim(),
        requestedAmount: raw.requestedAmount,
        patientNotes: raw.patientNotes.trim() || null,
        insuranceCardImageUrl: raw.insuranceCardImageUrl.trim() || null,
        supportingDocumentUrl: raw.supportingDocumentUrl.trim() || null,
      })
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.notify.success(response.message);
          void this.router.navigate([
            '/dashboard/patient/insurance-requests',
            response.data!.insuranceRequestId,
          ]);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          // A 409 means an active request already exists for this appointment - the
          // eligibility list is refreshed so the picker no longer offers it.
          this.serverError.set(friendlyMessageOf(error, 'Could not submit the insurance request.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }
}
