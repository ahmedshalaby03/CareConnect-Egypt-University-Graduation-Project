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
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { InsuranceCompany } from '../../../core/models/insurance-company.model';
import { InsuranceCompanyService } from '../../../core/services/insurance-company.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { InsuranceCompanyFormDialog, InsuranceCompanyFormDialogData } from './insurance-company-form-dialog';

@Component({
  selector: 'app-super-admin-insurance-companies',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './insurance-companies.html',
  styleUrl: './insurance-companies.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuperAdminInsuranceCompanies implements OnInit {
  private readonly companyService = inject(InsuranceCompanyService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayedColumns = ['name', 'arabicName', 'usage', 'status', 'actions'];

  protected readonly statusFilters = [
    { label: 'All statuses', value: null },
    { label: 'Active only', value: true },
    { label: 'Inactive only', value: false },
  ];

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly statusControl = new FormControl<boolean | null>(null);

  protected readonly companies = signal<InsuranceCompany[]>([]);
  protected readonly loading = signal(false);
  protected readonly togglingId = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
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

  protected openForm(company: InsuranceCompany | null): void {
    const ref = this.dialog.open<InsuranceCompanyFormDialog, InsuranceCompanyFormDialogData, string>(
      InsuranceCompanyFormDialog,
      { data: { company }, autoFocus: 'first-tabbable' },
    );

    ref.afterClosed().subscribe((message) => {
      if (message) {
        this.notify.success(message);
        this.load();
      }
    });
  }

  /**
   * There is no delete: deactivating hides the company from the patient's request form
   * while every request already referencing it keeps working. A confirmation guards the
   * switch either way, since it changes what patients can select right away.
   */
  protected toggleStatus(company: InsuranceCompany): void {
    if (this.togglingId()) {
      return;
    }

    const data: ConfirmDialogData = company.isActive
      ? {
          title: 'Deactivate this insurance company?',
          message:
            `${company.name} will disappear from the patient's request form. ` +
            `Existing requests (${company.requestCount}) keep their reference and are unaffected.`,
          confirmLabel: 'Deactivate',
          destructive: true,
        }
      : {
          title: 'Activate this insurance company?',
          message: `${company.name} will become selectable on the patient's request form again.`,
          confirmLabel: 'Activate',
        };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.applyToggle(company);
        }
      });
  }

  private applyToggle(company: InsuranceCompany): void {
    this.togglingId.set(company.id);

    this.companyService.toggleStatus(company.id).subscribe({
      next: (response) => {
        this.togglingId.set(null);

        const updated = response.data;
        if (updated) {
          this.companies.update((list) =>
            list.map((c) => (c.id === updated.id ? { ...c, isActive: updated.isActive } : c)),
          );
        }

        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.togglingId.set(null);
        this.notify.error(friendlyMessageOf(error, 'Could not update the insurance company.'));
      },
    });
  }

  private load(): void {
    this.loading.set(true);

    this.companyService
      .getAll({
        searchTerm: this.searchControl.value,
        isActive: this.statusControl.value,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.companies.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load insurance companies.'));
        },
      });
  }
}
