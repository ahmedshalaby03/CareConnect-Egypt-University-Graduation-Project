import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ReasonDialogData {
  title: string;
  message: string;
  fieldLabel: string;
  confirmLabel?: string;
  /** Renders the confirm button in the danger colour. Defaults to true. */
  destructive?: boolean;
}

/**
 * Generic "give a reason" dialog - used wherever the API requires a mandatory reason
 * (rejecting or cancelling an appointment). Closes with the trimmed reason string, or
 * undefined if the user backs out.
 */
@Component({
  selector: 'app-reason-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>

    <mat-dialog-content>
      <p class="lead">{{ data.message }}</p>

      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="cc-form-field">
          <mat-label>{{ data.fieldLabel }}</mat-label>
          <textarea matInput formControlName="reason" rows="4"></textarea>
          @if (form.controls.reason.hasError('required') && form.controls.reason.touched) {
            <mat-error>{{ data.fieldLabel }} is required.</mat-error>
          }
          @if (form.controls.reason.hasError('minlength')) {
            <mat-error>Please give at least 5 characters.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button type="button" mat-button (click)="dialogRef.close()">Cancel</button>
      <button
        type="button"
        mat-flat-button
        [color]="data.destructive === false ? 'primary' : 'warn'"
        (click)="submit()"
      >
        {{ data.confirmLabel ?? 'Confirm' }}
      </button>
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
export class ReasonDialog {
  protected readonly dialogRef = inject(MatDialogRef<ReasonDialog, string>);
  protected readonly data = inject<ReasonDialogData>(MAT_DIALOG_DATA);

  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly form = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(this.form.getRawValue().reason.trim());
  }
}
