import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { DELIVERY_MODE_LABELS, MEDICAL_SERVICE_REQUEST_STATUS_LABELS, MedicalServiceRequestDetails } from '../../../core/models/medical-service-request.model';
import { MedicalServiceRequestService } from '../../../core/services/medical-service-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { ReasonDialog, ReasonDialogData } from '../../../shared/reason-dialog/reason-dialog';
import { AcceptServiceRequestDialog, AcceptServiceRequestDialogData } from './accept-service-request-dialog';

@Component({
  selector: 'app-provider-service-request-details',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <a mat-button routerLink="/dashboard/service-provider/requests"><mat-icon>arrow_back</mat-icon>Back to requests</a>
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> }
      @else if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @else if (request(); as item) {
        <header class="head"><div><span class="eyebrow">{{ item.requestNumber }}</span><h1>{{ item.serviceName }}</h1><p>Submitted by {{ item.patientName }}</p></div><span class="cc-status-chip" [class.cc-status-chip--active]="item.status === 2 || item.status === 6" [class.cc-status-chip--pending]="item.status === 1" [class.cc-status-chip--inactive]="item.status === 3 || item.status === 4 || item.status === 5">{{ statusLabels[item.status] }}</span></header>
        <div class="actions">@if (item.status === 1) { <button mat-flat-button (click)="accept()"><mat-icon>check</mat-icon>Accept</button><button mat-stroked-button color="warn" (click)="reject()"><mat-icon>close</mat-icon>Reject</button> } @if (item.status === 2) { <button mat-flat-button (click)="complete()"><mat-icon>task_alt</mat-icon>Mark completed</button><button mat-stroked-button color="warn" (click)="cancel()"><mat-icon>cancel</mat-icon>Cancel</button> }</div>
        <div class="grid">
          <article class="cc-card"><h2>Patient and request</h2><dl><dt>Patient</dt><dd>{{ item.patientName }}</dd><dt>Phone</dt><dd>{{ item.patientPhoneNumber || 'Not provided' }}</dd><dt>Delivery</dt><dd>{{ deliveryLabels[item.deliveryMode] }}</dd>@if (item.homeVisitAddress) { <dt>Home-visit address</dt><dd>{{ item.homeVisitAddress }}</dd> }<dt>Preferred</dt><dd>{{ item.requestedDate | date:'mediumDate' }} at {{ item.preferredStartTime }}</dd><dt>Confirmed</dt><dd>{{ item.scheduledAt ? (item.scheduledAt | date:'medium') : 'Not confirmed' }}</dd>@if (item.patientNotes) { <dt>Patient notes</dt><dd>{{ item.patientNotes }}</dd> }</dl></article>
          <article class="cc-card"><h2>Service snapshot</h2><dl><dt>Service</dt><dd>{{ item.serviceName }}</dd><dt>Category</dt><dd>{{ item.categoryName }}</dd><dt>Price</dt><dd>{{ item.priceSnapshot | currency:'EGP ':'symbol':'1.0-2' }}</dd><dt>Duration</dt><dd>{{ item.durationMinutesSnapshot ? item.durationMinutesSnapshot + ' minutes' : 'Not specified' }}</dd></dl>@if (item.providerResponseNote) { <p><b>Response:</b> {{ item.providerResponseNote }}</p> }@if (item.rejectionReason) { <div class="cc-notice cc-notice--error"><b>Rejection:</b> {{ item.rejectionReason }}</div> }@if (item.cancellationReason) { <div class="cc-notice"><b>Cancellation:</b> {{ item.cancellationReason }}</div> }</article>
        </div>
        <article class="cc-card timeline"><h2>Status timeline</h2>@for (history of item.statusHistory; track history.createdAt + history.newStatus) { <div class="event"><mat-icon>radio_button_checked</mat-icon><div><strong>{{ statusLabels[history.newStatus] }}</strong><p>{{ history.actorLabel }} · {{ history.createdAt | date:'medium' }}</p>@if (history.reason) { <p>{{ history.reason }}</p> }</div></div> }</article>
      }
    </section>
  `,
  styles: `.head,.actions{display:flex;justify-content:space-between;gap:14px;align-items:flex-start;flex-wrap:wrap}.actions{justify-content:flex-start;margin:16px 0}.eyebrow{color:var(--cc-primary);font-weight:700}.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px}dl{display:grid;grid-template-columns:minmax(120px,auto) 1fr;gap:10px 16px}dt{font-weight:700}dd{margin:0}.timeline{margin-top:20px}.event{display:flex;gap:14px;padding:10px 0}.event mat-icon{font-size:16px;color:var(--cc-primary)}.event p{margin:4px 0;color:var(--mat-sys-on-surface-variant)}@media(max-width:800px){.grid{grid-template-columns:1fr}}`,
})
export class ProviderServiceRequestDetails implements OnInit {
  readonly id = input.required<string>();
  private readonly requests = inject(MedicalServiceRequestService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  protected readonly statusLabels = MEDICAL_SERVICE_REQUEST_STATUS_LABELS;
  protected readonly deliveryLabels = DELIVERY_MODE_LABELS;
  protected readonly request = signal<MedicalServiceRequestDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  ngOnInit(): void { this.load(); }

  protected accept(): void {
    const request = this.request(); if (!request) return;
    const data: AcceptServiceRequestDialogData = { request };
    this.dialog.open(AcceptServiceRequestDialog, { data }).afterClosed().subscribe((response) => {
      if (!response) return; this.request.set(response.data!); this.notify.success(response.message);
    });
  }
  protected reject(): void { this.withReason('Reject request?', 'Explain why this request cannot be accepted.', 'Rejection reason', 'Reject', (reason) => this.requests.reject(this.id(), { rejectionReason: reason, providerResponseNote: null })); }
  protected cancel(): void { this.withReason('Cancel accepted request?', 'The patient will see this reason in the request history.', 'Cancellation reason', 'Cancel request', (reason) => this.requests.cancelByProvider(this.id(), { cancellationReason: reason })); }
  protected complete(): void {
    const data: ConfirmDialogData = { title: 'Mark service completed?', message: 'Confirm that this accepted service has started or been delivered.', confirmLabel: 'Mark completed' };
    this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data }).afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.requests.complete(this.id()).subscribe({
        next: (response) => { this.request.set(response.data!); this.notify.success(response.message); },
        error: (error: unknown) => this.notify.error(friendlyMessageOf(error, 'Could not complete this request.')),
      });
    });
  }
  private withReason(title: string, message: string, fieldLabel: string, confirmLabel: string, operation: (reason: string) => ReturnType<MedicalServiceRequestService['cancelByProvider']>): void {
    const data: ReasonDialogData = { title, message, fieldLabel, confirmLabel, destructive: true };
    this.dialog.open<ReasonDialog, ReasonDialogData, string>(ReasonDialog, { data }).afterClosed().subscribe((reason) => {
      if (!reason) return;
      operation(reason).subscribe({
        next: (response) => { this.request.set(response.data!); this.notify.success(response.message); },
        error: (error: unknown) => this.notify.error(friendlyMessageOf(error, 'Could not update this request.')),
      });
    });
  }
  private load(): void {
    this.requests.getProviderRequest(this.id()).subscribe({
      next: (item) => { this.request.set(item); this.loading.set(false); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load this request.')); },
    });
  }
}
