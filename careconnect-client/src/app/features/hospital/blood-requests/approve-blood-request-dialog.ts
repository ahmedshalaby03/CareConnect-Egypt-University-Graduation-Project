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

export interface ApproveBloodRequestDialogData {
  requestId: string;
  bloodGroupDisplayName: string;
  unitsRequested: number;
  currentAvailableUnits: number;
}

/**
 * Approves a blood request. Shows the allocation math up front - the backend re-reads and
 * decrements stock itself, this is just making the consequence visible before confirming.
 */
@Component({
  selector: 'app-approve-blood-request-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './approve-blood-request-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(440px, 84vw);
      padding-top: 8px;
    }

    .allocation {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px 20px;
      margin: 0 0 16px;
      padding: 12px 14px;
      border-radius: 10px;
      background: var(--mat-sys-surface-container-low, #f4f7f7);
    }

    .allocation dt {
      font-size: 0.7rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--mat-sys-on-surface-variant);
    }

    .allocation dd {
      margin: 3px 0 0;
      font-weight: 600;
    }

    .allocation__negative {
      color: var(--cc-danger);
    }
  `,
})
export class ApproveBloodRequestDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly bloodRequests = inject(BloodRequestService);

  protected readonly dialogRef =
    inject(MatDialogRef<ApproveBloodRequestDialog, ApiResponse<HospitalBloodRequest>>);
  protected readonly data = inject<ApproveBloodRequestDialogData>(MAT_DIALOG_DATA);

  protected readonly remainingUnits = this.data.currentAvailableUnits - this.data.unitsRequested;
  protected readonly insufficientStock = this.remainingUnits < 0;

  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    hospitalNotes: ['', [Validators.maxLength(1000)]],
  });

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    this.bloodRequests
      .approve(this.data.requestId, { hospitalNotes: this.form.getRawValue().hospitalNotes.trim() || null })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.dialogRef.close(response);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          // A 409 means stock changed since this dialog opened - the message names the
          // current available count.
          this.serverError.set(friendlyMessageOf(error, 'Could not approve this request.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }
}
