import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  AFFILIATION_STATUSES,
  AffiliatedHospital,
  AffiliationStatus,
  DoctorHospitalRequest,
} from '../../../core/models/affiliation.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-doctor-hospital-requests',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './doctor-hospital-requests.html',
  styleUrl: './doctor-hospital-requests.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorHospitalRequests implements OnInit {
  private readonly affiliations = inject(AffiliationService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly statuses = AFFILIATION_STATUSES;

  protected readonly statusControl = new FormControl<AffiliationStatus | ''>('', { nonNullable: true });

  protected readonly requests = signal<DoctorHospitalRequest[]>([]);
  protected readonly approvedHospitals = signal<AffiliatedHospital[]>([]);
  protected readonly loading = signal(true);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
    this.load();
    this.loadApproved();
  }

  protected onStatusChange(): void {
    this.pageIndex.set(0);
    this.load();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected statusClass(status: AffiliationStatus): string {
    switch (status) {
      case 'Approved':
        return 'cc-status-chip--active';
      case 'Pending':
        return 'cc-status-chip--pending';
      default:
        return 'cc-status-chip--inactive';
    }
  }

  /** Only a request the hospital has not reviewed yet can be withdrawn. */
  protected cancel(request: DoctorHospitalRequest): void {
    const data: ConfirmDialogData = {
      title: 'Cancel request?',
      message: `Withdraw your affiliation request to ${request.hospitalName}? You can apply again later.`,
      confirmLabel: 'Cancel request',
      cancelLabel: 'Keep it',
      destructive: true,
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.busyId.set(request.id);

        this.affiliations.cancelRequest(request.id).subscribe({
          next: (response) => {
            this.busyId.set(null);
            this.notify.success(response.message);
            this.load();
          },
          error: (error: unknown) => {
            this.busyId.set(null);
            this.notify.error(friendlyMessageOf(error, 'Could not cancel the request.'));
          },
        });
      });
  }

  protected setPrimary(hospitalId: string): void {
    this.busyId.set(hospitalId);

    this.affiliations.setPrimaryHospital(hospitalId).subscribe({
      next: (response) => {
        this.busyId.set(null);
        this.notify.success(response.message);
        this.loadApproved();
        this.load();
      },
      error: (error: unknown) => {
        this.busyId.set(null);
        this.notify.error(friendlyMessageOf(error, 'Could not set the primary hospital.'));
      },
    });
  }

  private load(): void {
    this.loading.set(true);

    this.affiliations
      .getDoctorRequests({
        status: this.statusControl.value || null,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.requests.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load your requests.'));
        },
      });
  }

  private loadApproved(): void {
    this.affiliations.getDoctorHospitals().subscribe({
      next: (hospitals) => this.approvedHospitals.set(hospitals),
      error: () => undefined,
    });
  }
}
