import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { homeRouteForRole } from '../models/user.model';
import { TokenService } from '../services/token.service';

/** Sends an already-signed-in user away from the login and registration pages. */
export const guestGuard: CanActivateFn = () => {
  const tokens = inject(TokenService);
  const router = inject(Router);

  const user = tokens.user;

  if (!user) {
    return true;
  }

  return router.parseUrl(homeRouteForRole(user.role));
};
