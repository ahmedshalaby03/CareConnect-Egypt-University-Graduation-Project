import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { friendlyMessageOf, validationErrorsOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceCategoryOption, MedicalServiceOffering } from '../../../core/models/medical-service-provider.model';
import { ServiceDeliveryModeAvailability } from '../../../core/models/medical-service-request.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';

export interface ServiceFormDialogData {
  service: MedicalServiceOffering | null;
  categories: MedicalServiceCategoryOption[];
}

@Component({
  selector: 'app-service-form-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.service ? 'Edit service' : 'Add service' }}</h2>
    <mat-dialog-content>
      <form class="dialog-form" [formGroup]="form">
        <mat-form-field><mat-label>Service name</mat-label><input matInput formControlName="name"/></mat-form-field>
        <mat-form-field><mat-label>Category</mat-label><mat-select formControlName="categoryId">@for (category of data.categories; track category.id) { <mat-option [value]="category.id">{{ category.name }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field><mat-label>Price (EGP)</mat-label><input matInput type="number" min="0" formControlName="price"/></mat-form-field>
        <mat-form-field><mat-label>Duration (minutes)</mat-label><input matInput type="number" min="5" max="1440" formControlName="estimatedDurationMinutes"/></mat-form-field>
        <mat-form-field><mat-label>Delivery options</mat-label><mat-select formControlName="deliveryModeAvailability"><mat-option [value]="1">At provider location only</mat-option><mat-option [value]="2">Home visit only</mat-option><mat-option [value]="3">At provider location and home visit</mat-option></mat-select></mat-form-field>
        <mat-form-field><mat-label>Description</mat-label><textarea matInput rows="3" formControlName="description"></textarea></mat-form-field>
        <mat-form-field><mat-label>Preparation instructions</mat-label><textarea matInput rows="3" formControlName="preparationInstructions"></textarea></mat-form-field>
        <mat-slide-toggle formControlName="isActive">Active and visible after publication</mat-slide-toggle>
        @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }} @for (item of errors(); track item) { <div>{{ item }}</div> }</div> }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end"><button mat-button [mat-dialog-close]="null">Cancel</button><button mat-flat-button (click)="save()" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button></mat-dialog-actions>
  `,
  styles: `.dialog-form{display:grid;min-width:min(560px,75vw);gap:4px;padding-top:8px}.cc-notice{margin-top:10px}@media(max-width:650px){.dialog-form{min-width:0}}`,
})
export class ServiceFormDialog {
  protected readonly data = inject<ServiceFormDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ServiceFormDialog, string | null>);
  private readonly providers = inject(MedicalServiceProviderService);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly errors = signal<string[]>([]);
  protected readonly form = this.fb.group({
    name: [this.data.service?.name ?? '', [Validators.required, Validators.maxLength(150)]],
    categoryId: [this.data.service?.categoryId ?? '', Validators.required],
    price: [this.data.service?.price ?? 0, [Validators.required, Validators.min(0), Validators.max(10_000_000)]],
    estimatedDurationMinutes: [this.data.service?.estimatedDurationMinutes ?? null as number | null, [Validators.min(5), Validators.max(1440)]],
    description: [this.data.service?.description ?? '', Validators.maxLength(2000)],
    preparationInstructions: [this.data.service?.preparationInstructions ?? '', Validators.maxLength(2000)],
    deliveryModeAvailability: [this.data.service?.deliveryModeAvailability ?? 1 as ServiceDeliveryModeAvailability, Validators.required],
    isActive: [this.data.service?.isActive ?? true],
  });

  protected save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true); this.error.set(null); this.errors.set([]);
    const value = this.form.getRawValue();
    const request = { ...value, name: value.name.trim(), description: value.description.trim() || null, preparationInstructions: value.preparationInstructions.trim() || null };
    const operation = this.data.service
      ? this.providers.updateService(this.data.service.id, request)
      : this.providers.createService(request);
    operation.subscribe({
      next: (response) => this.dialogRef.close(response.message),
      error: (error: unknown) => { this.saving.set(false); this.error.set(friendlyMessageOf(error, 'Could not save the service.')); this.errors.set(validationErrorsOf(error)); },
    });
  }
}
