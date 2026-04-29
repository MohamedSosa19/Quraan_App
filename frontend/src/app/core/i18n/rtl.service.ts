import { Injectable, effect, inject } from '@angular/core';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class RtlService {
  private readonly language = inject(LanguageService);

  constructor() {
    effect(() => {
      const lang = this.language.currentLang();
      const dir = lang === 'ar' ? 'rtl' : 'ltr';
      document.documentElement.setAttribute('dir', dir);
      document.documentElement.setAttribute('lang', lang);
    });
  }
}
