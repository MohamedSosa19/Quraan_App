import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

/**
 * Parses RFC 7807 problem+json bodies into a strongly-typed shape so feature
 * code can branch on `error.problem.type` instead of stringly-typed status
 * codes (Principle VII).
 */
export interface ProblemError {
  type: string;
  title: string;
  status: number;
  detail?: string;
  correlationId?: string;
}

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const problem: ProblemError = {
        type: err.error?.type ?? 'about:blank',
        title: err.error?.title ?? err.statusText,
        status: err.status,
        detail: err.error?.detail,
        correlationId: err.error?.correlationId ?? err.headers.get('X-Correlation-ID') ?? undefined,
      };
      return throwError(() => Object.assign(err, { problem }));
    }),
  );
};
