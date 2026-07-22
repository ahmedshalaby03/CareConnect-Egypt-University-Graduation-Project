import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface RejectDialogData {
  doctorName: string;
}

/**
 * Collects the mandatory rejection reason. The API rejects an empty reason too, so this is
 * a convenience for the user rather than the enforcement point.
 */
@Component({
  selector: 'app-reject-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Reject request</h2>

    <mat-dialog-content>
      <p class="lead">
        Tell {{ data.doctorName }} why their request is being declined. They will see this
        message on their requests page.
      </p>

      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="cc-form-field">
          <mat-label>Rejection reason</mat-label>
          <textarea
            matInput
            formControlName="rejectionReason"
            rows="4"
            placeholder="e.g. We are not recruiting in this specialty at the moment."
          ></textarea>
          @if (form.controls.rejectionReason.hasError('required') && form.controls.rejectionReason.touched) {
            <mat-error>A rejection reason is required.</mat-error>
          }
          @if (form.controls.rejectionReason.hasError('minlength')) {
            <mat-error>Please give at least 5 characters.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button type="button" mat-button (click)="dialogRef.close()">Cancel</button>
      <button type="button" mat-flat-button color="warn" (click)="submit()">Reject request</button>
    </mat-dialog-actions>
  `,
  styles: `
    .lead {
      margin: 0 0 16px;
      color: var(--mat-sys-on-surface-variant);
      line-height: 1.55;
    }

    mat-dialog-content {
      min-width: min(420px, 80vw);
    }
  `,
})
export class RejectDialog {
  protected readonly dialogRef = inject(MatDialogRef<RejectDialog, string>);
  protected readonly data = inject<RejectDialogData>(MAT_DIALOG_DATA);

  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly form = this.fb.group({
    rejectionReason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(this.form.getRawValue().rejectionReason.trim());
  }
}
