import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { homeRouteForRole, UserRole } from '../models/user.model';
import { TokenService } from '../services/token.service';

/**
 * Restricts a route to a set of roles. Use as `canActivate: [authGuard, roleGuard('SuperAdmin')]`.
 *
 * A user with the wrong role is sent to their own dashboard rather than to an error page.
 * As with authGuard this is a UX affordance: the matching API endpoints carry their own
 * [Authorize] policies.
 */
export function roleGuard(...allowedRoles: UserRole[]): CanActivateFn {
  return () => {
    const tokens = inject(TokenService);
    const router = inject(Router);

    const role = tokens.user?.role ?? null;

    if (role && allowedRoles.includes(role)) {
      return true;
    }

    return router.parseUrl(role ? homeRouteForRole(role) : '/login');
  };
}
