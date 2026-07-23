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
import { InsuranceCompanyOption } from '../../../core/models/insurance-company.model';
import {
  INSURANCE_REQUEST_STATUSES,
  InsuranceRequestStatus,
  PatientInsuranceRequest,
} from '../../../core/models/insurance-request.model';
import { InsuranceCompanyService } from '../../../core/services/insurance-company.service';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-patient-insurance-requests',
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
  templateUrl: './insurance-requests.html',
  styleUrl: './insurance-requests.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientInsuranceRequests implements OnInit {
  private readonly insuranceRequests = inject(InsuranceRequestService);
  private readonly insuranceCompanies = inject(InsuranceCompanyService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly statuses = INSURANCE_REQUEST_STATUSES;

  protected readonly statusControl = new FormControl<InsuranceRequestStatus | ''>('', { nonNullable: true });
  protected readonly companyControl = new FormControl<string>('', { nonNullable: true });

  protected readonly companies = signal<InsuranceCompanyOption[]>([]);
  protected readonly requests = signal<PatientInsuranceRequest[]>([]);
  protected readonly loading = signal(true);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
    this.insuranceCompanies.getActive().subscribe({
      next: (items) => this.companies.set(items),
      error: () => undefined,
    });

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

  protected statusClass(status: InsuranceRequestStatus): string {
    switch (status) {
      case 'Approved':
        return 'cc-status-chip--active';
      case 'Pending':
      case 'UnderReview':
        return 'cc-status-chip--pending';
      default:
        return 'cc-status-chip--inactive';
    }
  }

  protected canCancel(request: PatientInsuranceRequest): boolean {
    return request.statusName === 'Pending';
  }

  protected cancel(request: PatientInsuranceRequest): void {
    const data: ConfirmDialogData = {
      title: 'Cancel insurance request?',
      message: `Withdraw your insurance request with ${request.insuranceCompanyName} for this appointment?`,
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

        this.busyId.set(request.insuranceRequestId);

        this.insuranceRequests.cancelRequest(request.insuranceRequestId).subscribe({
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

    this.insuranceRequests
      .getPatientRequests({
        status: this.statusControl.value || null,
        insuranceCompanyId: this.companyControl.value || null,
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
          this.notify.error(friendlyMessageOf(error, 'Could not load your insurance requests.'));
        },
      });
  }
}
