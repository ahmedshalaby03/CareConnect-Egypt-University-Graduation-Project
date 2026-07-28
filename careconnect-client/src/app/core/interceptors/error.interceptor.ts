import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiResponse } from '../models/api-response.model';

/**
 * Normalises everything the API (or the network) can throw into a single readable string on
 * `error.friendlyMessage`, so components never have to dig through the response shape.
 */
export interface FriendlyHttpError extends HttpErrorResponse {
  friendlyMessage: string;
  validationErrors: string[];
}

export const errorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const body = error.error as Partial<ApiResponse<unknown>> | string | null;
      const validationErrors =
        typeof body === 'object' && body !== null && Array.isArray(body.errors)
          ? body.errors
          : [];

      Object.assign(error, {
        friendlyMessage: resolveMessage(error, body),
        validationErrors,
      });

      return throwError(() => error);
    }),
  );

function resolveMessage(
  error: HttpErrorResponse,
  body: Partial<ApiResponse<unknown>> | string | null,
): string {
  if (typeof body === 'object' && body !== null && typeof body.message === 'string' && body.message) {
    return body.message;
  }

  // status 0 means the request never reached the server.
  if (error.status === 0) {
    return 'Cannot reach the CareConnect API. Check that the backend is running.';
  }

  switch (error.status) {
    case 401:
      return 'Your session is invalid or has expired. Please sign in again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested resource was not found.';
    case 409:
      return 'The request conflicts with the latest saved data. Refresh and try again.';
    case 429:
      return 'Too many requests were sent. Please wait a moment and try again.';
    case 500:
      return 'The server could not complete the request. Please try again.';
    case 503:
      return 'The service is temporarily unavailable. Please try again shortly.';
    default:
      return 'Something went wrong. Please try again.';
  }
}

/** Safe accessor for the message the interceptor attached. */
export function friendlyMessageOf(error: unknown, fallback = 'Something went wrong.'): string {
  if (error instanceof HttpErrorResponse) {
    const enriched = error as Partial<FriendlyHttpError>;
    return enriched.friendlyMessage ?? fallback;
  }

  return fallback;
}

export function validationErrorsOf(error: unknown): string[] {
  if (error instanceof HttpErrorResponse) {
    return (error as Partial<FriendlyHttpError>).validationErrors ?? [];
  }

  return [];
}
