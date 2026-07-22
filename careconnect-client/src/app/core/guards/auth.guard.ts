import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';

/**
 * Keeps signed-out visitors away from application screens.
 *
 * This is navigation convenience only. The API authorises every request independently, so
 * bypassing this guard in the browser gains an attacker nothing.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const tokens = inject(TokenService);
  const router = inject(Router);

  if (tokens.accessToken) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};
