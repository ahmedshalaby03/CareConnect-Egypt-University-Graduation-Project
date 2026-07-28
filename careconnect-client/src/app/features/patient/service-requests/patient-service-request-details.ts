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
import { ReasonDialog, ReasonDialogData } from '../../../shared/reason-dialog/reason-dialog';
import { ReviewAction } from '../../../shared/review-action/review-action';

@Component({
  selector: 'app-patient-service-request-details',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, ReviewAction],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <a mat-button routerLink="/dashboard/patient/service-requests"><mat-icon>arrow_back</mat-icon>Back to requests</a>
      @if (loading()) { <div class="cc-loading"><mat-spinner diameter="42"/></div> }
      @else if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      @else if (request(); as item) {
        <header class="head"><div><span class="eyebrow">{{ item.requestNumber }}</span><h1>{{ item.serviceName }}</h1><p>{{ item.providerName }}</p></div><span class="cc-status-chip" [class.cc-status-chip--active]="item.status === 2 || item.status === 6" [class.cc-status-chip--pending]="item.status === 1" [class.cc-status-chip--inactive]="item.status === 3 || item.status === 4 || item.status === 5">{{ statusLabels[item.status] }}</span></header>
        <div class="grid">
          <article class="cc-card"><h2>Request details</h2><dl><dt>Category</dt><dd>{{ item.categoryName }}</dd><dt>Price snapshot</dt><dd>{{ item.priceSnapshot | currency:'EGP ':'symbol':'1.0-2' }}</dd><dt>Duration</dt><dd>{{ item.durationMinutesSnapshot ? item.durationMinutesSnapshot + ' minutes' : 'Not specified' }}</dd><dt>Delivery</dt><dd>{{ deliveryLabels[item.deliveryMode] }}</dd><dt>Preferred time</dt><dd>{{ item.requestedDate | date:'mediumDate' }} at {{ item.preferredStartTime }}</dd><dt>Confirmed schedule</dt><dd>{{ item.scheduledAt ? (item.scheduledAt | date:'medium') : 'Awaiting provider confirmation' }}</dd>@if (item.homeVisitAddress) { <dt>Home-visit address</dt><dd>{{ item.homeVisitAddress }}</dd> }@if (item.patientNotes) { <dt>Your notes</dt><dd>{{ item.patientNotes }}</dd> }</dl></article>
          <article class="cc-card"><h2>Provider response</h2><p>{{ item.providerResponseNote || 'No response note yet.' }}</p>@if (item.rejectionReason) { <div class="cc-notice cc-notice--error"><b>Rejection:</b> {{ item.rejectionReason }}</div> }@if (item.cancellationReason) { <div class="cc-notice"><b>Cancellation:</b> {{ item.cancellationReason }}</div> }<p>{{ item.providerPhoneNumber }}<br/>{{ item.providerAddress }}</p>@if (item.status === 1 || item.status === 2) { <button mat-stroked-button color="warn" (click)="cancel()"><mat-icon>cancel</mat-icon>Cancel request</button> }</article>
        </div>
        @if(item.status === 6){<article class="cc-card timeline"><h2>Share your experience</h2><app-review-action [type]="3" [sourceId]="item.id" [targetName]="item.providerName" label="Provider"/></article>}
        <article class="cc-card timeline"><h2>Status timeline</h2>@for (history of item.statusHistory; track history.createdAt + history.newStatus) { <div class="event"><mat-icon>radio_button_checked</mat-icon><div><strong>{{ statusLabels[history.newStatus] }}</strong><p>{{ history.actorLabel }} · {{ history.createdAt | date:'medium' }}</p>@if (history.reason) { <p>{{ history.reason }}</p> }</div></div> }</article>
      }
    </section>
  `,
  styles: `.head{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.eyebrow{color:var(--cc-primary);font-weight:700}.grid{display:grid;grid-template-columns:2fr 1fr;gap:20px}.cc-card h2{margin-top:0}dl{display:grid;grid-template-columns:minmax(130px,auto) 1fr;gap:10px 18px}dt{font-weight:700}dd{margin:0}.timeline{margin-top:20px}.event{display:flex;gap:14px;padding:10px 0}.event mat-icon{color:var(--cc-primary);font-size:16px}.event p{margin:4px 0;color:var(--mat-sys-on-surface-variant)}@media(max-width:800px){.grid{grid-template-columns:1fr}}`,
})
export class PatientServiceRequestDetails implements OnInit {
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
  protected cancel(): void {
    const current = this.request(); if (!current) return;
    const data: ReasonDialogData = { title: 'Cancel service request?', message: 'The reason is recorded in the request history.', fieldLabel: 'Cancellation reason', confirmLabel: 'Cancel request', destructive: true };
    this.dialog.open<ReasonDialog, ReasonDialogData, string>(ReasonDialog, { data }).afterClosed().subscribe((reason) => {
      if (!reason) return;
      this.requests.cancelByPatient(current.id, { cancellationReason: reason }).subscribe({
        next: (response) => { this.request.set(response.data!); this.notify.success(response.message); },
        error: (error: unknown) => this.notify.error(friendlyMessageOf(error, 'Could not cancel this request.')),
      });
    });
  }
  private load(): void {
    this.requests.getPatientRequest(this.id()).subscribe({
      next: (item) => { this.request.set(item); this.loading.set(false); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load this request.')); },
    });
  }
}
