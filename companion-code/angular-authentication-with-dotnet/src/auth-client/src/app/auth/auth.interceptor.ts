import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { AuthService } from './auth.service';

// Functional interceptor (HttpInterceptorFn) - the class-based HttpInterceptor interface
// still works, but functional is the form `provideHttpClient(withInterceptors(...))` expects
// and it's what `ng generate interceptor` scaffolds by default now.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  if (!req.url.startsWith(API_BASE_URL)) {
    return next(req);
  }

  const token = auth.accessToken();
  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` }, withCredentials: true })
    : req.clone({ withCredentials: true });

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      const isAuthEndpoint = req.url.includes('/api/auth/');
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthEndpoint) {
        // The access token expired mid-session; retry once against the refresh-token cookie
        // before giving up, so a 5-minute access token doesn't mean a 5-minute login.
        return auth.refresh().pipe(
          switchMap((refreshed) => {
            if (!refreshed) return throwError(() => error);
            const retryToken = auth.accessToken();
            return next(req.clone({ setHeaders: { Authorization: `Bearer ${retryToken}` }, withCredentials: true }));
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
