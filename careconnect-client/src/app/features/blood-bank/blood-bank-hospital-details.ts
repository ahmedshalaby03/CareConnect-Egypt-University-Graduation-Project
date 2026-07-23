import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../core/interceptors/error.interceptor';
import { HospitalBloodBankDetails } from '../../core/models/blood-bank.model';
import { AuthService } from '../../core/services/auth.service';
import { BloodBankService } from '../../core/services/blood-bank.service';

@Component({
  selector: 'app-blood-bank-hospital-details',
  imports: [RouterLink, DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './blood-bank-hospital-details.html',
  styleUrl: './blood-bank-hospital-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BloodBankHospitalDetails implements OnInit {
  private readonly bloodBank = inject(BloodBankService);
  private readonly auth = inject(AuthService);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly isPatient = computed(() => this.auth.role() === 'Patient');

  protected readonly hospital = signal<HospitalBloodBankDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    this.loading.set(true);

    this.bloodBank.getHospitalBloodBank(this.id()).subscribe({
      next: (hospital) => {
        this.loading.set(false);
        this.hospital.set(hospital);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this hospital.'));
      },
    });
  }
}
