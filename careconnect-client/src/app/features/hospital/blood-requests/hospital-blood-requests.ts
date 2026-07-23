import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { BLOOD_GROUPS } from '../../../core/models/blood-group.model';
import {
  BLOOD_REQUEST_STATUSES,
  BLOOD_REQUEST_URGENCIES,
  BloodRequestStatus,
  BloodRequestUrgency,
  HospitalBloodRequest,
} from '../../../core/models/blood-request.model';
import { BloodRequestService } from '../../../core/services/blood-request.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-hospital-blood-requests',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    MatPaginatorModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './hospital-blood-requests.html',
  styleUrl: './hospital-blood-requests.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalBloodRequests implements OnInit {
  private readonly bloodRequests = inject(BloodRequestService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayedColumns = ['patient', 'beneficiary', 'bloodGroup', 'units', 'urgency', 'status'];
  protected readonly statuses = BLOOD_REQUEST_STATUSES;
  protected readonly urgencies = BLOOD_REQUEST_URGENCIES;
  protected readonly bloodGroups = BLOOD_GROUPS;

  protected readonly patientSearchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly beneficiarySearchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<BloodRequestStatus | ''>('', { nonNullable: true });
  protected readonly bloodGroupControl = new FormControl<string>('', { nonNullable: true });
  protected readonly urgencyControl = new FormControl<BloodRequestUrgency | ''>('', { nonNullable: true });

  protected readonly requests = signal<HospitalBloodRequest[]>([]);
  protected readonly loading = signal(true);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(15);

  ngOnInit(): void {
    this.patientSearchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.beneficiarySearchControl.valueChanges
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

  protected viewDetails(request: HospitalBloodRequest): void {
    void this.router.navigate(['/dashboard/hospital/blood-requests', request.bloodRequestId]);
  }

  private load(): void {
    this.loading.set(true);

    this.bloodRequests
      .getHospitalRequests({
        status: this.statusControl.value || null,
        bloodGroup: (this.bloodGroupControl.value as never) || null,
        urgency: this.urgencyControl.value || null,
        patientName: this.patientSearchControl.value,
        beneficiaryName: this.beneficiarySearchControl.value,
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
          this.notify.error(friendlyMessageOf(error, 'Could not load blood requests.'));
        },
      });
  }
}
