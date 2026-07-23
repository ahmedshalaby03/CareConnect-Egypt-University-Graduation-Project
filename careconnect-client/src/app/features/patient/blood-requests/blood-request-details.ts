import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { BloodRequestStatus, PatientBloodRequest } from '../../../core/models/blood-request.model';
import { BloodRequestService } from '../../../core/services/blood-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-patient-blood-request-details',
  imports: [RouterLink, DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './blood-request-details.html',
  styleUrl: './blood-request-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BloodRequestDetails implements OnInit {
  private readonly bloodRequests = inject(BloodRequestService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly request = signal<PatientBloodRequest | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);

  ngOnInit(): void {
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

  protected canCancel(request: PatientBloodRequest): boolean {
    return request.statusName === 'Pending';
  }

  protected cancel(): void {
    const current = this.request();
    if (!current) {
      return;
    }

    const data: ConfirmDialogData = {
      title: 'Cancel blood request?',
      message: `Withdraw the ${current.bloodGroupDisplayName} request for ${current.beneficiaryName}?`,
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

        this.bloodRequests.cancelRequest(current.bloodRequestId).subscribe({
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

    this.bloodRequests.getPatientRequest(this.id()).subscribe({
      next: (request) => {
        this.loading.set(false);
        this.request.set(request);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this blood request.'));
      },
    });
  }
}
