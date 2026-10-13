import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { API_BASE_URL } from '../auth/api-config';
import { AuthService } from '../auth/auth.service';

interface MeResponse {
  email: string;
  name: string;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  template: `
    <h1>Profile</h1>
    @if (me()) {
      <p>Signed in as {{ me()!.displayName }} ({{ me()!.email }})</p>
    }
    <button (click)="callProtectedEndpoint()">Call protected /api/me</button>
    <button (click)="logout()">Log out</button>
    @if (lastResult()) {
      <pre>{{ lastResult() }}</pre>
    }
  `,
})
export class ProfileComponent {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly me = this.auth.currentUser;
  readonly lastResult = signal<string | null>(null);

  callProtectedEndpoint(): void {
    this.http.get<MeResponse>(`${API_BASE_URL}/api/me`).subscribe({
      next: (response) => this.lastResult.set(JSON.stringify(response, null, 2)),
      error: (error) => this.lastResult.set(`Request failed: ${error.status}`),
    });
  }

  logout(): void {
    this.auth.logout().subscribe(() => this.router.navigateByUrl('/login'));
  }
}
