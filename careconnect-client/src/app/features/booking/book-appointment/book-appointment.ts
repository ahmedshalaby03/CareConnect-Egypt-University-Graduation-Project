import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { DoctorDirectoryDetails } from '../../../core/models/directory.model';
import { Slot } from '../../../core/models/slot.model';
import { DirectoryService } from '../../../core/services/directory.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';

/** Today, as "yyyy-MM-dd", used both as the date input's min and its default value. */
function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

@Component({
  selector: 'app-book-appointment',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './book-appointment.html',
  styleUrl: './book-appointment.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookAppointment implements OnInit {
  private readonly directory = inject(DirectoryService);
  private readonly appointments = inject(AppointmentService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly minDate = todayIso();

  protected readonly doctor = signal<DoctorDirectoryDetails | null>(null);
  protected readonly loadingDoctor = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly slots = signal<Slot[]>([]);
  protected readonly loadingSlots = signal(false);
  protected readonly slotsLoaded = signal(false);
  protected readonly selectedSlot = signal<Slot | null>(null);

  protected readonly submitting = signal(false);

  protected readonly hospitalControl = this.fb.control('', [Validators.required]);
  protected readonly dateControl = this.fb.control(this.minDate, [Validators.required]);

  protected readonly detailsForm = this.fb.group({
    reason: ['', [Validators.required, Validators.maxLength(500)]],
    patientNotes: ['', [Validators.maxLength(2000)]],
  });

  ngOnInit(): void {
    this.directory.getDoctor(this.id()).subscribe({
      next: (doctor) => {
        this.loadingDoctor.set(false);
        this.doctor.set(doctor);

        // A doctor with exactly one hospital can go straight to picking slots.
        if (doctor.hospitals.length === 1) {
          this.hospitalControl.setValue(doctor.hospitals[0].id);
          this.loadSlots();
        } else {
          const primary = doctor.hospitals.find((h) => h.isPrimary);
          if (primary) {
            this.hospitalControl.setValue(primary.id);
            this.loadSlots();
          }
        }
      },
      error: (error: unknown) => {
        this.loadingDoctor.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this doctor.'));
      },
    });
  }

  protected onHospitalOrDateChange(): void {
    this.selectedSlot.set(null);
    this.loadSlots();
  }

  protected selectSlot(slot: Slot): void {
    this.selectedSlot.set(slot);
  }

  protected submit(): void {
    const doctor = this.doctor();
    const slot = this.selectedSlot();

    if (!doctor || !slot || this.hospitalControl.invalid || this.detailsForm.invalid) {
      this.detailsForm.markAllAsTouched();
      if (!slot) {
        this.notify.error('Please select a time slot.');
      }
      return;
    }

    if (this.submitting()) {
      return;
    }

    this.submitting.set(true);
    const raw = this.detailsForm.getRawValue();

    this.appointments
      .bookAppointment({
        doctorProfileId: doctor.doctorProfileId,
        hospitalProfileId: this.hospitalControl.value,
        appointmentDate: this.dateControl.value,
        startTime: slot.startTime,
        reason: raw.reason.trim(),
        patientNotes: raw.patientNotes.trim() || null,
      })
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.notify.success(response.message);
          void this.router.navigateByUrl('/dashboard/patient/appointments');
        },
        error: (error: unknown) => {
          this.submitting.set(false);

          // A 409 means somebody else took this exact slot between the list load and the
          // submit - refresh so the patient sees what is actually still free.
          this.notify.error(friendlyMessageOf(error, 'Could not book this appointment.'));
          this.selectedSlot.set(null);
          this.loadSlots();
        },
      });
  }

  private loadSlots(): void {
    if (this.hospitalControl.invalid || this.dateControl.invalid) {
      return;
    }

    this.loadingSlots.set(true);
    this.slotsLoaded.set(false);

    this.directory
      .getAvailableSlots(this.id(), this.hospitalControl.value, this.dateControl.value)
      .subscribe({
        next: (response) => {
          this.loadingSlots.set(false);
          this.slotsLoaded.set(true);
          this.slots.set(response.slots);
        },
        error: (error: unknown) => {
          this.loadingSlots.set(false);
          this.slotsLoaded.set(true);
          this.slots.set([]);
          this.notify.error(friendlyMessageOf(error, 'Could not load available slots.'));
        },
      });
  }
}
