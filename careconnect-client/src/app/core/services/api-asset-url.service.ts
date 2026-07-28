import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

/** Resolves trusted relative asset paths returned by the API against the API origin. */
@Injectable({ providedIn: 'root' })
export class ApiAssetUrlService {
  private readonly apiOrigin = this.resolveApiOrigin();

  resolve(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    // Existing role-profile fields may still hold legitimate absolute HTTP(S) URLs.
    if (/^https?:\/\//i.test(value)) {
      return value;
    }

    if (!value.startsWith('/')) {
      return null;
    }

    return new URL(value, this.apiOrigin).toString();
  }

  private resolveApiOrigin(): string {
    const browserOrigin = globalThis.location?.origin ?? 'http://localhost';
    return new URL(environment.apiBaseUrl, browserOrigin).origin;
  }
}
