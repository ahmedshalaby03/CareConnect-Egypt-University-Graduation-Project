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
import { BloodStock } from '../../../core/models/blood-stock.model';
import { BloodStockService } from '../../../core/services/blood-stock.service';

export interface AdjustBloodStockDialogData {
  stock: BloodStock;
  direction: 'increase' | 'decrease';
}

/**
 * Increases or decreases a BloodStock record's AvailableUnits. Doubles as the required
 * confirmation step before a decrease - the warning text and destructive styling make the
 * consequence explicit before the hospital submits.
 */
@Component({
  selector: 'app-adjust-blood-stock-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './adjust-blood-stock-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(420px, 84vw);
      padding-top: 8px;
    }
  `,
})
export class AdjustBloodStockDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly bloodStock = inject(BloodStockService);

  protected readonly dialogRef = inject(MatDialogRef<AdjustBloodStockDialog, BloodStock>);
  protected readonly data = inject<AdjustBloodStockDialogData>(MAT_DIALOG_DATA);

  protected readonly isDecrease = this.data.direction === 'decrease';
  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    units: [
      1,
      this.isDecrease
        ? [Validators.required, Validators.min(1), Validators.max(this.data.stock.availableUnits || 1)]
        : [Validators.required, Validators.min(1)],
    ],
    notes: ['', [Validators.maxLength(1000)]],
  });

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request = { units: raw.units, notes: raw.notes.trim() || null };
    this.saving.set(true);

    const save$ = this.isDecrease
      ? this.bloodStock.decrease(this.data.stock.id, request)
      : this.bloodStock.increase(this.data.stock.id, request);

    save$.subscribe({
      next: (response) => {
        this.saving.set(false);
        this.dialogRef.close(response.data!);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.serverError.set(friendlyMessageOf(error, 'Could not update blood stock.'));
        this.serverErrorList.set(validationErrorsOf(error));
      },
    });
  }
}
