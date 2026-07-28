import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceProviderDetails, PROVIDER_TYPE_LABELS } from '../../../core/models/medical-service-provider.model';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';
import { RatingPanel } from '../../../shared/rating-panel/rating-panel';

@Component({
  selector: 'app-medical-service-provider-details',
  imports: [CurrencyPipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, RatingPanel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> }
      @else if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @else if (provider(); as item) {
        <article class="cc-card hero"><div class="hero-copy">@if (item.profileImageUrl) { <img [src]="item.profileImageUrl" [alt]="item.businessName + ' logo'" class="provider-logo"/> }<div><span class="eyebrow">{{ labels[item.providerType] }}</span><h1>{{ item.businessName }}</h1><p>{{ item.description }}</p><p><mat-icon>location_on</mat-icon>{{ item.address }}, {{ item.city }}, {{ item.governorate }}</p><p><mat-icon>phone</mat-icon>{{ item.phoneNumber }}</p>@if (item.distanceKm !== null) { <strong>{{ item.distanceKm }} km away (approximate straight-line distance)</strong> }</div></div>
          <div class="hero-actions"><button mat-stroked-button (click)="calculateDistance()"><mat-icon>my_location</mat-icon>Distance from me</button><a mat-flat-button [href]="item.directionsUrl" target="_blank" rel="noopener"><mat-icon>directions</mat-icon>Get directions</a></div>
        </article>
          <div class="columns"><section><h2>Services and prices</h2><div class="services">@for (service of item.services; track service.id) { <article class="cc-card"><div class="service-head"><div><small>{{ service.categoryName }}</small><h3>{{ service.name }}</h3></div><strong>{{ service.price | currency:'EGP ':'symbol':'1.0-2' }}</strong></div><p>{{ service.description }}</p>@if (service.estimatedDurationMinutes) { <p><b>Estimated duration:</b> {{ service.estimatedDurationMinutes }} minutes</p> }<p><b>Delivery:</b> {{ deliveryLabel(service.deliveryModeAvailability) }}</p>@if (service.preparationInstructions) { <p><b>Preparation:</b> {{ service.preparationInstructions }}</p> }@if (isPatient()) { <a mat-flat-button [routerLink]="['/medical-service-providers', item.id, 'services', service.id, 'request']"><mat-icon>send</mat-icon>Request service</a> }</article> }</div></section>
          <section><h2>Working hours</h2><article class="cc-card">@for (hour of item.workingHours; track hour.dayOfWeek) { <div class="hour"><span>{{ hour.dayName }}</span><strong>{{ hour.isClosed ? 'Closed' : hour.openTime + ' – ' + hour.closeTime }}</strong></div> }</article></section></div>
        <app-rating-panel [type]="3" [targetId]="item.id"/>
      }
    </section>
  `,
  styles: `.hero,.hero-actions,.service-head,.hour,.hero-copy{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.hero-copy{justify-content:flex-start;flex-wrap:nowrap}.provider-logo{width:90px;height:90px;border-radius:18px;object-fit:cover;flex:0 0 90px}.hero p mat-icon{font-size:19px;width:19px;height:19px;vertical-align:middle}.hero-actions{flex-direction:column}.eyebrow{color:var(--cc-brand);font-weight:700}.columns{display:grid;grid-template-columns:2fr 1fr;gap:22px;margin-top:22px}.services{display:grid;gap:12px}.services h3{margin:4px 0}.hour{padding:10px 0;border-bottom:1px solid var(--mat-sys-outline-variant)}.hour:last-child{border:0}@media(max-width:850px){.columns{grid-template-columns:1fr}}@media(max-width:520px){.hero-copy{flex-wrap:wrap}.provider-logo{width:72px;height:72px;flex-basis:72px}}`,
})
export class MedicalServiceProviderDetailsPage implements OnInit {
  readonly id = input.required<string>();
  private readonly service = inject(MedicalServiceProviderService);
  private readonly auth = inject(AuthService);
  private readonly geolocation = inject(GeolocationService);
  private readonly notify = inject(NotificationService);
  protected readonly labels = PROVIDER_TYPE_LABELS;
  protected readonly provider = signal<MedicalServiceProviderDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly isPatient = computed(() => this.auth.role() === 'Patient');
  ngOnInit(): void { this.load(); }
  protected deliveryLabel(value: number): string {
    if (value === 3) return 'Provider location or home visit';
    return value === 2 ? 'Home visit only' : 'Provider location only';
  }
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
