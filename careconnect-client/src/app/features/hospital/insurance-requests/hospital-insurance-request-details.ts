import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { HospitalInsuranceRequest, InsuranceRequestStatus } from '../../../core/models/insurance-request.model';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  ApproveInsuranceRequestDialog,
  ApproveInsuranceRequestDialogData,
} from './approve-insurance-request-dialog';
import {
  RejectInsuranceRequestDialog,
  RejectInsuranceRequestDialogData,
} from './reject-insurance-request-dialog';

@Component({
  selector: 'app-hospital-insurance-request-details',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './hospital-insurance-request-details.html',
  styleUrl: './hospital-insurance-request-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalInsuranceRequestDetails implements OnInit {
  private readonly insuranceRequests = inject(InsuranceRequestService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly request = signal<HospitalInsuranceRequest | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly savingNotes = signal(false);

  protected readonly notesForm = this.fb.group({
    hospitalNotes: ['', [Validators.maxLength(2000)]],
  });

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

  protected canEditNotes(request: HospitalInsuranceRequest): boolean {
    return request.statusName === 'Pending' || request.statusName === 'UnderReview';
  }

  protected saveNotes(): void {
    const current = this.request();
    if (!current || this.notesForm.invalid) {
      this.notesForm.markAllAsTouched();
      return;
    }

    this.savingNotes.set(true);

    this.insuranceRequests
      .updateHospitalNotes(current.insuranceRequestId, {
        hospitalNotes: this.notesForm.getRawValue().hospitalNotes.trim() || null,
      })
      .subscribe({
        next: (response) => {
          this.savingNotes.set(false);
          this.request.set(response.data!);
          this.notify.success(response.message);
        },
        error: (error: unknown) => {
          this.savingNotes.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not save notes.'));
        },
      });
  }

  protected startReview(): void {
    const current = this.request();
    if (!current) {
      return;
    }

    this.busy.set(true);

    this.insuranceRequests.startReview(current.insuranceRequestId).subscribe({
      next: (response) => {
        this.busy.set(false);
        this.request.set(response.data!);
        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.notify.error(friendlyMessageOf(error, 'Could not start review.'));
      },
    });
  }

  protected approve(): void {
    const current = this.request();
    if (!current) {
      return;
    }

    const data: ApproveInsuranceRequestDialogData = {
      requestId: current.insuranceRequestId,
      requestedAmount: current.requestedAmount,
    };

    this.dialog
      .open(ApproveInsuranceRequestDialog, { data })
      .afterClosed()
      .subscribe((response) => {
        if (!response) {
          return;
        }

        this.request.set(response.data!);
        this.notify.success(response.message);
      });
  }

  protected reject(): void {
    const current = this.request();
    if (!current) {
      return;
    }

    const data: RejectInsuranceRequestDialogData = { requestId: current.insuranceRequestId };

    this.dialog
      .open(RejectInsuranceRequestDialog, { data })
      .afterClosed()
      .subscribe((response) => {
        if (!response) {
          return;
        }

        this.request.set(response.data!);
        this.notify.success(response.message);
      });
  }

  private load(): void {
    this.loading.set(true);

    this.insuranceRequests.getHospitalRequest(this.id()).subscribe({
      next: (request) => {
        this.loading.set(false);
        this.request.set(request);
        this.notesForm.patchValue({ hospitalNotes: request.hospitalNotes ?? '' });
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this insurance request.'));
      },
    });
  }
}
