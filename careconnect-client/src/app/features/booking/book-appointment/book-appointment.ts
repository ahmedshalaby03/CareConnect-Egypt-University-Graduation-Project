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

/** Formats a local calendar date without the UTC shift caused by Date#toISOString. */
function localDateIso(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function todayIso(): string {
  return localDateIso(new Date());
}

function addDays(dateIso: string, days: number): string {
  const [year, month, day] = dateIso.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  date.setDate(date.getDate() + days);
  return localDateIso(date);
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
  private static readonly nearestSlotSearchDays = 14;

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
  protected readonly nearestDateSelected = signal(false);

  protected readonly hospitalControl = this.fb.control('', [Validators.required]);
  protected readonly dateControl = this.fb.control(this.minDate, [Validators.required]);
  private slotRequestVersion = 0;

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
          this.findNextAvailableDate();
        } else {
          const primary = doctor.hospitals.find((h) => h.isPrimary);
          if (primary) {
            this.hospitalControl.setValue(primary.id);
            this.findNextAvailableDate();
          }
        }
      },
      error: (error: unknown) => {
        this.loadingDoctor.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this doctor.'));
      },
    });
  }

  protected onHospitalChange(): void {
    this.selectedSlot.set(null);
    this.findNextAvailableDate();
  }

  protected onDateChange(): void {
    this.selectedSlot.set(null);
    this.nearestDateSelected.set(false);
    this.loadSlots();
  }

  protected findNextAvailableDate(): void {
    if (this.hospitalControl.invalid) {
      return;
    }

    const startingDate = this.dateControl.valid ? this.dateControl.value : this.minDate;
    const requestVersion = ++this.slotRequestVersion;

    this.selectedSlot.set(null);
    this.nearestDateSelected.set(false);
    this.loadingSlots.set(true);
    this.slotsLoaded.set(false);
    this.searchNextAvailableDate(startingDate, 0, requestVersion);
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

    const requestVersion = ++this.slotRequestVersion;
    this.loadingSlots.set(true);
    this.slotsLoaded.set(false);

    this.directory
      .getAvailableSlots(this.id(), this.hospitalControl.value, this.dateControl.value)
      .subscribe({
        next: (response) => {
          if (requestVersion !== this.slotRequestVersion) {
            return;
          }

          this.loadingSlots.set(false);
          this.slotsLoaded.set(true);
          this.slots.set(response.slots);
        },
        error: (error: unknown) => {
          if (requestVersion !== this.slotRequestVersion) {
            return;
          }

          this.loadingSlots.set(false);
          this.slotsLoaded.set(true);
          this.slots.set([]);
          this.notify.error(friendlyMessageOf(error, 'Could not load available slots.'));
        },
      });
  }

  private searchNextAvailableDate(
    startingDate: string,
    dayOffset: number,
    requestVersion: number,
  ): void {
    const candidateDate = addDays(startingDate, dayOffset);

    this.directory
      .getAvailableSlots(this.id(), this.hospitalControl.value, candidateDate)
      .subscribe({
        next: (response) => {
          if (requestVersion !== this.slotRequestVersion) {
            return;
          }

          if (response.slots.length > 0) {
            this.dateControl.setValue(candidateDate, { emitEvent: false });
            this.slots.set(response.slots);
            this.nearestDateSelected.set(dayOffset > 0);
            this.loadingSlots.set(false);
            this.slotsLoaded.set(true);
            return;
          }

          if (dayOffset + 1 < BookAppointment.nearestSlotSearchDays) {
            this.searchNextAvailableDate(startingDate, dayOffset + 1, requestVersion);
            return;
          }

          this.slots.set([]);
          this.loadingSlots.set(false);
          this.slotsLoaded.set(true);
        },
        error: (error: unknown) => {
          if (requestVersion !== this.slotRequestVersion) {
            return;
          }

          this.slots.set([]);
          this.loadingSlots.set(false);
          this.slotsLoaded.set(true);
          this.notify.error(friendlyMessageOf(error, 'Could not load available slots.'));
        },
      });
  }
}
