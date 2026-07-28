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
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { MedicalServiceCategory } from '../../../core/models/medical-service-provider.model';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';
import { MedicalServiceCategoryFormDialog } from './category-form-dialog';

@Component({
  selector: 'app-super-admin-medical-service-categories',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressBarModule, MatSelectModule, MatTableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page"><header class="page-head"><div><h1>Medical service categories</h1><p>Categories organize the services published by medical service providers.</p></div><button mat-flat-button (click)="openForm(null)"><mat-icon>add</mat-icon>Add category</button></header>
      <div class="cc-filters filters"><mat-form-field><mat-label>Search</mat-label><input matInput [formControl]="search"/></mat-form-field><mat-form-field><mat-label>Status</mat-label><mat-select [formControl]="status" (selectionChange)="filterChanged()"><mat-option [value]="null">All</mat-option><mat-option [value]="true">Active</mat-option><mat-option [value]="false">Inactive</mat-option></mat-select></mat-form-field></div>
      @if (loading()) { <mat-progress-bar mode="indeterminate"/> } @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      <div class="table-wrap"><table mat-table [dataSource]="categories()"><ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let item"><strong>{{ item.name }}</strong><div>{{ item.description }}</div></td></ng-container><ng-container matColumnDef="usage"><th mat-header-cell *matHeaderCellDef>Services</th><td mat-cell *matCellDef="let item">{{ item.serviceUsageCount }}</td></ng-container><ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th><td mat-cell *matCellDef="let item"><span class="cc-status-chip" [class.cc-status-chip--active]="item.isActive" [class.cc-status-chip--inactive]="!item.isActive">{{ item.isActive ? 'Active' : 'Inactive' }}</span></td></ng-container><ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th><td mat-cell *matCellDef="let item"><button mat-icon-button (click)="openForm(item)" aria-label="Edit"><mat-icon>edit</mat-icon></button><button mat-button (click)="confirmStatus(item)">{{ item.isActive ? 'Deactivate' : 'Activate' }}</button></td></ng-container><tr mat-header-row *matHeaderRowDef="columns"></tr><tr mat-row *matRowDef="let row; columns: columns"></tr></table></div>
      @if (!loading() && !categories().length) { <div class="cc-empty-state">No categories match these filters.</div> }
      <mat-paginator [length]="totalCount()" [pageIndex]="pageIndex()" [pageSize]="pageSize()" [pageSizeOptions]="[10,25,50]" (page)="onPage($event)"/>
    </section>
  `,
  styles: `.page-head{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.filters{grid-template-columns:2fr 1fr;margin:20px 0}.table-wrap{overflow:auto;margin-top:18px}table{width:100%}td div{color:var(--mat-sys-on-surface-variant);font-size:.9rem;max-width:600px}@media(max-width:600px){.filters{grid-template-columns:1fr}}`,
})
export class SuperAdminMedicalServiceCategories implements OnInit {
  private readonly service = inject(MedicalServiceProviderService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly columns = ['name', 'usage', 'status', 'actions'];
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly status = new FormControl<boolean | null>(null);
  protected readonly categories = signal<MedicalServiceCategory[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);
  ngOnInit(): void { this.search.valueChanges.pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe(() => { this.pageIndex.set(0); this.load(); }); this.load(); }
  protected filterChanged(): void { this.pageIndex.set(0); this.load(); }
  protected onPage(event: PageEvent): void { this.pageIndex.set(event.pageIndex); this.pageSize.set(event.pageSize); this.load(); }
  protected openForm(item: MedicalServiceCategory | null): void { const ref = this.dialog.open(MedicalServiceCategoryFormDialog, { data: item, autoFocus: 'first-tabbable' }); ref.afterClosed().subscribe((message) => { if (message) { this.notify.success(message); this.load(); } }); }
  protected confirmStatus(item: MedicalServiceCategory): void {
    const ref = this.dialog.open(ConfirmDialog, { data: { title: item.isActive ? 'Deactivate category?' : 'Activate category?', message: item.isActive ? `${item.serviceUsageCount} existing service(s) remain linked, but providers cannot select this category for new services.` : 'Providers will be able to select this category.', destructive: item.isActive, confirmLabel: item.isActive ? 'Deactivate' : 'Activate' } });
    ref.afterClosed().subscribe((confirmed) => { if (!confirmed) return; this.service.setCategoryStatus(item.id, !item.isActive).subscribe({ next: (response) => { this.notify.success(response.message); this.load(); }, error: (error: unknown) => this.notify.error(friendlyMessageOf(error, 'Could not update category status.')) }); });
  }
  private load(): void { this.loading.set(true); this.error.set(null); this.service.getCategories({ search: this.search.value, isActive: this.status.value, page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe({ next: (result) => { this.categories.set(result.items); this.totalCount.set(result.totalCount); this.loading.set(false); }, error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load categories.')); } }); }
}
