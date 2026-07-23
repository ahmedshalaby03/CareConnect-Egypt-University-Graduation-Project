import { DatePipe } from '@angular/common';
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
import { InsuranceCompanyOption } from '../../../core/models/insurance-company.model';
import {
  HospitalInsuranceRequest,
  INSURANCE_REQUEST_STATUSES,
  InsuranceRequestStatus,
} from '../../../core/models/insurance-request.model';
import { InsuranceCompanyService } from '../../../core/services/insurance-company.service';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-hospital-insurance-requests',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    MatPaginatorModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './hospital-insurance-requests.html',
  styleUrl: './hospital-insurance-requests.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalInsuranceRequests implements OnInit {
  private readonly insuranceRequests = inject(InsuranceRequestService);
  private readonly insuranceCompanies = inject(InsuranceCompanyService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayedColumns = ['patient', 'doctor', 'company', 'when', 'status'];
  protected readonly statuses = INSURANCE_REQUEST_STATUSES;

  protected readonly patientSearchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly doctorSearchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<InsuranceRequestStatus | ''>('', { nonNullable: true });
  protected readonly companyControl = new FormControl<string>('', { nonNullable: true });

  protected readonly companies = signal<InsuranceCompanyOption[]>([]);
  protected readonly requests = signal<HospitalInsuranceRequest[]>([]);
  protected readonly loading = signal(true);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(15);

  ngOnInit(): void {
    this.insuranceCompanies.getActive().subscribe({
      next: (items) => this.companies.set(items),
      error: () => undefined,
    });

    this.patientSearchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.doctorSearchControl.valueChanges
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

  protected viewDetails(request: HospitalInsuranceRequest): void {
    void this.router.navigate(['/dashboard/hospital/insurance-requests', request.insuranceRequestId]);
  }

  private load(): void {
    this.loading.set(true);

    this.insuranceRequests
      .getHospitalRequests({
        status: this.statusControl.value || null,
        insuranceCompanyId: this.companyControl.value || null,
        patientName: this.patientSearchControl.value,
        doctorName: this.doctorSearchControl.value,
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
          this.notify.error(friendlyMessageOf(error, 'Could not load insurance requests.'));
        },
      });
  }
}
