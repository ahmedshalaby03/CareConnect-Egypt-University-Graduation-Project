import { Injectable, signal } from '@angular/core';
import { AuthSession } from '../models/auth.model';
import { User } from '../models/user.model';

const STORAGE_KEY = 'careconnect.session';

/**
 * The only place in the application that touches persistent storage.
 *
 * Everything else reads the session through this service, so swapping localStorage for
 * sessionStorage, a cookie or an in-memory store later is a change to this one file.
 */
@Injectable({ providedIn: 'root' })
export class TokenService {
  /** Signal so components and guards react to sign-in and sign-out without a subscription. */
  private readonly sessionSignal = signal<AuthSession | null>(this.read());

  readonly session = this.sessionSignal.asReadonly();

  get accessToken(): string | null {
    return this.sessionSignal()?.accessToken ?? null;
  }

  get refreshToken(): string | null {
    return this.sessionSignal()?.refreshToken ?? null;
  }

  get user(): User | null {
    return this.sessionSignal()?.user ?? null;
  }

  save(session: AuthSession): void {
    this.sessionSignal.set(session);
    this.write(session);
  }

  /** Used after /me, so a role or status change on the server reaches the UI. */
  updateUser(user: User): void {
    const current = this.sessionSignal();
    if (!current) {
      return;
    }

    this.save({ ...current, user });
  }

  clear(): void {
    this.sessionSignal.set(null);

    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Private browsing modes can refuse storage access; signing out must not throw.
    }
  }

  private read(): AuthSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }

      const parsed = JSON.parse(raw) as Partial<AuthSession>;

      // Anything hand-edited or left over from an older shape is discarded rather than
      // trusted, otherwise the app boots into a half-authenticated state.
      if (!parsed?.accessToken || !parsed?.refreshToken || !parsed?.user?.role) {
        return null;
      }

      return parsed as AuthSession;
    } catch {
      return null;
    }
  }

  private write(session: AuthSession): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Storage full or blocked: the session still lives in the signal for this tab.
    }
  }
}
