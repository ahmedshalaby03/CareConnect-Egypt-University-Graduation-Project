import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceOffering } from '../../../core/models/medical-service-provider.model';
import { DELIVERY_MODE_LABELS, MEDICAL_SERVICE_REQUEST_STATUSES, MEDICAL_SERVICE_REQUEST_STATUS_LABELS, MedicalServiceRequestStatus, MedicalServiceRequestSummary, ServiceDeliveryMode } from '../../../core/models/medical-service-request.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { MedicalServiceRequestService } from '../../../core/services/medical-service-request.service';

@Component({
  selector: 'app-provider-service-requests',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressBarModule, MatSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page"><header><span class="eyebrow">Provider workspace</span><h1>Service Requests</h1><p>Review and manage requests sent to your provider profile.</p></header>
      <section class="cc-card cc-filters">
        <mat-form-field><mat-label>Search</mat-label><input matInput [formControl]="search" placeholder="Request or patient"/></mat-form-field>
        <mat-form-field><mat-label>Status</mat-label><mat-select [formControl]="status"><mat-option [value]="null">All statuses</mat-option>@for (item of statuses; track item) { <mat-option [value]="item">{{ statusLabels[item] }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field><mat-label>Service</mat-label><mat-select [formControl]="serviceId"><mat-option [value]="null">All services</mat-option>@for (service of services(); track service.id) { <mat-option [value]="service.id">{{ service.name }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field><mat-label>Delivery</mat-label><mat-select [formControl]="deliveryMode"><mat-option [value]="null">All modes</mat-option><mat-option [value]="1">At provider location</mat-option><mat-option [value]="2">Home visit</mat-option></mat-select></mat-form-field>
        <mat-form-field><mat-label>From</mat-label><input matInput type="date" [formControl]="dateFrom"/></mat-form-field>
        <mat-form-field><mat-label>To</mat-label><input matInput type="date" [formControl]="dateTo"/></mat-form-field>
        <button mat-stroked-button (click)="applyFilters()"><mat-icon>filter_alt</mat-icon>Apply</button>
      </section>
      @if (loading()) { <mat-progress-bar mode="indeterminate"/> }
      @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @if (!loading() && !items().length) { <div class="cc-empty-state"><mat-icon>inbox</mat-icon><h2>No matching requests</h2></div> }
      <div class="requests">@for (item of items(); track item.id) { <article class="cc-card request"><div><div class="row"><strong>{{ item.requestNumber }}</strong><span class="cc-status-chip" [class.cc-status-chip--active]="item.status === 2 || item.status === 6" [class.cc-status-chip--pending]="item.status === 1" [class.cc-status-chip--inactive]="item.status === 3 || item.status === 4 || item.status === 5">{{ statusLabels[item.status] }}</span></div><h2>{{ item.serviceName }}</h2><p><b>{{ item.patientName }}</b> · {{ deliveryLabels[item.deliveryMode] }}</p><p>Preferred {{ item.requestedDate | date:'mediumDate' }} at {{ item.preferredStartTime }} @if (item.scheduledAt) { · Confirmed {{ item.scheduledAt | date:'medium' }} }</p></div><a mat-stroked-button [routerLink]="[item.id]">View details</a></article> }</div>
      @if (totalCount() > pageSize()) { <mat-paginator [length]="totalCount()" [pageIndex]="page()-1" [pageSize]="pageSize()" [pageSizeOptions]="[5,10,20]" (page)="pageChanged($event)"/> }
    </section>
  `,
  styles: `.eyebrow{color:var(--cc-primary);font-weight:700}.cc-filters{display:grid;grid-template-columns:2fr repeat(5,1fr) auto;gap:10px;align-items:start}.requests{display:grid;gap:14px;margin-top:18px}.request,.row{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.request h2{margin:8px 0}.request p{color:var(--mat-sys-on-surface-variant)}@media(max-width:1050px){.cc-filters{grid-template-columns:repeat(2,1fr)}}@media(max-width:600px){.cc-filters{grid-template-columns:1fr}.request>a{width:100%}}`,
})
export class ProviderServiceRequests implements OnInit {
  private readonly requests = inject(MedicalServiceRequestService);
  private readonly providers = inject(MedicalServiceProviderService);
  protected readonly statuses = MEDICAL_SERVICE_REQUEST_STATUSES;
  protected readonly statusLabels = MEDICAL_SERVICE_REQUEST_STATUS_LABELS;
  protected readonly deliveryLabels = DELIVERY_MODE_LABELS;
  protected readonly services = signal<MedicalServiceOffering[]>([]);
  protected readonly items = signal<MedicalServiceRequestSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1); protected readonly pageSize = signal(10); protected readonly totalCount = signal(0);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly status = new FormControl<MedicalServiceRequestStatus | null>(null);
  protected readonly serviceId = new FormControl<string | null>(null);
  protected readonly deliveryMode = new FormControl<ServiceDeliveryMode | null>(null);
  protected readonly dateFrom = new FormControl('', { nonNullable: true });
  protected readonly dateTo = new FormControl('', { nonNullable: true });
  ngOnInit(): void { this.providers.getServices().subscribe((items) => this.services.set(items)); this.load(); }
  protected applyFilters(): void { this.page.set(1); this.load(); }
  protected pageChanged(event: PageEvent): void { this.page.set(event.pageIndex + 1); this.pageSize.set(event.pageSize); this.load(); }
  private load(): void {
    this.loading.set(true); this.error.set(null);
    this.requests.getProviderRequests({ search: this.search.value.trim(), status: this.status.value, serviceId: this.serviceId.value, deliveryMode: this.deliveryMode.value, dateFrom: this.dateFrom.value, dateTo: this.dateTo.value, page: this.page(), pageSize: this.pageSize() }).subscribe({
      next: (result) => { this.loading.set(false); this.items.set(result.items); this.totalCount.set(result.totalCount); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load provider requests.')); },
    });
  }
}
