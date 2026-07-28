import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceProviderDetails, PROVIDER_TYPE_LABELS } from '../../../core/models/medical-service-provider.model';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-medical-service-provider-details',
  imports: [CurrencyPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> }
      @else if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @else if (provider(); as item) {
        <article class="cc-card hero"><div><span class="eyebrow">{{ labels[item.providerType] }}</span><h1>{{ item.businessName }}</h1><p>{{ item.description }}</p><p><mat-icon>location_on</mat-icon>{{ item.address }}, {{ item.city }}, {{ item.governorate }}</p><p><mat-icon>phone</mat-icon>{{ item.phoneNumber }}</p>@if (item.distanceKm !== null) { <strong>{{ item.distanceKm }} km away (approximate straight-line distance)</strong> }</div>
          <div class="hero-actions"><button mat-stroked-button (click)="calculateDistance()"><mat-icon>my_location</mat-icon>Distance from me</button><a mat-flat-button [href]="item.directionsUrl" target="_blank" rel="noopener"><mat-icon>directions</mat-icon>Get directions</a></div>
        </article>
        <div class="columns"><section><h2>Services and prices</h2><div class="services">@for (service of item.services; track service.id) { <article class="cc-card"><div class="service-head"><div><small>{{ service.categoryName }}</small><h3>{{ service.name }}</h3></div><strong>{{ service.price | currency:'EGP ':'symbol':'1.0-2' }}</strong></div><p>{{ service.description }}</p>@if (service.estimatedDurationMinutes) { <p><b>Estimated duration:</b> {{ service.estimatedDurationMinutes }} minutes</p> }@if (service.preparationInstructions) { <p><b>Preparation:</b> {{ service.preparationInstructions }}</p> }</article> }</div></section>
          <section><h2>Working hours</h2><article class="cc-card">@for (hour of item.workingHours; track hour.dayOfWeek) { <div class="hour"><span>{{ hour.dayName }}</span><strong>{{ hour.isClosed ? 'Closed' : hour.openTime + ' – ' + hour.closeTime }}</strong></div> }</article></section></div>
      }
    </section>
  `,
  styles: `.hero,.hero-actions,.service-head,.hour{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.hero p mat-icon{font-size:19px;width:19px;height:19px;vertical-align:middle}.hero-actions{flex-direction:column}.eyebrow{color:var(--cc-primary);font-weight:700}.columns{display:grid;grid-template-columns:2fr 1fr;gap:22px;margin-top:22px}.services{display:grid;gap:12px}.services h3{margin:4px 0}.hour{padding:10px 0;border-bottom:1px solid var(--mat-sys-outline-variant)}.hour:last-child{border:0}@media(max-width:850px){.columns{grid-template-columns:1fr}}`,
})
export class MedicalServiceProviderDetailsPage implements OnInit {
  readonly id = input.required<string>();
  private readonly service = inject(MedicalServiceProviderService);
  private readonly geolocation = inject(GeolocationService);
  private readonly notify = inject(NotificationService);
  protected readonly labels = PROVIDER_TYPE_LABELS;
  protected readonly provider = signal<MedicalServiceProviderDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  ngOnInit(): void { this.load(); }
  protected calculateDistance(): void {
    this.geolocation.getCurrentPosition().then((coords) => this.load(coords.latitude, coords.longitude)).catch((error: unknown) => this.notify.error(error instanceof GeolocationFailure ? error.message : 'Could not determine your location.'));
  }
  private load(latitude?: number, longitude?: number): void {
    this.loading.set(true); this.error.set(null);
    this.service.getDetails(this.id(), latitude, longitude).subscribe({
      next: (item) => { this.provider.set(item); this.loading.set(false); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load this provider.')); },
    });
  }
}
