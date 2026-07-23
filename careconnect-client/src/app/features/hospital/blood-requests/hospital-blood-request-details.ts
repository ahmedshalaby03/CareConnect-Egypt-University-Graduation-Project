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
import { BloodRequestStatus, HospitalBloodRequest } from '../../../core/models/blood-request.model';
import { BloodRequestService } from '../../../core/services/blood-request.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { ApproveBloodRequestDialog, ApproveBloodRequestDialogData } from './approve-blood-request-dialog';
import { RejectBloodRequestDialog, RejectBloodRequestDialogData } from './reject-blood-request-dialog';

@Component({
  selector: 'app-hospital-blood-request-details',
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
  templateUrl: './hospital-blood-request-details.html',
  styleUrl: './hospital-blood-request-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalBloodRequestDetails implements OnInit {
  private readonly bloodRequests = inject(BloodRequestService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly request = signal<HospitalBloodRequest | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly savingNotes = signal(false);

  protected readonly notesForm = this.fb.group({
    hospitalNotes: ['', [Validators.maxLength(1000)]],
  });

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

  protected canEditNotes(request: HospitalBloodRequest): boolean {
    return request.statusName === 'Pending' || request.statusName === 'Approved';
  }

  protected saveNotes(): void {
    const current = this.request();
    if (!current || this.notesForm.invalid) {
      this.notesForm.markAllAsTouched();
      return;
    }

    this.savingNotes.set(true);

    this.bloodRequests
      .updateHospitalNotes(current.bloodRequestId, {
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

  protected approve(): void {
    const current = this.request();
    if (!current) {
      return;
    }

    const data: ApproveBloodRequestDialogData = {
      requestId: current.bloodRequestId,
      bloodGroupDisplayName: current.bloodGroupDisplayName,
      unitsRequested: current.unitsRequested,
      currentAvailableUnits: current.currentAvailableUnits,
    };

    this.dialog
      .open(ApproveBloodRequestDialog, { data })
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

    const data: RejectBloodRequestDialogData = { requestId: current.bloodRequestId };

    this.dialog
      .open(RejectBloodRequestDialog, { data })
      .afterClosed()
      .subscribe((response) => {
        if (!response) {
          return;
        }

        this.request.set(response.data!);
        this.notify.success(response.message);
      });
  }

  protected fulfill(): void {
    const current = this.request();
    if (!current) {
      return;
    }

    const data: ConfirmDialogData = {
      title: 'Mark as fulfilled?',
      message: `Confirm the ${current.bloodGroupDisplayName} units for ${current.beneficiaryName} have been handed off.`,
      confirmLabel: 'Mark fulfilled',
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.busy.set(true);

        this.bloodRequests.fulfill(current.bloodRequestId).subscribe({
          next: (response) => {
            this.busy.set(false);
            this.request.set(response.data!);
            this.notify.success(response.message);
          },
          error: (error: unknown) => {
            this.busy.set(false);
            this.notify.error(friendlyMessageOf(error, 'Could not mark this request as fulfilled.'));
          },
        });
      });
  }

  private load(): void {
    this.loading.set(true);

    this.bloodRequests.getHospitalRequest(this.id()).subscribe({
      next: (request) => {
        this.loading.set(false);
        this.request.set(request);
        this.notesForm.patchValue({ hospitalNotes: request.hospitalNotes ?? '' });
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this blood request.'));
      },
    });
  }
}
