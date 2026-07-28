import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { friendlyMessageOf, validationErrorsOf } from '../../../core/interceptors/error.interceptor';
import { ProviderWorkingHour } from '../../../core/models/medical-service-provider.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { NotificationService } from '../../../core/services/notification.service';

type HourForm = FormGroup<{
  dayOfWeek: FormControl<string>;
  dayName: FormControl<string>;
  isClosed: FormControl<boolean>;
  openTime: FormControl<string>;
  closeTime: FormControl<string>;
}>;

@Component({
  selector: 'app-service-provider-working-hours',
  imports: [ReactiveFormsModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page"><header><h1>Working hours</h1><p>Configure one clear schedule for every day of the week.</p></header>
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> } @else {
        <form (ngSubmit)="save()">
          <div class="hours">
            @for (row of rows.controls; track row.controls.dayOfWeek.value) {
              <article class="cc-card hour-row" [formGroup]="row">
                <div class="day"><strong>{{ row.controls.dayName.value }}</strong><mat-checkbox formControlName="isClosed" (change)="closedChanged(row)">Closed</mat-checkbox></div>
                <mat-form-field><mat-label>Opens</mat-label><input matInput type="time" formControlName="openTime"/></mat-form-field>
                <mat-form-field><mat-label>Closes</mat-label><input matInput type="time" formControlName="closeTime"/></mat-form-field>
              </article>
            }
          </div>
          @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }} @for (item of errors(); track item) { <div>{{ item }}</div> }</div> }
          <button mat-flat-button type="submit" [disabled]="saving()"><mat-icon>save</mat-icon>{{ saving() ? 'Saving…' : 'Save schedule' }}</button>
        </form>
      }
    </section>
  `,
  styles: `.hours{display:grid;gap:12px;margin:20px 0}.hour-row{display:grid;grid-template-columns:1.2fr 1fr 1fr;gap:14px;align-items:center}.day{display:flex;justify-content:space-between;align-items:center;gap:12px}@media(max-width:700px){.hour-row{grid-template-columns:1fr}.day{margin-bottom:4px}}`,
})
export class ServiceProviderWorkingHoursPage implements OnInit {
  private readonly providers = inject(MedicalServiceProviderService);
  private readonly notify = inject(NotificationService);
  protected readonly rows = new FormArray<HourForm>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly errors = signal<string[]>([]);

  ngOnInit(): void {
    this.providers.getWorkingHours().subscribe({
      next: (items) => { items.forEach((item) => this.rows.push(this.createRow(item))); this.loading.set(false); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load working hours.')); },
    });
  }
  protected closedChanged(row: HourForm): void { this.setTimeState(row); }
  protected save(): void {
    this.error.set(null); this.errors.set([]);
    const invalid = this.rows.controls.some((row) => {
      const value = row.getRawValue();
      return !value.isClosed && (!value.openTime || !value.closeTime || value.openTime >= value.closeTime);
    });
    if (invalid) { this.notify.error('Every open day needs an opening time before its closing time.'); return; }
    this.saving.set(true);
    this.providers.updateWorkingHours({ workingHours: this.rows.controls.map((row) => {
      const value = row.getRawValue();
      return { dayOfWeek: value.dayOfWeek, isClosed: value.isClosed, openTime: value.isClosed ? null : value.openTime, closeTime: value.isClosed ? null : value.closeTime };
    }) }).subscribe({
      next: (response) => { this.saving.set(false); this.notify.success(response.message); },
      error: (error: unknown) => { this.saving.set(false); this.error.set(friendlyMessageOf(error, 'Could not save working hours.')); this.errors.set(validationErrorsOf(error)); },
    });
  }
  private createRow(item: ProviderWorkingHour): HourForm {
    const row = new FormGroup({
      dayOfWeek: new FormControl(item.dayOfWeek, { nonNullable: true }),
      dayName: new FormControl(item.dayName, { nonNullable: true }),
      isClosed: new FormControl(item.isClosed, { nonNullable: true }),
      openTime: new FormControl(item.openTime ?? '09:00', { nonNullable: true, validators: Validators.required }),
      closeTime: new FormControl(item.closeTime ?? '17:00', { nonNullable: true, validators: Validators.required }),
    });
    this.setTimeState(row);
    return row;
  }
  private setTimeState(row: HourForm): void {
    const closed = row.controls.isClosed.value;
    for (const control of [row.controls.openTime, row.controls.closeTime]) {
      closed ? control.disable({ emitEvent: false }) : control.enable({ emitEvent: false });
    }
  }
}
