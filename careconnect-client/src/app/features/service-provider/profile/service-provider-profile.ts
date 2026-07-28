import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { friendlyMessageOf, validationErrorsOf } from '../../../core/interceptors/error.interceptor';
import { EGYPT_GOVERNORATES } from '../../../core/models/directory.model';
import {
  MEDICAL_SERVICE_PROVIDER_TYPES,
  MedicalServiceProviderProfile,
  PROVIDER_TYPE_LABELS,
} from '../../../core/models/medical-service-provider.model';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-service-provider-profile',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <header class="page-head"><div><h1>Business profile</h1><p>Complete and publish the information shown in the medical services directory.</p></div>
        @if (profile(); as item) { <span class="cc-status-chip" [class.cc-status-chip--active]="item.isPublished" [class.cc-status-chip--pending]="!item.isPublished">{{ item.isPublished ? 'Published' : 'Draft' }}</span> }
      </header>
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> }
      @else {
        <form class="cc-card form-grid" [formGroup]="form" (ngSubmit)="save()">
          <mat-form-field><mat-label>Business name</mat-label><input matInput formControlName="businessName"/><mat-error>Business name is required.</mat-error></mat-form-field>
          <mat-form-field><mat-label>Provider type</mat-label><mat-select formControlName="providerType">@for (type of providerTypes; track type) { <mat-option [value]="type">{{ providerTypeLabels[type] }}</mat-option> }</mat-select></mat-form-field>
          <mat-form-field class="wide"><mat-label>Description</mat-label><textarea matInput rows="4" formControlName="description"></textarea></mat-form-field>
          <mat-form-field><mat-label>Phone number</mat-label><input matInput formControlName="phoneNumber"/></mat-form-field>
          <mat-form-field><mat-label>Governorate</mat-label><mat-select formControlName="governorate">@for (item of governorates; track item) { <mat-option [value]="item">{{ item }}</mat-option> }</mat-select></mat-form-field>
          <mat-form-field><mat-label>City</mat-label><input matInput formControlName="city"/></mat-form-field>
          <mat-form-field class="wide"><mat-label>Address</mat-label><input matInput formControlName="address"/></mat-form-field>
          <mat-form-field><mat-label>Latitude</mat-label><input matInput type="number" formControlName="latitude"/></mat-form-field>
          <mat-form-field><mat-label>Longitude</mat-label><input matInput type="number" formControlName="longitude"/></mat-form-field>
          <div class="wide actions">
            <button type="button" mat-stroked-button (click)="useCurrentLocation()" [disabled]="locating()"><mat-icon>my_location</mat-icon>{{ locating() ? 'Locating…' : 'Use my current location' }}</button>
            <button mat-flat-button type="submit" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save profile' }}</button>
          </div>
        </form>
        @if (serverError()) { <div class="cc-notice cc-notice--error">{{ serverError() }} @for (item of serverErrors(); track item) { <div>{{ item }}</div> }</div> }
        @if (profile(); as item) {
          <article class="cc-card publish-card"><div><h2>Public directory</h2><p>{{ item.isReadyToPublish ? 'This profile meets the publication requirements.' : 'Complete the missing requirements before publishing.' }}</p>
            @if (!item.isReadyToPublish) { <ul>@for (requirement of item.missingRequirements; track requirement) { <li>{{ requirement }}</li> }</ul> }
          </div><button mat-flat-button (click)="togglePublication()" [disabled]="publishing() || (!item.isPublished && !item.isReadyToPublish)">{{ item.isPublished ? 'Unpublish' : 'Publish profile' }}</button></article>
        }
      }
    </section>
  `,
  styles: `.page-head,.publish-card,.actions{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;margin:20px 0}.wide{grid-column:1/-1}.actions{align-items:center}.publish-card{margin-top:20px}.publish-card h2{margin-top:0}@media(max-width:700px){.form-grid{grid-template-columns:1fr}}`,
})
export class ServiceProviderProfilePage implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly providers = inject(MedicalServiceProviderService);
  private readonly geolocation = inject(GeolocationService);
  private readonly notify = inject(NotificationService);
  protected readonly providerTypes = MEDICAL_SERVICE_PROVIDER_TYPES;
  protected readonly providerTypeLabels = PROVIDER_TYPE_LABELS;
  protected readonly governorates = EGYPT_GOVERNORATES;
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly publishing = signal(false);
  protected readonly locating = signal(false);
  protected readonly profile = signal<MedicalServiceProviderProfile | null>(null);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrors = signal<string[]>([]);
  protected readonly form = this.fb.group({
    businessName: ['', [Validators.required, Validators.maxLength(150)]],
    providerType: [null as typeof MEDICAL_SERVICE_PROVIDER_TYPES[number] | null, Validators.required],
    description: ['', Validators.maxLength(2000)],
    phoneNumber: ['', Validators.maxLength(30)],
    address: ['', Validators.maxLength(300)],
    governorate: [''],
    city: ['', Validators.maxLength(100)],
    latitude: [null as number | null, [Validators.min(-90), Validators.max(90)]],
    longitude: [null as number | null, [Validators.min(-180), Validators.max(180)]],
  });

  ngOnInit(): void { this.load(); }
  protected useCurrentLocation(): void {
    this.locating.set(true);
    this.geolocation.getCurrentPosition().then((coords) => {
      this.form.patchValue(coords); this.locating.set(false);
      this.notify.success('Coordinates filled in. Review them before saving.');
    }).catch((error: unknown) => {
      this.locating.set(false);
      this.notify.error(error instanceof GeolocationFailure ? error.message : 'Could not determine your location.');
    });
  }
  protected save(): void {
    this.serverError.set(null); this.serverErrors.set([]);
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    if ((value.latitude === null) !== (value.longitude === null)) { this.notify.error('Latitude and longitude must be provided together.'); return; }
    this.saving.set(true);
    this.providers.updateProfile({
      ...value,
      description: value.description.trim() || null,
      phoneNumber: value.phoneNumber.trim() || null,
      address: value.address.trim() || null,
      governorate: value.governorate.trim() || null,
      city: value.city.trim() || null,
      businessName: value.businessName.trim(),
    }).subscribe({
      next: (response) => { this.saving.set(false); this.apply(response.data!); this.notify.success(response.message); },
      error: (error: unknown) => { this.saving.set(false); this.serverError.set(friendlyMessageOf(error, 'Could not save the profile.')); this.serverErrors.set(validationErrorsOf(error)); },
    });
  }
  protected togglePublication(): void {
    const item = this.profile(); if (!item) return;
    this.publishing.set(true);
    this.providers.setPublication(!item.isPublished).subscribe({
      next: (response) => { this.publishing.set(false); this.apply(response.data!); this.notify.success(response.message); },
      error: (error: unknown) => { this.publishing.set(false); this.notify.error(friendlyMessageOf(error, 'Could not change publication status.')); },
    });
  }
  private load(): void { this.providers.getProfile().subscribe({ next: (item) => { this.loading.set(false); this.apply(item); }, error: (error: unknown) => { this.loading.set(false); this.serverError.set(friendlyMessageOf(error, 'Could not load the profile.')); } }); }
  private apply(item: MedicalServiceProviderProfile): void {
    this.profile.set(item);
    this.form.patchValue({
      businessName: item.businessName ?? '', providerType: item.providerType,
      description: item.description ?? '', phoneNumber: item.phoneNumber ?? '',
      address: item.address ?? '', governorate: item.governorate ?? '', city: item.city ?? '',
      latitude: item.latitude, longitude: item.longitude,
    });
  }
}
