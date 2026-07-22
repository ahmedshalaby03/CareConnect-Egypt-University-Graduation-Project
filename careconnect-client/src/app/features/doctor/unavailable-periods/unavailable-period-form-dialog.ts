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
import { CreateUnavailablePeriodRequest } from '../../../core/models/unavailable-period.model';
import { DoctorUnavailablePeriodService } from '../../../core/services/doctor-unavailable-period.service';

export interface UnavailablePeriodFormDialogData {
  hospitals: AffiliatedHospital[];
}

/** Add one absence or vacation span. Closes with the API's success message when it saves. */
@Component({
  selector: 'app-unavailable-period-form-dialog',
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
  templateUrl: './unavailable-period-form-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(460px, 82vw);
      padding-top: 8px;
    }
  `,
})
export class UnavailablePeriodFormDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly periods = inject(DoctorUnavailablePeriodService);

  protected readonly dialogRef = inject(MatDialogRef<UnavailablePeriodFormDialog, string>);
  protected readonly data = inject<UnavailablePeriodFormDialogData>(MAT_DIALOG_DATA);

  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected readonly form = this.fb.group({
    hospitalProfileId: ['', [Validators.required]],
    startDate: ['', [Validators.required]],
    startTime: ['00:00', [Validators.required]],
    endDate: ['', [Validators.required]],
    endTime: ['23:59', [Validators.required]],
    reason: ['', [Validators.maxLength(500)]],
  });

  protected submit(): void {
    this.serverError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: CreateUnavailablePeriodRequest = {
      hospitalProfileId: raw.hospitalProfileId,
      startDateTime: new Date(`${raw.startDate}T${raw.startTime}:00`).toISOString(),
      endDateTime: new Date(`${raw.endDate}T${raw.endTime}:00`).toISOString(),
      reason: raw.reason.trim() || null,
    };

    this.saving.set(true);

    this.periods.create(request).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.dialogRef.close(response.message);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        // A conflicting appointment comes back as 409 with the details in the message.
        const errors = validationErrorsOf(error);
        this.serverError.set(
          [friendlyMessageOf(error, 'Could not save this period.'), ...errors].join(' '),
        );
      },
    });
  }
}
