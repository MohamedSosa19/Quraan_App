import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthService, AuthResponse } from './auth.service';

const RESPONSE: AuthResponse = {
  accessToken: 'jwt.token.value',
  expiresInSeconds: 900,
  user: { id: '00000000-0000-0000-0000-000000000001', email: 'a@b.test', preferredLanguage: 'en' },
};

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    (globalThis as Record<string, unknown>)['__quraan_access_token__'] = undefined;
  });

  it('starts unauthenticated', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.user()).toBeNull();
  });

  it('signIn stores the access token in memory and updates user signal', async () => {
    const promise = service.signIn('a@b.test', 'pw').toPromise();
    const req = http.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(RESPONSE);
    await promise;

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.user()?.email).toBe('a@b.test');
    expect((globalThis as Record<string, unknown>)['__quraan_access_token__']).toBe('jwt.token.value');
  });

  it('register hits /auth/register and applies the auth response', async () => {
    const promise = service.register('a@b.test', 'longenoughpassword', 'Alice', 'en').toPromise();
    const req = http.expectOne('/api/v1/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.test', password: 'longenoughpassword', displayName: 'Alice', preferredLanguage: 'en' });
    req.flush(RESPONSE);
    await promise;

    expect(service.user()?.email).toBe('a@b.test');
  });

  it('refresh updates the access token without touching the user signal', async () => {
    // First sign in to set a user.
    const signInPromise = service.signIn('a@b.test', 'pw').toPromise();
    http.expectOne('/api/v1/auth/login').flush(RESPONSE);
    await signInPromise;

    const refreshPromise = service.refresh().toPromise();
    const req = http.expectOne('/api/v1/auth/refresh');
    req.flush({ accessToken: 'new.jwt', expiresInSeconds: 900 });
    await refreshPromise;

    expect((globalThis as Record<string, unknown>)['__quraan_access_token__']).toBe('new.jwt');
    expect(service.user()?.email).toBe('a@b.test');
  });

  it('signOut clears the user signal and the in-memory token', async () => {
    const signInPromise = service.signIn('a@b.test', 'pw').toPromise();
    http.expectOne('/api/v1/auth/login').flush(RESPONSE);
    await signInPromise;
    expect(service.isAuthenticated()).toBeTrue();

    const out = service.signOut();
    http.expectOne('/api/v1/auth/logout').flush(null, { status: 204, statusText: 'No Content' });
    await out;

    expect(service.isAuthenticated()).toBeFalse();
    expect((globalThis as Record<string, unknown>)['__quraan_access_token__']).toBeUndefined();
  });

  it('signOut still clears state when the server call fails', async () => {
    const signInPromise = service.signIn('a@b.test', 'pw').toPromise();
    http.expectOne('/api/v1/auth/login').flush(RESPONSE);
    await signInPromise;

    const out = service.signOut();
    http.expectOne('/api/v1/auth/logout').flush(null, { status: 500, statusText: 'Server Error' });
    await out;

    expect(service.isAuthenticated()).toBeFalse();
  });
});
