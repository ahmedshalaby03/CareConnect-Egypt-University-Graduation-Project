import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService, SKIP_AUTH } from '../services/auth.service';
import { TokenService } from '../services/token.service';

/**
 * Shared across every request so that a burst of parallel 401s triggers one refresh call.
 * Emits the new access token when the refresh completes, or null while one is in flight.
 */
let refreshInProgress = false;
const refreshedToken$ = new BehaviorSubject<string | null>(null);

export const jwtInterceptor: HttpInterceptorFn = (request, next) => {
  const tokens = inject(TokenService);
  const auth = inject(AuthService);
  const router = inject(Router);

  // Login, register and refresh must go out unauthenticated.
  if (request.context.get(SKIP_AUTH)) {
    return next(request);
  }

  const accessToken = tokens.accessToken;
  const authorized = accessToken ? withBearer(request, accessToken) : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      const isAuthFailure = error instanceof HttpErrorResponse && error.status === 401;

      // Nothing to refresh with, or the failure was not about authentication.
      if (!isAuthFailure || !tokens.refreshToken) {
        return throwError(() => error);
      }

      return handleUnauthorized(request, next, auth, tokens, router);
    }),
  );
};

function withBearer(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function handleUnauthorized(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
  auth: AuthService,
  tokens: TokenService,
  router: Router,
): Observable<HttpEvent<unknown>> {
  // A refresh is already running: wait for its result instead of starting another.
  if (refreshInProgress) {
    return refreshedToken$.pipe(
      filter((token): token is string => token !== null),
      take(1),
      switchMap((token) => next(withBearer(request, token))),
    );
  }

  refreshInProgress = true;
  refreshedToken$.next(null);

  return auth.refreshToken().pipe(
    switchMap((response) => {
      refreshInProgress = false;
      refreshedToken$.next(response.accessToken);
      return next(withBearer(request, response.accessToken));
    }),
    catchError((refreshError: unknown) => {
      // The refresh token is gone, expired or revoked - this session is over.
      refreshInProgress = false;
      tokens.clear();

      void router.navigate(['/login'], {
        queryParams: { returnUrl: router.url, reason: 'session-expired' },
      });

      return throwError(() => refreshError);
    }),
  );
}
