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
import { DoctorProfile } from '../../../core/models/doctor.model';
import { EGYPT_GOVERNORATES } from '../../../core/models/directory.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { AuthService } from '../../../core/services/auth.service';
import { DoctorProfileService } from '../../../core/services/doctor-profile.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';
import { ProfileCompletion } from '../../../shared/profile-completion/profile-completion';

@Component({
  selector: 'app-doctor-profile',
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
  templateUrl: './doctor-profile.html',
  styleUrl: './doctor-profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorProfilePage implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly profiles = inject(DoctorProfileService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly auth = inject(AuthService);

  protected readonly governorates = EGYPT_GOVERNORATES;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly profile = signal<DoctorProfile | null>(null);

  protected readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    phoneNumber: ['', [Validators.pattern(/^\+?[0-9][0-9\s-]{6,19}$/)]],
    specialtyId: ['', [Validators.required]],
    licenseNumber: ['', [Validators.required, Validators.maxLength(100)]],
    yearsOfExperience: [null as number | null, [Validators.required, Validators.min(0), Validators.max(70)]],
    consultationPrice: [null as number | null, [Validators.min(0)]],
    biography: ['', [Validators.maxLength(2000)]],
    address: ['', [Validators.maxLength(400)]],
    governorate: ['', [Validators.required]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    profileImageUrl: ['', [Validators.maxLength(500)]],
  });

  ngOnInit(): void {
    // Both are needed before the form can be rendered meaningfully, so they load together.
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
        this.loadError.set(friendlyMessageOf(error, 'Could not load your profile.'));
      },
    });
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
        fullName: raw.fullName.trim(),
        phoneNumber: raw.phoneNumber.trim() || null,
        specialtyId: raw.specialtyId || null,
        licenseNumber: raw.licenseNumber.trim() || null,
        yearsOfExperience: raw.yearsOfExperience,
        biography: raw.biography.trim() || null,
        consultationPrice: raw.consultationPrice,
        address: raw.address.trim() || null,
        governorate: raw.governorate || null,
        city: raw.city.trim() || null,
        profileImageUrl: raw.profileImageUrl.trim() || null,
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.applyProfile(response.data!);
          this.notify.success(response.message);

          // The header shows the account name, so refresh it after a rename.
          this.auth.me().subscribe({ error: () => undefined });
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.serverError.set(friendlyMessageOf(error, 'Could not save your profile.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }

  private applyProfile(profile: DoctorProfile): void {
    this.profile.set(profile);

    this.form.patchValue({
      fullName: profile.fullName,
      phoneNumber: profile.phoneNumber ?? '',
      specialtyId: profile.specialty?.id ?? '',
      licenseNumber: profile.licenseNumber ?? '',
      yearsOfExperience: profile.yearsOfExperience,
      consultationPrice: profile.consultationPrice,
      biography: profile.biography ?? '',
      address: profile.address ?? '',
      governorate: profile.governorate ?? '',
      city: profile.city ?? '',
      profileImageUrl: profile.profileImageUrl ?? '',
    });
  }
}
