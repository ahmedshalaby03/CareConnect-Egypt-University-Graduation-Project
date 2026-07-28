import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceCategoryOption, MedicalServiceOffering } from '../../../core/models/medical-service-provider.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';
import { ServiceFormDialog } from './service-form-dialog';

@Component({
  selector: 'app-service-provider-services',
  imports: [CurrencyPipe, MatButtonModule, MatIconModule, MatProgressBarModule, MatSlideToggleModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <header class="page-head"><div><h1>My services</h1><p>Manage the catalog patients see on your published profile.</p></div><button mat-flat-button (click)="openForm(null)"><mat-icon>add</mat-icon>Add service</button></header>
      @if (loading()) { <mat-progress-bar mode="indeterminate"/> }
      @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @if (!loading() && !services().length) { <div class="cc-empty-state"><mat-icon>medical_services</mat-icon><h2>No services yet</h2><p>Add your first service before publishing.</p></div> }
      <div class="cc-card-grid">
        @for (service of services(); track service.id) {
          <article class="cc-card service-card">
            <div class="service-head"><div><span class="eyebrow">{{ service.categoryName }}</span><h2>{{ service.name }}</h2></div><span class="cc-status-chip" [class.cc-status-chip--active]="service.isActive" [class.cc-status-chip--inactive]="!service.isActive">{{ service.isActive ? 'Active' : 'Inactive' }}</span></div>
            <p>{{ service.description || 'No description provided.' }}</p>
            <div class="facts"><strong>{{ service.price | currency:'EGP ':'symbol':'1.0-2' }}</strong><span>{{ service.estimatedDurationMinutes ? service.estimatedDurationMinutes + ' minutes' : 'Duration not set' }}</span></div>
            <div class="actions"><button mat-stroked-button (click)="openForm(service)"><mat-icon>edit</mat-icon>Edit</button><button mat-button (click)="confirmStatus(service)">{{ service.isActive ? 'Deactivate' : 'Activate' }}</button></div>
          </article>
        }
      </div>
    </section>
  `,
  styles: `.page-head,.service-head,.facts,.actions{display:flex;justify-content:space-between;gap:12px;align-items:flex-start;flex-wrap:wrap}.cc-card-grid{margin-top:20px}.service-card h2{margin:4px 0}.eyebrow{color:var(--cc-primary);font-weight:700}.service-card p{color:var(--mat-sys-on-surface-variant);min-height:48px}.facts{border-top:1px solid var(--mat-sys-outline-variant);padding-top:14px}.actions{margin-top:18px}`,
})
export class ServiceProviderServicesPage implements OnInit {
  private readonly providers = inject(MedicalServiceProviderService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  protected readonly services = signal<MedicalServiceOffering[]>([]);
  protected readonly categories = signal<MedicalServiceCategoryOption[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void { this.load(); this.providers.getActiveCategories().subscribe((items) => this.categories.set(items)); }
  protected openForm(service: MedicalServiceOffering | null): void {
    const ref = this.dialog.open(ServiceFormDialog, { data: { service, categories: this.categories() }, autoFocus: 'first-tabbable' });
    ref.afterClosed().subscribe((message) => { if (message) { this.notify.success(message); this.load(); } });
  }
  protected confirmStatus(service: MedicalServiceOffering): void {
    const ref = this.dialog.open(ConfirmDialog, { data: { title: service.isActive ? 'Deactivate service?' : 'Activate service?', message: service.isActive ? 'It will disappear from the public catalog. Existing data is preserved.' : 'It will be visible when your profile is published.', confirmLabel: service.isActive ? 'Deactivate' : 'Activate', destructive: service.isActive } });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.providers.setServiceStatus(service.id, !service.isActive).subscribe({
        next: (response) => { this.notify.success(response.message); this.load(); },
        error: (error: unknown) => this.notify.error(friendlyMessageOf(error, 'Could not update the service.')),
      });
    });
  }
  private load(): void {
    this.loading.set(true); this.error.set(null);
    this.providers.getServices().subscribe({
      next: (items) => { this.services.set(items); this.loading.set(false); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load services.')); },
    });
  }
}
