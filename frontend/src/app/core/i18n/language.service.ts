import { Injectable, effect, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLocale = 'ar' | 'en';

const STORAGE_KEY = 'quraan.lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  readonly currentLang = signal<AppLocale>(this.read());

  /**
   * Optional sink invoked whenever the user actively switches languages while
   * signed in. Wired up in US6 (auth.service.ts) to fire `PATCH /users/me`.
   * Kept null while anonymous so US2 ships without an auth dependency.
   */
  private serverSyncSink: ((lang: AppLocale) => void) | null = null;

  constructor() {
    this.translate.use(this.currentLang());
    effect(() => {
      const lang = this.currentLang();
      this.translate.use(lang);
      try { localStorage.setItem(STORAGE_KEY, lang); } catch { /* ignore */ }
    });
  }

  /** User-initiated language switch. Fires `serverSyncSink` if registered. */
  setLanguage(lang: AppLocale): void {
    if (this.currentLang() === lang) return;
    this.currentLang.set(lang);
    this.serverSyncSink?.(lang);
  }

  /**
   * Apply a server-supplied preference (e.g. on sign-in). Updates the local
   * state without re-firing `serverSyncSink` so we don't loop a PATCH back
   * to the server that just told us the value.
   */
  applyServerPreference(lang: AppLocale): void {
    if (this.currentLang() === lang) return;
    this.currentLang.set(lang);
  }

  /** Registered by US6 once `auth.service.ts` exists. */
  registerServerSyncSink(sink: ((lang: AppLocale) => void) | null): void {
    this.serverSyncSink = sink;
  }

  private read(): AppLocale {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'ar' || stored === 'en') return stored;
    } catch { /* ignore */ }
    return navigator.language?.startsWith('ar') ? 'ar' : 'en';
  }
}
