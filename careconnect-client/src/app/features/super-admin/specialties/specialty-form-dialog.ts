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
import { Specialty, SpecialtyRequest } from '../../../core/models/specialty.model';
import { SpecialtyService } from '../../../core/services/specialty.service';

export interface SpecialtyFormDialogData {
  /** Null for create, the existing row for edit. */
  specialty: Specialty | null;
}

/** Create or edit a specialty. Closes with the API's success message when it saves. */
@Component({
  selector: 'app-specialty-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './specialty-form-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(460px, 82vw);
      padding-top: 8px;
    }
  `,
})
export class SpecialtyFormDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly specialties = inject(SpecialtyService);

  protected readonly dialogRef = inject(MatDialogRef<SpecialtyFormDialog, string>);
  protected readonly data = inject<SpecialtyFormDialogData>(MAT_DIALOG_DATA);

  protected readonly isEdit = this.data.specialty !== null;
  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    name: [
      this.data.specialty?.name ?? '',
      [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    ],
    arabicName: [this.data.specialty?.arabicName ?? '', [Validators.maxLength(120)]],
    description: [this.data.specialty?.description ?? '', [Validators.maxLength(500)]],
  });

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: SpecialtyRequest = {
      name: raw.name.trim(),
      arabicName: raw.arabicName.trim() || null,
      description: raw.description.trim() || null,
    };

    this.saving.set(true);

    const save$ = this.isEdit
      ? this.specialties.update(this.data.specialty!.id, request)
      : this.specialties.create(request);

    save$.subscribe({
      next: (response) => {
        this.saving.set(false);
        this.dialogRef.close(response.message);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        // A duplicate name comes back as 409 and lands here with a readable message.
        this.serverError.set(friendlyMessageOf(error, 'Could not save the specialty.'));
        this.serverErrorList.set(validationErrorsOf(error));
      },
    });
  }
}
