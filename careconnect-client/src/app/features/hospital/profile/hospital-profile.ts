import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { EGYPT_GOVERNORATES } from '../../../core/models/directory.model';
import { HospitalProfile } from '../../../core/models/hospital.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { AuthService } from '../../../core/services/auth.service';
import { HospitalProfileService } from '../../../core/services/hospital-profile.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';
import { ProfileCompletion } from '../../../shared/profile-completion/profile-completion';

@Component({
  selector: 'app-hospital-profile',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    ProfileCompletion,
  ],
  templateUrl: './hospital-profile.html',
  styleUrl: './hospital-profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalProfilePage implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly profiles = inject(HospitalProfileService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly auth = inject(AuthService);

  protected readonly governorates = EGYPT_GOVERNORATES;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly savingSpecialties = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly profile = signal<HospitalProfile | null>(null);

  protected readonly form = this.fb.group({
    hospitalName: ['', [Validators.required, Validators.maxLength(200)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[0-9][0-9\s-]{6,19}$/)]],
    address: ['', [Validators.required, Validators.maxLength(400)]],
    governorate: ['', [Validators.required]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(2000)]],
    logoUrl: ['', [Validators.maxLength(500)]],
    websiteUrl: ['', [Validators.maxLength(500)]],
    openingTime: ['', [Validators.pattern(/^([01]\d|2[0-3]):[0-5]\d$/)]],
    closingTime: ['', [Validators.pattern(/^([01]\d|2[0-3]):[0-5]\d$/)]],
  });

  /** Separate control: specialties are saved through their own endpoint and transaction. */
  protected readonly selectedSpecialtyIds = signal<string[]>([]);

  ngOnInit(): void {
    forkJoin({
      specialties: this.specialtyService.getActive(),
      profile: this.profiles.get(),
    }).subscribe({
      next: ({ specialties, profile }) => {
        this.specialties.set(specialties);
        this.applyProfile(profile);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load your hospital profile.'));
      },
    });
  }

  protected onSpecialtiesChange(ids: string[]): void {
    this.selectedSpecialtyIds.set(ids);
  }

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notify.error('Please fix the highlighted fields before saving.');
      return;
    }

    const raw = this.form.getRawValue();
    this.saving.set(true);

    this.profiles
      .update({
        fullName: null,
        hospitalName: raw.hospitalName.trim() || null,
        address: raw.address.trim() || null,
        governorate: raw.governorate || null,
        city: raw.city.trim() || null,
        latitude: this.profile()?.latitude ?? null,
        longitude: this.profile()?.longitude ?? null,
        phoneNumber: raw.phoneNumber.trim() || null,
        description: raw.description.trim() || null,
        logoUrl: raw.logoUrl.trim() || null,
        websiteUrl: raw.websiteUrl.trim() || null,
        openingTime: raw.openingTime || null,
        closingTime: raw.closingTime || null,
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.applyProfile(response.data!);
          this.notify.success(response.message);
          this.auth.me().subscribe({ error: () => undefined });
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.serverError.set(friendlyMessageOf(error, 'Could not save your hospital profile.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }

  protected saveSpecialties(): void {
    this.savingSpecialties.set(true);

    this.profiles.updateSpecialties(this.selectedSpecialtyIds()).subscribe({
      next: (response) => {
        this.savingSpecialties.set(false);
        this.applyProfile(response.data!);
        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.savingSpecialties.set(false);
        this.notify.error(friendlyMessageOf(error, 'Could not update your specialties.'));
      },
    });
  }

  private applyProfile(profile: HospitalProfile): void {
    this.profile.set(profile);
    this.selectedSpecialtyIds.set(profile.specialties.map((s) => s.id));

    this.form.patchValue({
      hospitalName: profile.hospitalName ?? '',
      phoneNumber: profile.phoneNumber ?? '',
      address: profile.address ?? '',
      governorate: profile.governorate ?? '',
      city: profile.city ?? '',
      description: profile.description ?? '',
      logoUrl: profile.logoUrl ?? '',
      websiteUrl: profile.websiteUrl ?? '',
      openingTime: profile.openingTime ?? '',
      closingTime: profile.closingTime ?? '',
    });
  }
}
