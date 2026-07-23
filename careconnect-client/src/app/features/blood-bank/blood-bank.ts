import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../core/interceptors/error.interceptor';
import { BLOOD_GROUPS } from '../../core/models/blood-group.model';
import { BloodAvailability } from '../../core/models/blood-bank.model';
import { EGYPT_GOVERNORATES } from '../../core/models/directory.model';
import { AuthService } from '../../core/services/auth.service';
import { BloodBankService } from '../../core/services/blood-bank.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-blood-bank',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
  ],
  templateUrl: './blood-bank.html',
  styleUrl: './blood-bank.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BloodBank implements OnInit {
  private readonly bloodBank = inject(BloodBankService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);

  protected readonly bloodGroups = BLOOD_GROUPS;
  protected readonly governorates = EGYPT_GOVERNORATES;
  protected readonly isPatient = computed(() => this.auth.role() === 'Patient');

  protected readonly bloodGroupControl = new FormControl<string>('', { nonNullable: true });
  protected readonly governorateControl = new FormControl<string>('', { nonNullable: true });
  protected readonly cityControl = new FormControl<string>('', { nonNullable: true });
  protected readonly hospitalNameControl = new FormControl<string>('', { nonNullable: true });
  protected readonly availableOnlyControl = new FormControl<boolean>(true, { nonNullable: true });

  protected readonly results = signal<BloodAvailability[]>([]);
  protected readonly loading = signal(true);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(12);

  ngOnInit(): void {
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

  private load(): void {
    this.loading.set(true);

    this.bloodBank
      .searchAvailability({
        bloodGroup: (this.bloodGroupControl.value as never) || null,
        governorate: this.governorateControl.value || null,
        city: this.cityControl.value || null,
        hospitalName: this.hospitalNameControl.value || null,
        availableOnly: this.availableOnlyControl.value,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.results.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load blood availability.'));
        },
      });
  }
}
