import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { RtlService } from './core/i18n/rtl.service';
import { LanguageToggleComponent } from './shared/components/language-toggle/language-toggle.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterOutlet, TranslateModule, LanguageToggleComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  // Eagerly initialize so document.documentElement.dir is set on bootstrap.
  private readonly _rtl = inject(RtlService);
}
