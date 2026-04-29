import { Injectable, effect, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLocale = 'ar' | 'en';

const STORAGE_KEY = 'quraan.lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  readonly currentLang = signal<AppLocale>(this.read());

  constructor() {
    this.translate.use(this.currentLang());
    effect(() => {
      const lang = this.currentLang();
      this.translate.use(lang);
      try { localStorage.setItem(STORAGE_KEY, lang); } catch { /* ignore */ }
    });
  }

  setLanguage(lang: AppLocale): void {
    this.currentLang.set(lang);
  }

  private read(): AppLocale {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'ar' || stored === 'en') return stored;
    } catch { /* ignore */ }
    return navigator.language?.startsWith('ar') ? 'ar' : 'en';
  }
}
