import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { ApiResponse } from '../../../core/models/api-response.model';
import { HospitalBloodRequest } from '../../../core/models/blood-request.model';
import { BloodRequestService } from '../../../core/services/blood-request.service';

export interface RejectBloodRequestDialogData {
  requestId: string;
}

/** Rejects a blood request. Closes with the API response when it saves. */
@Component({
  selector: 'app-reject-blood-request-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './reject-blood-request-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(440px, 84vw);
      padding-top: 8px;
    }
  `,
})
export class RejectBloodRequestDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly bloodRequests = inject(BloodRequestService);

  protected readonly dialogRef =
    inject(MatDialogRef<RejectBloodRequestDialog, ApiResponse<HospitalBloodRequest>>);
  protected readonly data = inject<RejectBloodRequestDialogData>(MAT_DIALOG_DATA);

  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    rejectionReason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
    hospitalNotes: ['', [Validators.maxLength(1000)]],
  });

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.saving.set(true);

    this.bloodRequests
      .reject(this.data.requestId, {
        rejectionReason: raw.rejectionReason.trim(),
        hospitalNotes: raw.hospitalNotes.trim() || null,
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.dialogRef.close(response);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.serverError.set(friendlyMessageOf(error, 'Could not reject this request.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }
}
