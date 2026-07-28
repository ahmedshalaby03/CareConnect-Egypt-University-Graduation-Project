import { CurrencyPipe, DatePipe } from '@angular/common';
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
import {
  DELIVERY_MODE_LABELS,
  MEDICAL_SERVICE_REQUEST_STATUSES,
  MEDICAL_SERVICE_REQUEST_STATUS_LABELS,
  MedicalServiceRequestStatus,
  MedicalServiceRequestSummary,
} from '../../../core/models/medical-service-request.model';
import { MedicalServiceRequestService } from '../../../core/services/medical-service-request.service';

@Component({
  selector: 'app-patient-service-requests',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressBarModule, MatSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <header class="head"><div><span class="eyebrow">Patient workspace</span><h1>Service Requests</h1><p>Track requests sent to medical service providers.</p></div><a mat-flat-button routerLink="/medical-service-providers"><mat-icon>add</mat-icon>Find a service</a></header>
      <section class="cc-card cc-filters">
        <mat-form-field><mat-label>Search</mat-label><input matInput [formControl]="search" placeholder="Request, service, provider"/></mat-form-field>
        <mat-form-field><mat-label>Status</mat-label><mat-select [formControl]="status"><mat-option [value]="null">All statuses</mat-option>@for (item of statuses; track item) { <mat-option [value]="item">{{ statusLabels[item] }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field><mat-label>From</mat-label><input matInput type="date" [formControl]="dateFrom"/></mat-form-field>
        <mat-form-field><mat-label>To</mat-label><input matInput type="date" [formControl]="dateTo"/></mat-form-field>
        <button mat-stroked-button (click)="applyFilters()"><mat-icon>search</mat-icon>Apply</button>
      </section>
      @if (loading()) { <mat-progress-bar mode="indeterminate"/> }
      @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @if (!loading() && !items().length) { <div class="cc-empty-state"><mat-icon>medical_services</mat-icon><h2>No service requests found</h2><p>Choose a published provider and request one of its active services.</p></div> }
      <div class="requests">@for (item of items(); track item.id) {
        <article class="cc-card request">
          <div><div class="row"><strong>{{ item.requestNumber }}</strong><span class="cc-status-chip" [class.cc-status-chip--active]="item.status === 2 || item.status === 6" [class.cc-status-chip--pending]="item.status === 1" [class.cc-status-chip--inactive]="item.status === 3 || item.status === 4 || item.status === 5">{{ statusLabels[item.status] }}</span></div><h2>{{ item.serviceName }}</h2><p>{{ item.providerName }} · {{ deliveryLabels[item.deliveryMode] }}</p><p>Preferred: {{ item.requestedDate | date:'mediumDate' }} at {{ item.preferredStartTime }} @if (item.scheduledAt) { · Confirmed: {{ item.scheduledAt | date:'medium' }} }</p><strong>{{ item.priceSnapshot | currency:'EGP ':'symbol':'1.0-2' }}</strong></div>
          <a mat-stroked-button [routerLink]="[item.id]">View details</a>
        </article>
      }</div>
      @if (totalCount() > pageSize()) { <mat-paginator [length]="totalCount()" [pageIndex]="page()-1" [pageSize]="pageSize()" [pageSizeOptions]="[5,10,20]" (page)="pageChanged($event)"/> }
    </section>
  `,
  styles: `.head,.row,.request{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.eyebrow{color:var(--cc-primary);font-weight:700}.cc-filters{display:grid;grid-template-columns:2fr repeat(3,1fr) auto;gap:12px;align-items:start}.requests{display:grid;gap:14px;margin-top:18px}.request h2{margin:8px 0}.request p{color:var(--mat-sys-on-surface-variant)}@media(max-width:900px){.cc-filters{grid-template-columns:1fr 1fr}}@media(max-width:600px){.cc-filters{grid-template-columns:1fr}.request>a{width:100%}}`,
})
export class PatientServiceRequests implements OnInit {
  private readonly requests = inject(MedicalServiceRequestService);
  protected readonly statuses = MEDICAL_SERVICE_REQUEST_STATUSES;
  protected readonly statusLabels = MEDICAL_SERVICE_REQUEST_STATUS_LABELS;
  protected readonly deliveryLabels = DELIVERY_MODE_LABELS;
  protected readonly items = signal<MedicalServiceRequestSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly totalCount = signal(0);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly status = new FormControl<MedicalServiceRequestStatus | null>(null);
  protected readonly dateFrom = new FormControl('', { nonNullable: true });
  protected readonly dateTo = new FormControl('', { nonNullable: true });

  ngOnInit(): void { this.load(); }
  protected applyFilters(): void { this.page.set(1); this.load(); }
  protected pageChanged(event: PageEvent): void { this.page.set(event.pageIndex + 1); this.pageSize.set(event.pageSize); this.load(); }
  private load(): void {
    this.loading.set(true); this.error.set(null);
    this.requests.getPatientRequests({
      search: this.search.value.trim(), status: this.status.value,
      dateFrom: this.dateFrom.value, dateTo: this.dateTo.value,
      page: this.page(), pageSize: this.pageSize(),
    }).subscribe({
      next: (result) => { this.loading.set(false); this.items.set(result.items); this.totalCount.set(result.totalCount); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load service requests.')); },
    });
  }
}
