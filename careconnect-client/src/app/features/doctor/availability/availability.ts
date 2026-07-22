import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { AffiliatedHospital } from '../../../core/models/affiliation.model';
import { Availability } from '../../../core/models/availability.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { DoctorAvailabilityService } from '../../../core/services/doctor-availability.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AvailabilityFormDialog, AvailabilityFormDialogData } from './availability-form-dialog';

@Component({
  selector: 'app-doctor-availability',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './availability.html',
  styleUrl: './availability.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorAvailability implements OnInit {
  private readonly availabilityService = inject(DoctorAvailabilityService);
  private readonly affiliations = inject(AffiliationService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly displayedColumns = ['day', 'hospital', 'time', 'duration', 'status', 'actions'];

  protected readonly hospitalControl = new FormControl<string>('', { nonNullable: true });

  protected readonly hospitals = signal<AffiliatedHospital[]>([]);
  protected readonly availabilities = signal<Availability[]>([]);
  protected readonly loading = signal(true);
  protected readonly togglingId = signal<string | null>(null);

  ngOnInit(): void {
    this.affiliations.getDoctorHospitals().subscribe({
      next: (hospitals) => this.hospitals.set(hospitals),
      error: () => undefined,
    });

    this.load();
  }

  protected onHospitalChange(): void {
    this.load();
  }

  protected openForm(availability: Availability | null): void {
    if (this.hospitals().length === 0) {
      this.notify.error('You need an approved hospital affiliation before you can set availability.');
      return;
    }

    const ref = this.dialog.open<AvailabilityFormDialog, AvailabilityFormDialogData, string>(
      AvailabilityFormDialog,
      { data: { availability, hospitals: this.hospitals() }, autoFocus: 'first-tabbable' },
    );

    ref.afterClosed().subscribe((message) => {
      if (message) {
        this.notify.success(message);
        this.load();
      }
    });
  }

  protected toggleStatus(availability: Availability): void {
    if (this.togglingId()) {
      return;
    }

    this.togglingId.set(availability.id);

    this.availabilityService.toggleStatus(availability.id).subscribe({
      next: (response) => {
        this.togglingId.set(null);

        const updated = response.data;
        if (updated) {
          this.availabilities.update((list) =>
            list.map((a) => (a.id === updated.id ? updated : a)),
          );
        }

        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.togglingId.set(null);
        this.notify.error(friendlyMessageOf(error, 'Could not update this schedule.'));
      },
    });
  }

  private load(): void {
    this.loading.set(true);

    this.availabilityService
      .getAll({ hospitalProfileId: this.hospitalControl.value || null })
      .subscribe({
        next: (items) => {
          this.loading.set(false);
          this.availabilities.set(items);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load your availability.'));
        },
      });
  }
}
