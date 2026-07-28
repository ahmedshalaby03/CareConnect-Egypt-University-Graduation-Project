import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { friendlyMessageOf, validationErrorsOf } from '../../../core/interceptors/error.interceptor';
import { ApiResponse } from '../../../core/models/api-response.model';
import { MedicalServiceRequestDetails } from '../../../core/models/medical-service-request.model';
import { MedicalServiceRequestService } from '../../../core/services/medical-service-request.service';

export interface AcceptServiceRequestDialogData {
  request: MedicalServiceRequestDetails;
}

@Component({
  selector: 'app-accept-service-request-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Accept {{ data.request.requestNumber }}</h2>
    <mat-dialog-content><p>Confirm a date and time within your published working hours.</p><form class="form" [formGroup]="form">
      <mat-form-field><mat-label>Confirmed date</mat-label><input matInput type="date" formControlName="scheduledDate" [min]="minimumDate" [max]="maximumDate"/></mat-form-field>
      <mat-form-field><mat-label>Confirmed start time</mat-label><input matInput type="time" formControlName="scheduledStartTime"/></mat-form-field>
      <mat-form-field><mat-label>Response note (optional)</mat-label><textarea matInput rows="4" formControlName="providerResponseNote"></textarea></mat-form-field>
      @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }} @for (item of errors(); track item) { <div>{{ item }}</div> }</div> }
    </form></mat-dialog-content>
    <mat-dialog-actions align="end"><button mat-button (click)="dialogRef.close()">Close</button><button mat-flat-button (click)="submit()" [disabled]="saving()">{{ saving() ? 'Accepting…' : 'Accept request' }}</button></mat-dialog-actions>
  `,
  styles: `.form{display:grid;min-width:min(500px,75vw);gap:6px;padding-top:8px}@media(max-width:600px){.form{min-width:0}}`,
})
export class AcceptServiceRequestDialog {
  protected readonly data = inject<AcceptServiceRequestDialogData>(MAT_DIALOG_DATA);
  protected readonly dialogRef = inject(MatDialogRef<AcceptServiceRequestDialog, ApiResponse<MedicalServiceRequestDetails>>);
  private readonly requests = inject(MedicalServiceRequestService);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly errors = signal<string[]>([]);
  protected readonly minimumDate = this.isoDate(new Date());
  protected readonly maximumDate = this.isoDate(new Date(Date.now() + 90 * 86400000));
  protected readonly form = this.fb.group({
    scheduledDate: [this.data.request.requestedDate, Validators.required],
    scheduledStartTime: [this.data.request.preferredStartTime.slice(0, 5), Validators.required],
    providerResponseNote: ['', Validators.maxLength(2000)],
  });
  protected submit(): void {
    if (this.form.invalid || this.saving()) { this.form.markAllAsTouched(); return; }
    this.saving.set(true); this.error.set(null); this.errors.set([]);
    const value = this.form.getRawValue();
    this.requests.accept(this.data.request.id, {
      scheduledDate: value.scheduledDate,
      scheduledStartTime: value.scheduledStartTime,
      providerResponseNote: value.providerResponseNote.trim() || null,
    }).subscribe({
      next: (response) => this.dialogRef.close(response),
      error: (error: unknown) => { this.saving.set(false); this.error.set(friendlyMessageOf(error, 'Could not accept this request.')); this.errors.set(validationErrorsOf(error)); },
    });
  }
  private isoDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
