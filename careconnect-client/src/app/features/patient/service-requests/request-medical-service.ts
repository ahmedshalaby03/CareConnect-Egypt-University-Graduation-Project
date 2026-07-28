import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { friendlyMessageOf, validationErrorsOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceOffering, MedicalServiceProviderDetails } from '../../../core/models/medical-service-provider.model';
import { DELIVERY_MODE_LABELS, ServiceDeliveryMode } from '../../../core/models/medical-service-request.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { MedicalServiceRequestService } from '../../../core/services/medical-service-request.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-request-medical-service',
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <a mat-button [routerLink]="['/medical-service-providers', providerId()]"><mat-icon>arrow_back</mat-icon>Back to provider</a>
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> }
      @else if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @else if (provider(); as provider) {
        @if (selectedService(); as service) {
          <header><span class="eyebrow">Medical service request</span><h1>{{ service.name }}</h1><p>{{ provider.businessName }} · {{ service.categoryName }}</p></header>
          <div class="layout">
            <form class="cc-card form" [formGroup]="form" (ngSubmit)="submit()">
              <div class="service-summary"><strong>{{ service.price | currency:'EGP ':'symbol':'1.0-2' }}</strong><span>Price captured when you submit</span>@if (service.estimatedDurationMinutes) { <span>{{ service.estimatedDurationMinutes }} minutes</span> }</div>
              <mat-form-field><mat-label>Preferred date</mat-label><input matInput type="date" formControlName="requestedDate" [min]="minimumDate" [max]="maximumDate"/></mat-form-field>
              <mat-form-field><mat-label>Preferred start time</mat-label><input matInput type="time" formControlName="preferredStartTime"/></mat-form-field>
              <mat-form-field><mat-label>Delivery mode</mat-label><mat-select formControlName="deliveryMode">@for (mode of allowedModes(); track mode) { <mat-option [value]="mode">{{ deliveryLabels[mode] }}</mat-option> }</mat-select></mat-form-field>
              @if (form.controls.deliveryMode.value === 2) {
                <mat-form-field><mat-label>Home-visit address</mat-label><textarea matInput rows="3" formControlName="homeVisitAddress"></textarea><mat-hint>Required for a home visit</mat-hint></mat-form-field>
              }
              <mat-form-field><mat-label>Patient notes (optional)</mat-label><textarea matInput rows="4" formControlName="patientNotes"></textarea></mat-form-field>
              <div class="cc-notice">This request is not a payment or medical diagnosis. The provider must accept and confirm the schedule.</div>
              @if (serverError()) { <div class="cc-notice cc-notice--error">{{ serverError() }} @for (item of serverErrors(); track item) { <div>{{ item }}</div> }</div> }
              <button mat-flat-button type="submit" [disabled]="submitting()">{{ submitting() ? 'Submitting…' : 'Submit request' }}</button>
            </form>
            <aside class="cc-card"><h2>Working hours</h2>@for (hour of provider.workingHours; track hour.dayOfWeek) { <div class="hour"><span>{{ hour.dayName }}</span><strong>{{ hour.isClosed ? 'Closed' : hour.openTime + ' – ' + hour.closeTime }}</strong></div> }</aside>
          </div>
        } @else { <div class="cc-notice cc-notice--error">This service is not active or no longer belongs to this provider.</div> }
      }
    </section>
  `,
  styles: `.eyebrow{color:var(--cc-primary);font-weight:700}.layout{display:grid;grid-template-columns:minmax(0,2fr) minmax(260px,1fr);gap:22px}.form{display:grid;gap:8px}.service-summary{display:flex;gap:16px;align-items:center;flex-wrap:wrap;padding-bottom:12px}.service-summary strong{font-size:1.4rem;color:var(--cc-primary)}.hour{display:flex;justify-content:space-between;gap:12px;padding:10px 0;border-bottom:1px solid var(--mat-sys-outline-variant)}@media(max-width:800px){.layout{grid-template-columns:1fr}}`,
})
export class RequestMedicalService implements OnInit {
  readonly providerId = input.required<string>();
  readonly serviceId = input.required<string>();
  private readonly providers = inject(MedicalServiceProviderService);
  private readonly requests = inject(MedicalServiceRequestService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly provider = signal<MedicalServiceProviderDetails | null>(null);
  protected readonly selectedService = computed<MedicalServiceOffering | null>(
    () => this.provider()?.services.find((service) => service.id === this.serviceId() && service.isActive) ?? null,
  );
  protected readonly allowedModes = computed<ServiceDeliveryMode[]>(() => {
    const availability = this.selectedService()?.deliveryModeAvailability;
    if (availability === 2) return [2];
    if (availability === 3) return [1, 2];
    return [1];
  });
  protected readonly deliveryLabels = DELIVERY_MODE_LABELS;
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrors = signal<string[]>([]);
  protected readonly minimumDate = this.isoDate(new Date());
  protected readonly maximumDate = this.isoDate(new Date(Date.now() + 90 * 86400000));

  protected readonly form = this.fb.group({
    requestedDate: ['', Validators.required],
    preferredStartTime: ['', Validators.required],
    deliveryMode: [1 as ServiceDeliveryMode, Validators.required],
    homeVisitAddress: ['', Validators.maxLength(500)],
    patientNotes: ['', Validators.maxLength(2000)],
  });

  ngOnInit(): void {
    this.providers.getDetails(this.providerId()).subscribe({
      next: (provider) => {
        this.provider.set(provider);
        this.loading.set(false);
        const firstMode = this.allowedModes()[0];
        if (firstMode) this.form.controls.deliveryMode.setValue(firstMode);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(friendlyMessageOf(error, 'Could not load this medical service.'));
      },
    });
  }

  protected submit(): void {
    const service = this.selectedService();
    const value = this.form.getRawValue();
    this.serverError.set(null);
    this.serverErrors.set([]);
    if (!service || this.form.invalid || (value.deliveryMode === 2 && !value.homeVisitAddress.trim())) {
      this.form.markAllAsTouched();
      if (value.deliveryMode === 2 && !value.homeVisitAddress.trim()) this.serverError.set('A home-visit address is required.');
      return;
    }
    if (this.submitting()) return;
    this.submitting.set(true);
    this.requests.create({
      medicalServiceOfferingId: service.id,
      requestedDate: value.requestedDate,
      preferredStartTime: value.preferredStartTime,
      deliveryMode: value.deliveryMode,
      homeVisitAddress: value.deliveryMode === 2 ? value.homeVisitAddress.trim() : null,
      patientNotes: value.patientNotes.trim() || null,
    }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.notify.success(response.message);
        void this.router.navigate(['/dashboard/patient/service-requests', response.data!.id]);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.serverError.set(friendlyMessageOf(error, 'Could not submit the service request.'));
        this.serverErrors.set(validationErrorsOf(error));
      },
    });
  }

  private isoDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
