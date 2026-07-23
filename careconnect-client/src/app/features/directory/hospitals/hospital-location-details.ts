import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { HospitalLocationDetails } from '../../../core/models/hospital-discovery.model';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { HospitalDiscoveryService } from '../../../core/services/hospital-discovery.service';
import { NotificationService } from '../../../core/services/notification.service';

/** Public location details for one hospital: address, landmark, directions link. */
@Component({
  selector: 'app-hospital-location-details',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './hospital-location-details.html',
  styleUrl: './hospital-location-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalLocationDetailsPage implements OnInit {
  private readonly discovery = inject(HospitalDiscoveryService);
  private readonly geolocation = inject(GeolocationService);
  private readonly notify = inject(NotificationService);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly location = signal<HospitalLocationDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly locatingDevice = signal(false);

  ngOnInit(): void {
    this.load(null, null);
  }

  /**
   * User-triggered only. The coordinates are used for this one request and are never
   * stored - a fresh call to the same read-only endpoint, just with two extra query params.
   */
  protected findMyDistance(): void {
    this.locatingDevice.set(true);

    this.geolocation
      .getCurrentPosition()
      .then((coords) => {
        this.locatingDevice.set(false);
        this.load(coords.latitude, coords.longitude);
      })
      .catch((error: unknown) => {
        this.locatingDevice.set(false);

        if (error instanceof GeolocationFailure) {
          const messages: Record<typeof error.reason, string> = {
            denied: 'Location permission was denied, so distance cannot be shown.',
            unavailable: 'Your location could not be determined.',
            timeout: 'Getting your location took too long. You can try again.',
          };
          this.notify.error(messages[error.reason]);
          return;
        }

        this.notify.error('Your location could not be determined.');
      });
  }

  private load(userLatitude: number | null, userLongitude: number | null): void {
    this.loading.set(true);

    this.discovery.getLocationDetails(this.id(), userLatitude, userLongitude).subscribe({
      next: (location) => {
        this.loading.set(false);
        this.location.set(location);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this hospital location.'));
      },
    });
  }
}
