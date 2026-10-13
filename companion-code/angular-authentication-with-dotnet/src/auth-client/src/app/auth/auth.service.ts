import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { API_BASE_URL } from './api-config';

interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  email: string;
  displayName: string;
}

export interface Session {
  accessToken: string;
  email: string;
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Held in a signal, not localStorage: JavaScript-readable storage is exactly what an XSS
  // payload would scrape, and an access token lives at most 5 minutes so losing it on reload
  // is cheap - the HttpOnly refresh cookie (never visible to JavaScript) gets a new one back.
  private readonly session = signal<Session | null>(null);

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly currentUser = computed(() => this.session());

  constructor(private readonly http: HttpClient) {}

  accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  login(email: string, password: string): Observable<boolean> {
    return this.http
      .post<LoginResponse>(`${API_BASE_URL}/api/auth/login`, { email, password }, { withCredentials: true })
      .pipe(
        tap((response) => this.applySession(response)),
        map(() => true),
        catchError(() => of(false)),
      );
  }

  // Called by the interceptor on a 401, and on app start so a page refresh can pick a session
  // back up from the refresh-token cookie alone, without the user having to log in again.
  refresh(): Observable<boolean> {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/api/auth/refresh`, {}, { withCredentials: true }).pipe(
      tap((response) => this.applySession(response)),
      map(() => true),
      catchError(() => {
        this.session.set(null);
        return of(false);
      }),
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/api/auth/logout`, {}, { withCredentials: true }).pipe(
      tap(() => this.session.set(null)),
      catchError(() => {
        this.session.set(null);
        return of(undefined);
      }),
    );
  }

  private applySession(response: LoginResponse): void {
    this.session.set({
      accessToken: response.accessToken,
      email: response.email,
      displayName: response.displayName,
    });
  }
}
