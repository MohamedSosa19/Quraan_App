import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { LanguageService } from './core/i18n/language.service';
import { RtlService } from './core/i18n/rtl.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterOutlet, TranslateModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly language = inject(LanguageService);
  // Eagerly initialize so document.documentElement.dir is set on bootstrap.
  private readonly _rtl = inject(RtlService);

  switchLanguage(): void {
    this.language.setLanguage(this.language.currentLang() === 'ar' ? 'en' : 'ar');
  }

  get currentLang(): 'ar' | 'en' {
    return this.language.currentLang();
  }
}
