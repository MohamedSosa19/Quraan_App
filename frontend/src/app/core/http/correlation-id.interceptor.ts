import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Generates a per-request correlation ID and forwards it via the
 * `X-Correlation-ID` header so logs on both ends can be joined (Principle VII).
 * The backend echoes the same header on responses for client-side toast
 * traceability.
 */
export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) return next(req);
  const cid = (crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`).replace(/-/g, '');
  return next(req.clone({ setHeaders: { 'X-Correlation-ID': cid } }));
};
