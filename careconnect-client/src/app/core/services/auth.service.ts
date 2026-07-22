import { HttpClient, HttpContext, HttpContextToken } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, of, tap, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  AuthResponse,
  ChangePasswordRequest,
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
} from '../models/auth.model';
import { homeRouteForRole, User } from '../models/user.model';
import { TokenService } from './token.service';

/**
 * Marks the calls the JWT interceptor must leave alone: attaching an expired token to the
 * refresh call, or retrying the refresh call itself, would loop forever.
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

export function skipAuthContext(): HttpContext {
  return new HttpContext().set(SKIP_AUTH, true);
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(TokenService);
  private readonly router = inject(Router);

  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  readonly currentUser = computed(() => this.tokens.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.tokens.session() !== null);
  readonly role = computed(() => this.currentUser()?.role ?? null);

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http
      .post<ApiResponse<RegisterResponse>>(`${this.baseUrl}/register`, request, {
        context: skipAuthContext(),
      })
      .pipe(map((response) => response.data!));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<ApiResponse<AuthResponse>>(`${this.baseUrl}/login`, request, {
        context: skipAuthContext(),
      })
      .pipe(
        map((response) => response.data!),
        tap((auth) => this.storeSession(auth)),
      );
  }

  /** Called by the interceptor when an access token comes back rejected. */
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.tokens.refreshToken;

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token is available.'));
    }

    return this.http
      .post<ApiResponse<AuthResponse>>(
        `${this.baseUrl}/refresh-token`,
        { refreshToken },
        { context: skipAuthContext() },
      )
      .pipe(
        map((response) => response.data!),
        tap((auth) => this.storeSession(auth)),
      );
  }

  me(): Observable<User> {
    return this.http.get<ApiResponse<User>>(`${this.baseUrl}/me`).pipe(
      map((response) => response.data!),
      tap((user) => this.tokens.updateUser(user)),
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<string> {
    return this.http
      .post<ApiResponse<boolean>>(`${this.baseUrl}/change-password`, request)
      .pipe(map((response) => response.message));
  }

  /**
   * Tells the API to revoke the refresh token, then clears local state either way: a failed
   * network call must never leave the user apparently signed in.
   */
  logout(redirectTo: string = '/login'): void {
    const refreshToken = this.tokens.refreshToken;

    const finish = () => {
      this.tokens.clear();
      void this.router.navigateByUrl(redirectTo);
    };

    if (!refreshToken) {
      finish();
      return;
    }

    this.http
      .post<ApiResponse<boolean>>(`${this.baseUrl}/logout`, { refreshToken })
      .pipe(catchError(() => of(null)))
      .subscribe(() => finish());
  }

  /** Clears the session without calling the API, for when the server already rejected us. */
  forceSignOut(): void {
    this.tokens.clear();
  }

  homeRoute(): string {
    return homeRouteForRole(this.role());
  }

  private storeSession(auth: AuthResponse): void {
    this.tokens.save({
      accessToken: auth.accessToken,
      refreshToken: auth.refreshToken,
      accessTokenExpiresAt: auth.accessTokenExpiresAt,
      user: auth.user,
    });
  }
}
