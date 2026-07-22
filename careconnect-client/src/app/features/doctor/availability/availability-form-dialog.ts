import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { AffiliatedHospital } from '../../../core/models/affiliation.model';
import { Availability, AvailabilityRequest, DAYS_OF_WEEK } from '../../../core/models/availability.model';
import { DoctorAvailabilityService } from '../../../core/services/doctor-availability.service';

export interface AvailabilityFormDialogData {
  /** Null for create, the existing row for edit. */
  availability: Availability | null;
  /** Hospitals the doctor is approved at - the only ones a new block can target. */
  hospitals: AffiliatedHospital[];
}

/** Create or edit one weekly schedule block. Closes with the API's success message when it saves. */
@Component({
  selector: 'app-availability-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './availability-form-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(460px, 82vw);
      padding-top: 8px;
    }
  `,
})
export class AvailabilityFormDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly availabilityService = inject(DoctorAvailabilityService);

  protected readonly dialogRef = inject(MatDialogRef<AvailabilityFormDialog, string>);
  protected readonly data = inject<AvailabilityFormDialogData>(MAT_DIALOG_DATA);

  protected readonly isEdit = this.data.availability !== null;
  protected readonly days = DAYS_OF_WEEK;
  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    hospitalProfileId: [this.data.availability?.hospitalProfileId ?? '', [Validators.required]],
    dayOfWeek: [this.data.availability?.dayOfWeek ?? 0, [Validators.required]],
    startTime: [this.data.availability?.startTime ?? '09:00', [Validators.required]],
    endTime: [this.data.availability?.endTime ?? '17:00', [Validators.required]],
    slotDurationMinutes: [
      this.data.availability?.slotDurationMinutes ?? 30,
      [Validators.required, Validators.min(10), Validators.max(180)],
    ],
  });

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: AvailabilityRequest = this.form.getRawValue();
    this.saving.set(true);

    const save$ = this.isEdit
      ? this.availabilityService.update(this.data.availability!.id, request)
      : this.availabilityService.create(request);

    save$.subscribe({
      next: (response) => {
        this.saving.set(false);
        this.dialogRef.close(response.message);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        // A schedule overlap comes back as 409 and lands here with a readable message.
        this.serverError.set(friendlyMessageOf(error, 'Could not save this schedule.'));
        this.serverErrorList.set(validationErrorsOf(error));
      },
    });
  }
}
