import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { InsuranceRequestStatus, PatientInsuranceRequest } from '../../../core/models/insurance-request.model';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-patient-insurance-request-details',
  imports: [RouterLink, DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './insurance-request-details.html',
  styleUrl: './insurance-request-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InsuranceRequestDetails implements OnInit {
  private readonly insuranceRequests = inject(InsuranceRequestService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly request = signal<PatientInsuranceRequest | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);

  ngOnInit(): void {
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

  protected cancel(): void {
    const current = this.request();
    if (!current) {
      return;
    }

    const data: ConfirmDialogData = {
      title: 'Cancel insurance request?',
      message: `Withdraw your insurance request with ${current.insuranceCompanyName} for this appointment?`,
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

        this.busy.set(true);

        this.insuranceRequests.cancelRequest(current.insuranceRequestId).subscribe({
          next: (response) => {
            this.busy.set(false);
            this.request.set(response.data!);
            this.notify.success(response.message);
          },
          error: (error: unknown) => {
            this.busy.set(false);
            this.notify.error(friendlyMessageOf(error, 'Could not cancel this request.'));
          },
        });
      });
  }

  private load(): void {
    this.loading.set(true);

    this.insuranceRequests.getPatientRequest(this.id()).subscribe({
      next: (request) => {
        this.loading.set(false);
        this.request.set(request);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this insurance request.'));
      },
    });
  }
}
