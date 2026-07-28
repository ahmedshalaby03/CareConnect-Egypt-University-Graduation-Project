import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AccountProfile, UpdateAccountProfileRequest } from '../models/account.model';
import { ApiResponse } from '../models/api-response.model';
import { ApiAssetUrlService } from './api-asset-url.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class AccountSettingsService {
  private readonly http = inject(HttpClient);
  private readonly assets = inject(ApiAssetUrlService);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiBaseUrl}/account`;

  getProfile(): Observable<AccountProfile> {
    return this.http.get<ApiResponse<AccountProfile>>(`${this.baseUrl}/profile`).pipe(
      map((response) => this.normalize(response.data!)),
      tap((profile) => this.syncCurrentUser(profile)),
    );
  }

  updateProfile(request: UpdateAccountProfileRequest): Observable<ApiResponse<AccountProfile>> {
    return this.http
      .put<ApiResponse<AccountProfile>>(`${this.baseUrl}/profile`, request)
      .pipe(
        map((response) => ({
          ...response,
          data: this.normalize(response.data!),
        })),
        tap((response) => this.syncCurrentUser(response.data!)),
      );
  }

  uploadProfileImage(file: File): Observable<ApiResponse<AccountProfile>> {
    const body = new FormData();
    body.append('image', file);

    return this.http
      .post<ApiResponse<AccountProfile>>(`${this.baseUrl}/profile-image`, body)
      .pipe(
        map((response) => ({
          ...response,
          data: this.normalize(response.data!),
        })),
        tap((response) => this.syncCurrentUser(response.data!)),
      );
  }

  deleteProfileImage(): Observable<ApiResponse<AccountProfile>> {
    return this.http
      .delete<ApiResponse<AccountProfile>>(`${this.baseUrl}/profile-image`)
      .pipe(
        map((response) => ({
          ...response,
          data: this.normalize(response.data!),
        })),
        tap((response) => this.syncCurrentUser(response.data!)),
      );
  }

  private normalize(profile: AccountProfile): AccountProfile {
    return {
      ...profile,
      profileImageUrl: this.assets.resolve(profile.profileImageUrl),
    };
  }

  private syncCurrentUser(profile: AccountProfile): void {
    const current = this.auth.currentUser();
    if (!current) {
      return;
    }

    this.auth.updateCurrentUser({
      ...current,
      fullName: profile.fullName,
      phoneNumber: profile.phoneNumber,
      isActive: profile.isActive,
      profileImageUrl: profile.profileImageUrl,
    });
  }
}
