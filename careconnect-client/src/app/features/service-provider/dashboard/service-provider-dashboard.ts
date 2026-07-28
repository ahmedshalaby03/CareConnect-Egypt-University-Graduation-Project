import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceProviderProfile } from '../../../core/models/medical-service-provider.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';

@Component({
  selector: 'app-service-provider-dashboard',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <header class="page-head">
        <div>
          <span class="eyebrow">Provider workspace</span>
          <h1>{{ profile()?.businessName || 'Medical service provider' }}</h1>
          <p>Manage the information patients see in the medical services directory.</p>
        </div>
        <a mat-flat-button routerLink="/dashboard/service-provider/profile">
          <mat-icon>edit</mat-icon> Complete profile
        </a>
      </header>

      @if (loading()) {
        <div class="cc-loading"><mat-spinner diameter="42" /></div>
      } @else if (error()) {
        <div class="cc-notice cc-notice--error">{{ error() }}</div>
      } @else if (profile(); as item) {
        <div class="cc-card-grid stats">
          <article class="cc-card"><mat-icon>public</mat-icon><strong>{{ item.isPublished ? 'Published' : 'Draft' }}</strong><span>Directory status</span></article>
          <article class="cc-card"><mat-icon>medical_services</mat-icon><strong>{{ item.activeServicesCount }}</strong><span>Active services</span></article>
          <article class="cc-card"><mat-icon>visibility_off</mat-icon><strong>{{ item.inactiveServicesCount }}</strong><span>Inactive services</span></article>
          <article class="cc-card"><mat-icon>category</mat-icon><strong>{{ item.serviceCategoriesCount }}</strong><span>Categories used</span></article>
          <article class="cc-card"><mat-icon>{{ item.isReadyToPublish ? 'task_alt' : 'pending_actions' }}</mat-icon><strong>{{ item.isReadyToPublish ? 'Complete' : 'Needs work' }}</strong><span>Publication readiness</span></article>
        </div>
        @if (!item.isReadyToPublish) {
          <article class="cc-card requirements">
            <h2>Before publishing</h2>
            <ul>@for (requirement of item.missingRequirements; track requirement) { <li>{{ requirement }}</li> }</ul>
          </article>
        }
        <div class="quick-links">
          <a mat-stroked-button routerLink="/dashboard/service-provider/services"><mat-icon>medical_services</mat-icon> My services</a>
          <a mat-stroked-button routerLink="/dashboard/service-provider/working-hours"><mat-icon>schedule</mat-icon> Working hours</a>
          <a mat-stroked-button routerLink="/dashboard/service-provider/preview"><mat-icon>preview</mat-icon> Public preview</a>
        </div>
      }
    </section>
  `,
  styles: `
    .page-head,.quick-links{display:flex;justify-content:space-between;gap:16px;flex-wrap:wrap}.eyebrow{color:var(--cc-primary);font-weight:700}.stats{margin:24px 0}.stats article{display:grid;gap:8px}.stats mat-icon{color:var(--cc-primary)}.stats strong{font-size:1.45rem}.stats span{color:var(--mat-sys-on-surface-variant)}.requirements{margin-bottom:20px}.requirements h2{margin-top:0}.requirements li{margin:6px 0}
  `,
})
export class ServiceProviderDashboard implements OnInit {
  private readonly providers = inject(MedicalServiceProviderService);
  protected readonly profile = signal<MedicalServiceProviderProfile | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.providers.getProfile().subscribe({
      next: (profile) => { this.profile.set(profile); this.loading.set(false); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load the provider dashboard.')); },
    });
  }
}
