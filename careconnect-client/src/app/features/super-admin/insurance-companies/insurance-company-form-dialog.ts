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
import { InsuranceCompany, InsuranceCompanyRequest } from '../../../core/models/insurance-company.model';
import { InsuranceCompanyService } from '../../../core/services/insurance-company.service';

export interface InsuranceCompanyFormDialogData {
  /** Null for create, the existing row for edit. */
  company: InsuranceCompany | null;
}

/** Create or edit an insurance company. Closes with the API's success message when it saves. */
@Component({
  selector: 'app-insurance-company-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './insurance-company-form-dialog.html',
  styles: `
    mat-dialog-content {
      min-width: min(480px, 84vw);
      padding-top: 8px;
    }
  `,
})
export class InsuranceCompanyFormDialog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly companies = inject(InsuranceCompanyService);

  protected readonly dialogRef = inject(MatDialogRef<InsuranceCompanyFormDialog, string>);
  protected readonly data = inject<InsuranceCompanyFormDialogData>(MAT_DIALOG_DATA);

  protected readonly isEdit = this.data.company !== null;
  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly form = this.fb.group({
    name: [
      this.data.company?.name ?? '',
      [Validators.required, Validators.minLength(2), Validators.maxLength(150)],
    ],
    arabicName: [this.data.company?.arabicName ?? '', [Validators.maxLength(150)]],
    description: [this.data.company?.description ?? '', [Validators.maxLength(1000)]],
    phoneNumber: [this.data.company?.phoneNumber ?? '', [Validators.pattern(/^\+?[0-9][0-9\s-]{6,19}$/)]],
    websiteUrl: [this.data.company?.websiteUrl ?? '', [Validators.maxLength(500)]],
    logoUrl: [this.data.company?.logoUrl ?? '', [Validators.maxLength(500)]],
  });

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: InsuranceCompanyRequest = {
      name: raw.name.trim(),
      arabicName: raw.arabicName.trim() || null,
      description: raw.description.trim() || null,
      phoneNumber: raw.phoneNumber.trim() || null,
      websiteUrl: raw.websiteUrl.trim() || null,
      logoUrl: raw.logoUrl.trim() || null,
    };

    this.saving.set(true);

    const save$ = this.isEdit
      ? this.companies.update(this.data.company!.id, request)
      : this.companies.create(request);

    save$.subscribe({
      next: (response) => {
        this.saving.set(false);
        this.dialogRef.close(response.message);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        // A duplicate name comes back as 409 and lands here with a readable message.
        this.serverError.set(friendlyMessageOf(error, 'Could not save the insurance company.'));
        this.serverErrorList.set(validationErrorsOf(error));
      },
    });
  }
}
