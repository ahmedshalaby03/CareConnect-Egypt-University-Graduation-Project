import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { EGYPT_GOVERNORATES } from '../../../core/models/directory.model';
import { HospitalLocation } from '../../../core/models/hospital-location.model';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { HospitalLocationService } from '../../../core/services/hospital-location.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-hospital-location',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './hospital-location.html',
  styleUrl: './hospital-location.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalLocationPage implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly locations = inject(HospitalLocationService);
  private readonly geolocation = inject(GeolocationService);
  private readonly notify = inject(NotificationService);

  protected readonly governorates = EGYPT_GOVERNORATES;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly locatingDevice = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);
  protected readonly location = signal<HospitalLocation | null>(null);

  protected readonly form = this.fb.group({
    address: ['', [Validators.required, Validators.maxLength(400)]],
    governorate: ['', [Validators.required]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    locationDescription: ['', [Validators.maxLength(500)]],
    nearbyLandmark: ['', [Validators.maxLength(200)]],
    latitude: [null as number | null, [Validators.min(-90), Validators.max(90)]],
    longitude: [null as number | null, [Validators.min(-180), Validators.max(180)]],
  });

  ngOnInit(): void {
    this.locations.get().subscribe({
      next: (location) => {
        this.loading.set(false);
        this.applyLocation(location);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load your hospital location.'));
      },
    });
  }

  /**
   * Only ever called from a direct button click. Fills the coordinate fields for the
   * hospital to review - it never submits the form itself, and nothing here keeps watching
   * the device afterwards.
   */
  protected useCurrentLocation(): void {
    this.locatingDevice.set(true);

    this.geolocation
      .getCurrentPosition()
      .then((coords) => {
        this.locatingDevice.set(false);
        this.form.patchValue({ latitude: coords.latitude, longitude: coords.longitude });
        this.notify.success('Coordinates filled in from your device. Review them, then save.');
      })
      .catch((error: unknown) => {
        this.locatingDevice.set(false);

        if (error instanceof GeolocationFailure) {
          const messages: Record<typeof error.reason, string> = {
            denied: 'Location permission was denied. You can still enter coordinates manually.',
            unavailable: 'Your location could not be determined. You can enter coordinates manually.',
            timeout: 'Getting your location took too long. You can try again or enter coordinates manually.',
          };
          this.notify.error(messages[error.reason]);
          return;
        }

        this.notify.error('Your location could not be determined.');
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

    if ((raw.latitude === null) !== (raw.longitude === null)) {
      this.notify.error('Latitude and longitude must both be set, or both left empty.');
      return;
    }

    this.saving.set(true);

    this.locations
      .update({
        address: raw.address.trim(),
        governorate: raw.governorate,
        city: raw.city.trim(),
        locationDescription: raw.locationDescription.trim() || null,
        nearbyLandmark: raw.nearbyLandmark.trim() || null,
        latitude: raw.latitude,
        longitude: raw.longitude,
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.applyLocation(response.data!);
          this.notify.success(response.message);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.serverError.set(friendlyMessageOf(error, 'Could not save your hospital location.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }

  private applyLocation(location: HospitalLocation): void {
    this.location.set(location);

    this.form.patchValue({
      address: location.address ?? '',
      governorate: location.governorate ?? '',
      city: location.city ?? '',
      locationDescription: location.locationDescription ?? '',
      nearbyLandmark: location.nearbyLandmark ?? '',
      latitude: location.latitude,
      longitude: location.longitude,
    });
  }
}
