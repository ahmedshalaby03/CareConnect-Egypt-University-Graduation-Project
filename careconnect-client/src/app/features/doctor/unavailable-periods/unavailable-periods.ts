import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { AffiliatedHospital } from '../../../core/models/affiliation.model';
import { UnavailablePeriod } from '../../../core/models/unavailable-period.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { DoctorUnavailablePeriodService } from '../../../core/services/doctor-unavailable-period.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import {
  UnavailablePeriodFormDialog,
  UnavailablePeriodFormDialogData,
} from './unavailable-period-form-dialog';

@Component({
  selector: 'app-doctor-unavailable-periods',
  imports: [DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './unavailable-periods.html',
  styleUrl: './unavailable-periods.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorUnavailablePeriods implements OnInit {
  private readonly periodsService = inject(DoctorUnavailablePeriodService);
  private readonly affiliations = inject(AffiliationService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly hospitals = signal<AffiliatedHospital[]>([]);
  protected readonly periods = signal<UnavailablePeriod[]>([]);
  protected readonly loading = signal(true);
  protected readonly deletingId = signal<string | null>(null);

  ngOnInit(): void {
    this.affiliations.getDoctorHospitals().subscribe({
      next: (hospitals) => this.hospitals.set(hospitals),
      error: () => undefined,
    });

    this.load();
  }

  protected isFuture(period: UnavailablePeriod): boolean {
    return new Date(period.startDateTime).getTime() > Date.now();
  }

  protected openForm(): void {
    if (this.hospitals().length === 0) {
      this.notify.error('You need an approved hospital affiliation before you can add an unavailable period.');
      return;
    }

    const ref = this.dialog.open<
      UnavailablePeriodFormDialog,
      UnavailablePeriodFormDialogData,
      string
    >(UnavailablePeriodFormDialog, {
      data: { hospitals: this.hospitals() },
      autoFocus: 'first-tabbable',
    });

    ref.afterClosed().subscribe((message) => {
      if (message) {
        this.notify.success(message);
        this.load();
      }
    });
  }

  protected delete(period: UnavailablePeriod): void {
    const data: ConfirmDialogData = {
      title: 'Delete this period?',
      message: 'This removes the unavailable period. Any slots it was blocking become bookable again.',
      confirmLabel: 'Delete',
      destructive: true,
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.deletingId.set(period.id);

        this.periodsService.delete(period.id).subscribe({
          next: (response) => {
            this.deletingId.set(null);
            this.notify.success(response.message);
            this.load();
          },
          error: (error: unknown) => {
            this.deletingId.set(null);
            this.notify.error(friendlyMessageOf(error, 'Could not delete this period.'));
          },
        });
      });
  }

  private load(): void {
    this.loading.set(true);

    this.periodsService.getAll({}).subscribe({
      next: (items) => {
        this.loading.set(false);
        this.periods.set(items);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.notify.error(friendlyMessageOf(error, 'Could not load your unavailable periods.'));
      },
    });
  }
}
