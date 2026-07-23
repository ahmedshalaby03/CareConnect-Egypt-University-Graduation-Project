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
import { BloodGroup } from '../../../core/models/blood-group.model';
import { BloodStock } from '../../../core/models/blood-stock.model';
import { BloodStockService } from '../../../core/services/blood-stock.service';

export interface BloodStockFormDialogData {
  bloodGroup: BloodGroup;
  bloodGroupDisplayName: string;
  /** Null when creating the missing record, the existing row when editing. */
  stock: BloodStock | null;
}

/** Creates a missing BloodStock record, or edits an existing one's units/minimum/notes. */
@Component({
  selector: 'app-blood-stock-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './blood-stock-form-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(420px, 84vw);
      padding-top: 8px;
    }
  `,
})
export class BloodStockFormDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly bloodStock = inject(BloodStockService);

  protected readonly dialogRef = inject(MatDialogRef<BloodStockFormDialog, BloodStock>);
  protected readonly data = inject<BloodStockFormDialogData>(MAT_DIALOG_DATA);

  protected readonly isEdit = this.data.stock !== null;
  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    availableUnits: [this.data.stock?.availableUnits ?? 0, [Validators.required, Validators.min(0)]],
    minimumRequiredUnits: [
      this.data.stock?.minimumRequiredUnits ?? 0,
      [Validators.required, Validators.min(0)],
    ],
    notes: [this.data.stock?.notes ?? '', [Validators.maxLength(1000)]],
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

    const save$ = this.isEdit
      ? this.bloodStock.update(this.data.stock!.id, {
          availableUnits: raw.availableUnits,
          minimumRequiredUnits: raw.minimumRequiredUnits,
          notes: raw.notes.trim() || null,
        })
      : this.bloodStock.create({
          bloodGroup: this.data.bloodGroup,
          availableUnits: raw.availableUnits,
          minimumRequiredUnits: raw.minimumRequiredUnits,
          notes: raw.notes.trim() || null,
        });

    save$.subscribe({
      next: (response) => {
        this.saving.set(false);
        this.dialogRef.close(response.data!);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.serverError.set(friendlyMessageOf(error, 'Could not save blood stock.'));
        this.serverErrorList.set(validationErrorsOf(error));
      },
    });
  }
}
