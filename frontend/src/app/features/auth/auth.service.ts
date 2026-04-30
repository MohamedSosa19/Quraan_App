import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, firstValueFrom, of, tap } from 'rxjs';

export interface AuthUser {
  id: string;
  email: string;
  displayName?: string;
  preferredLanguage: 'ar' | 'en';
}

export interface AuthResponse {
  accessToken: string;
  expiresInSeconds: number;
  user: AuthUser;
}

export interface AccessTokenResponse {
  accessToken: string;
  expiresInSeconds: number;
}

const TOKEN_KEY = '__quraan_access_token__';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly user = signal<AuthUser | null>(null);
  readonly isAuthenticated = computed(() => this.user() !== null);

  signIn(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/v1/auth/login', { email, password }, { withCredentials: true })
      .pipe(tap((res) => this.applyAuthResponse(res)));
  }

  register(email: string, password: string, displayName?: string, preferredLanguage: 'ar' | 'en' = 'en'): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/v1/auth/register', { email, password, displayName, preferredLanguage }, { withCredentials: true })
      .pipe(tap((res) => this.applyAuthResponse(res)));
  }

  refresh(): Observable<AccessTokenResponse | null> {
    return this.http
      .post<AccessTokenResponse>('/api/v1/auth/refresh', null, { withCredentials: true })
      .pipe(tap((res) => this.setAccessToken(res.accessToken)));
  }

  async signOut(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/logout', null, { withCredentials: true }));
    } catch {
      // Logout is best-effort: clear client state regardless.
    }
    this.clear();
  }

  private applyAuthResponse(res: AuthResponse): void {
    this.setAccessToken(res.accessToken);
    this.user.set(res.user);
  }

  private setAccessToken(token: string): void {
    // R-06: in-memory only — never localStorage. Survives same-tab navigation
    // but not page reload; the refresh-cookie flow restores it.
    (globalThis as Record<string, unknown>)[TOKEN_KEY] = token;
  }

  private clear(): void {
    (globalThis as Record<string, unknown>)[TOKEN_KEY] = undefined;
    this.user.set(null);
  }
}
