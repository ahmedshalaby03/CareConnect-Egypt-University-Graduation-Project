import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { friendlyMessageOf, validationErrorsOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceCategory } from '../../../core/models/medical-service-provider.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';

@Component({
  selector: 'app-medical-service-category-form-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit category' : 'Add category' }}</h2>
    <mat-dialog-content><form class="dialog-form" [formGroup]="form"><mat-form-field><mat-label>Name</mat-label><input matInput formControlName="name"/></mat-form-field><mat-form-field><mat-label>Description</mat-label><textarea matInput rows="4" formControlName="description"></textarea></mat-form-field>@if (error()) { <div class="cc-notice cc-notice--error">{{ error() }} @for (item of errors(); track item) { <div>{{ item }}</div> }</div> }</form></mat-dialog-content>
    <mat-dialog-actions align="end"><button mat-button [mat-dialog-close]="null">Cancel</button><button mat-flat-button (click)="save()" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button></mat-dialog-actions>
  `,
  styles: `.dialog-form{display:grid;min-width:min(480px,75vw);gap:6px;padding-top:8px}@media(max-width:600px){.dialog-form{min-width:0}}`,
})
export class MedicalServiceCategoryFormDialog {
  protected readonly data = inject<MedicalServiceCategory | null>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<MedicalServiceCategoryFormDialog, string | null>);
  private readonly service = inject(MedicalServiceProviderService);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly errors = signal<string[]>([]);
  protected readonly form = this.fb.group({ name: [this.data?.name ?? '', [Validators.required, Validators.maxLength(120)]], description: [this.data?.description ?? '', Validators.maxLength(1000)] });
  protected save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true); const value = this.form.getRawValue(); const request = { name: value.name.trim(), description: value.description.trim() || null };
    const operation = this.data ? this.service.updateCategory(this.data.id, request) : this.service.createCategory(request);
    operation.subscribe({ next: (response) => this.ref.close(response.message), error: (error: unknown) => { this.saving.set(false); this.error.set(friendlyMessageOf(error, 'Could not save the category.')); this.errors.set(validationErrorsOf(error)); } });
  }
}
