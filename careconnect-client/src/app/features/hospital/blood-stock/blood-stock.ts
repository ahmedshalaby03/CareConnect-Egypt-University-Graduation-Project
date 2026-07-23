import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { BLOOD_GROUP_LABELS, BLOOD_GROUPS, BloodGroup } from '../../../core/models/blood-group.model';
import { BloodStock } from '../../../core/models/blood-stock.model';
import { BloodStockService } from '../../../core/services/blood-stock.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AdjustBloodStockDialog, AdjustBloodStockDialogData } from './adjust-blood-stock-dialog';
import { BloodStockFormDialog, BloodStockFormDialogData } from './blood-stock-form-dialog';

@Component({
  selector: 'app-hospital-blood-stock',
  imports: [DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './blood-stock.html',
  styleUrl: './blood-stock.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalBloodStock implements OnInit {
  private readonly bloodStock = inject(BloodStockService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly bloodGroups = BLOOD_GROUPS;
  protected readonly labels = BLOOD_GROUP_LABELS;

  protected readonly loading = signal(true);
  protected readonly stockByGroup = signal<Map<BloodGroup, BloodStock>>(new Map());

  ngOnInit(): void {
    this.load();
  }

  protected stockFor(bloodGroup: BloodGroup): BloodStock | null {
    return this.stockByGroup().get(bloodGroup) ?? null;
  }

  protected addRecord(bloodGroup: BloodGroup): void {
    const data: BloodStockFormDialogData = {
      bloodGroup,
      bloodGroupDisplayName: this.labels[bloodGroup],
      stock: null,
    };

    this.dialog
      .open<BloodStockFormDialog, BloodStockFormDialogData, BloodStock>(BloodStockFormDialog, { data })
      .afterClosed()
      .subscribe((stock) => {
        if (stock) {
          this.upsert(stock);
          this.notify.success(`${stock.bloodGroupDisplayName} stock record added.`);
        }
      });
  }

  protected edit(stock: BloodStock): void {
    const data: BloodStockFormDialogData = {
      bloodGroup: stock.bloodGroup,
      bloodGroupDisplayName: stock.bloodGroupDisplayName,
      stock,
    };

    this.dialog
      .open<BloodStockFormDialog, BloodStockFormDialogData, BloodStock>(BloodStockFormDialog, { data })
      .afterClosed()
      .subscribe((updated) => {
        if (updated) {
          this.upsert(updated);
          this.notify.success(`${updated.bloodGroupDisplayName} stock updated.`);
        }
      });
  }

  protected adjust(stock: BloodStock, direction: 'increase' | 'decrease'): void {
    const data: AdjustBloodStockDialogData = { stock, direction };

    this.dialog
      .open<AdjustBloodStockDialog, AdjustBloodStockDialogData, BloodStock>(AdjustBloodStockDialog, { data })
      .afterClosed()
      .subscribe((updated) => {
        if (updated) {
          this.upsert(updated);
          this.notify.success(
            direction === 'increase'
              ? `${updated.bloodGroupDisplayName} stock increased.`
              : `${updated.bloodGroupDisplayName} stock decreased.`,
          );
        }
      });
  }

  private upsert(stock: BloodStock): void {
    const next = new Map(this.stockByGroup());
    next.set(stock.bloodGroup, stock);
    this.stockByGroup.set(next);
  }

  private load(): void {
    this.loading.set(true);

    this.bloodStock.getHospitalStock({}).subscribe({
      next: (items) => {
        this.loading.set(false);
        this.stockByGroup.set(new Map(items.map((item) => [item.bloodGroup, item])));
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.notify.error(friendlyMessageOf(error, 'Could not load blood stock.'));
      },
    });
  }
}
