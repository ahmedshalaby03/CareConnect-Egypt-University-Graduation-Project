import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  AFFILIATION_STATUSES,
  AffiliationStatus,
  HospitalDoctorRequest,
} from '../../../core/models/affiliation.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { RejectDialog, RejectDialogData } from '../../../shared/reject-dialog/reject-dialog';

@Component({
  selector: 'app-hospital-doctor-requests',
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
  ],
  templateUrl: './hospital-doctor-requests.html',
  styleUrl: './hospital-doctor-requests.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalDoctorRequests implements OnInit {
  private readonly affiliations = inject(AffiliationService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly statuses = AFFILIATION_STATUSES;

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<AffiliationStatus | ''>('Pending', {
    nonNullable: true,
  });
  protected readonly specialtyControl = new FormControl<string>('', { nonNullable: true });

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly requests = signal<HospitalDoctorRequest[]>([]);
  protected readonly loading = signal(true);
  protected readonly busyId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
    this.specialtyService.getActive().subscribe({
      next: (items) => this.specialties.set(items),
      error: () => undefined,
    });

    this.searchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
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

  protected approve(request: HospitalDoctorRequest): void {
    const data: ConfirmDialogData = {
      title: 'Approve this doctor?',
      message: `${request.doctorName} will be added to your hospital's medical team and shown on your public page.`,
      confirmLabel: 'Approve',
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.busyId.set(request.id);

        this.affiliations.approveRequest(request.id).subscribe({
          next: (response) => {
            this.busyId.set(null);
            this.notify.success(response.message);
            this.load();
          },
          error: (error: unknown) => {
            this.busyId.set(null);
            this.notify.error(friendlyMessageOf(error, 'Could not approve the request.'));
          },
        });
      });
  }

  /** Rejection always needs a reason; the dialog collects it and the API enforces it too. */
  protected reject(request: HospitalDoctorRequest): void {
    this.dialog
      .open<RejectDialog, RejectDialogData, string>(RejectDialog, {
        data: { doctorName: request.doctorName },
      })
      .afterClosed()
      .subscribe((reason) => {
        if (!reason) {
          return;
        }

        this.busyId.set(request.id);

        this.affiliations.rejectRequest(request.id, reason).subscribe({
          next: (response) => {
            this.busyId.set(null);
            this.notify.success(response.message);
            this.load();
          },
          error: (error: unknown) => {
            this.busyId.set(null);
            this.notify.error(friendlyMessageOf(error, 'Could not reject the request.'));
          },
        });
      });
  }

  private load(): void {
    this.loading.set(true);

    this.affiliations
      .getHospitalRequests({
        status: this.statusControl.value || null,
        search: this.searchControl.value,
        specialtyId: this.specialtyControl.value || null,
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
          this.notify.error(friendlyMessageOf(error, 'Could not load doctor requests.'));
        },
      });
  }
}
