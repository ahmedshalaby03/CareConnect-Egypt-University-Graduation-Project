import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { BLOOD_GROUPS } from '../../../core/models/blood-group.model';
import {
  BLOOD_REQUEST_STATUSES,
  BLOOD_REQUEST_URGENCIES,
  BloodRequestStatus,
  BloodRequestUrgency,
  PatientBloodRequest,
} from '../../../core/models/blood-request.model';
import { BloodRequestService } from '../../../core/services/blood-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-patient-blood-requests',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
  ],
  templateUrl: './blood-requests.html',
  styleUrl: './blood-requests.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientBloodRequests implements OnInit {
  private readonly bloodRequests = inject(BloodRequestService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly statuses = BLOOD_REQUEST_STATUSES;
  protected readonly urgencies = BLOOD_REQUEST_URGENCIES;
  protected readonly bloodGroups = BLOOD_GROUPS;

  protected readonly statusControl = new FormControl<BloodRequestStatus | ''>('', { nonNullable: true });
  protected readonly bloodGroupControl = new FormControl<string>('', { nonNullable: true });
  protected readonly urgencyControl = new FormControl<BloodRequestUrgency | ''>('', { nonNullable: true });

  protected readonly requests = signal<PatientBloodRequest[]>([]);
  protected readonly loading = signal(true);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
    this.load();
  }

  protected onFilterChange(): void {
    this.pageIndex.set(0);
    this.load();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected statusClass(status: BloodRequestStatus): string {
    switch (status) {
      case 'Approved':
      case 'Fulfilled':
        return 'cc-status-chip--active';
      case 'Pending':
        return 'cc-status-chip--pending';
      default:
        return 'cc-status-chip--inactive';
    }
  }

  protected urgencyClass(urgency: BloodRequestUrgency): string {
    switch (urgency) {
      case 'Emergency':
        return 'cc-status-chip--inactive';
      case 'Urgent':
        return 'cc-status-chip--pending';
      default:
        return '';
    }
  }

  protected canCancel(request: PatientBloodRequest): boolean {
    return request.statusName === 'Pending';
  }

  protected cancel(request: PatientBloodRequest): void {
    const data: ConfirmDialogData = {
      title: 'Cancel blood request?',
      message: `Withdraw the ${request.bloodGroupDisplayName} request for ${request.beneficiaryName} at ${request.hospitalName}?`,
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

        this.busyId.set(request.bloodRequestId);

        this.bloodRequests.cancelRequest(request.bloodRequestId).subscribe({
          next: (response) => {
            this.busyId.set(null);
            this.notify.success(response.message);
            this.load();
          },
          error: (error: unknown) => {
            this.busyId.set(null);
            this.notify.error(friendlyMessageOf(error, 'Could not cancel this request.'));
          },
        });
      });
  }

  private load(): void {
    this.loading.set(true);

    this.bloodRequests
      .getPatientRequests({
        status: this.statusControl.value || null,
        bloodGroup: (this.bloodGroupControl.value as never) || null,
        urgency: this.urgencyControl.value || null,
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
          this.notify.error(friendlyMessageOf(error, 'Could not load your blood requests.'));
        },
      });
  }
}
