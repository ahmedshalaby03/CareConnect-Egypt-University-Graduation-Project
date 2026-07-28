import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from '../models/user.model';
import { TokenService } from '../services/token.service';

/**
 * Restricts a route to a set of roles. Use as `canActivate: [authGuard, roleGuard('SuperAdmin')]`.
 *
 * As with authGuard this is a UX affordance: matching API endpoints carry their own
 * [Authorize] policies. Wrong-role navigation gets a clear 403 page.
 */
export function roleGuard(...allowedRoles: UserRole[]): CanActivateFn {
  return () => {
    const tokens = inject(TokenService);
    const router = inject(Router);

    const role = tokens.user?.role ?? null;

    if (role && allowedRoles.includes(role)) {
      return true;
    }

    return role ? router.parseUrl('/forbidden') : router.parseUrl('/login');
  };
}
