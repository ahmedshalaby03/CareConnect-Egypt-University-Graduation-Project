import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceProviderPreview } from '../../../core/models/medical-service-provider.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';

@Component({
  selector: 'app-service-provider-preview',
  imports: [CurrencyPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page"><header><span class="eyebrow">Owner preview</span><h1>Public profile preview</h1><p>This preview may include draft or inactive content. The directory only exposes published profiles and active services.</p></header>
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> }
      @else if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @else if (preview(); as data) {
        <article class="cc-card hero"><div><span class="cc-status-chip" [class.cc-status-chip--active]="data.profile.isPublished" [class.cc-status-chip--pending]="!data.profile.isPublished">{{ data.profile.isPublished ? 'Published' : 'Draft' }}</span><h2>{{ data.profile.businessName || 'Business name not set' }}</h2><p>{{ data.profile.providerTypeName }} · {{ data.profile.city }}, {{ data.profile.governorate }}</p><p>{{ data.profile.description }}</p></div>
          @if (data.directionsUrl) { <a mat-stroked-button [href]="data.directionsUrl" target="_blank" rel="noopener"><mat-icon>directions</mat-icon>Directions</a> }
        </article>
        <div class="columns"><section><h2>Services</h2><div class="services">@for (service of data.services; track service.id) { <article class="cc-card"><div class="service-head"><div><small>{{ service.categoryName }}</small><h3>{{ service.name }}</h3></div><strong>{{ service.price | currency:'EGP ':'symbol':'1.0-2' }}</strong></div><p>{{ service.description }}</p>@if (service.preparationInstructions) { <p><b>Preparation:</b> {{ service.preparationInstructions }}</p> }</article> } @empty { <div class="cc-empty-state">No services yet.</div> }</div></section>
          <section><h2>Working hours</h2><article class="cc-card">@for (hour of data.workingHours; track hour.dayOfWeek) { <div class="hour"><span>{{ hour.dayName }}</span><strong>{{ hour.isClosed ? 'Closed' : hour.openTime + ' – ' + hour.closeTime }}</strong></div> }</article></section></div>
      }
    </section>
  `,
  styles: `.eyebrow{color:var(--cc-primary);font-weight:700}.hero,.service-head,.hour{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.hero{margin:20px 0}.columns{display:grid;grid-template-columns:2fr 1fr;gap:22px}.services{display:grid;gap:12px}.services h3{margin:4px 0}.hour{padding:10px 0;border-bottom:1px solid var(--mat-sys-outline-variant)}.hour:last-child{border:0}@media(max-width:850px){.columns{grid-template-columns:1fr}}`,
})
export class ServiceProviderPreviewPage implements OnInit {
  private readonly providers = inject(MedicalServiceProviderService);
  protected readonly preview = signal<MedicalServiceProviderPreview | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  ngOnInit(): void { this.providers.getPreview().subscribe({ next: (item) => { this.preview.set(item); this.loading.set(false); }, error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load the preview.')); } }); }
}
