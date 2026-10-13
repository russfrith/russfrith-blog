import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  // A page refresh clears the in-memory session signal, so before bouncing to /login, try
  // redeeming the refresh-token cookie - it's still valid for up to 7 days after login.
  return auth.refresh().pipe(map((refreshed) => refreshed || router.createUrlTree(['/login'])));
};
