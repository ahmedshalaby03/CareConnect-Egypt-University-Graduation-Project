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
import { HospitalInsuranceRequest } from '../../../core/models/insurance-request.model';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';

export interface ApproveInsuranceRequestDialogData {
  requestId: string;
  requestedAmount: number | null;
}

/** Approves an insurance request. Closes with the API response when it saves. */
@Component({
  selector: 'app-approve-insurance-request-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './approve-insurance-request-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(440px, 84vw);
      padding-top: 8px;
    }
  `,
})
export class ApproveInsuranceRequestDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly insuranceRequests = inject(InsuranceRequestService);

  protected readonly dialogRef =
    inject(MatDialogRef<ApproveInsuranceRequestDialog, ApiResponse<HospitalInsuranceRequest>>);
  protected readonly data = inject<ApproveInsuranceRequestDialogData>(MAT_DIALOG_DATA);

  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    approvedAmount: [this.data.requestedAmount, [Validators.min(0)]],
    approvalReferenceNumber: ['', [Validators.maxLength(100)]],
    hospitalNotes: ['', [Validators.maxLength(2000)]],
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

    this.insuranceRequests
      .approve(this.data.requestId, {
        approvedAmount: raw.approvedAmount,
        approvalReferenceNumber: raw.approvalReferenceNumber.trim() || null,
        hospitalNotes: raw.hospitalNotes.trim() || null,
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.dialogRef.close(response);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.serverError.set(friendlyMessageOf(error, 'Could not approve this request.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }
}
