import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Attaches the in-memory access token to outgoing API requests, when one
 * exists. Refresh-on-401 logic lives in `auth.service.ts` (US6).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = (globalThis as { __quraan_access_token__?: string }).__quraan_access_token__;
  if (token && req.url.startsWith('/api/')) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
