import { Injectable } from '@angular/core';

export type GeolocationFailureReason = 'denied' | 'unavailable' | 'timeout';

export interface GeolocationCoordinates {
  latitude: number;
  longitude: number;
}

export class GeolocationFailure extends Error {
  constructor(
    public readonly reason: GeolocationFailureReason,
    message: string,
  ) {
    super(message);
  }
}

/**
 * A single one-shot browser location read - never a watch, never called on app startup.
 * Every caller triggers this only in direct response to the user clicking a button, and the
 * coordinates it resolves are used for one search request and never persisted here.
 */
@Injectable({ providedIn: 'root' })
export class GeolocationService {
  getCurrentPosition(): Promise<GeolocationCoordinates> {
    return new Promise((resolve, reject) => {
      if (!('geolocation' in navigator)) {
        reject(new GeolocationFailure('unavailable', 'This browser cannot provide your location.'));
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position) => {
          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          });
        },
        (error) => {
          if (error.code === error.PERMISSION_DENIED) {
            reject(new GeolocationFailure('denied', 'Location permission was denied.'));
          } else if (error.code === error.TIMEOUT) {
            reject(new GeolocationFailure('timeout', 'Getting your location took too long.'));
          } else {
            reject(new GeolocationFailure('unavailable', 'Your location could not be determined.'));
          }
        },
        { enableHighAccuracy: false, timeout: 10_000, maximumAge: 0 },
      );
    });
  }
}
