import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { LanguageService } from '../../../core/i18n/language.service';

@Component({
  selector: 'app-language-toggle',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <button
      type="button"
      class="language-toggle"
      (click)="toggle()"
      [attr.aria-label]="ariaLabel()"
      [attr.lang]="targetLang()"
      [attr.data-testid]="'language-toggle'"
    >
      {{ label() }}
    </button>
  `,
  styles: [
    `
      .language-toggle {
        padding-block: var(--space-1);
        padding-inline: var(--space-3);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        background: var(--color-bg);
        color: var(--color-fg);
        cursor: pointer;
        font: inherit;

        &:hover {
          border-color: var(--color-accent);
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageToggleComponent {
  private readonly language = inject(LanguageService);

  readonly targetLang = computed<'ar' | 'en'>(() =>
    this.language.currentLang() === 'ar' ? 'en' : 'ar',
  );

  readonly label = computed(() => (this.targetLang() === 'ar' ? 'العربية' : 'English'));
  readonly ariaLabel = computed(() =>
    this.targetLang() === 'ar' ? 'Switch to Arabic' : 'Switch to English',
  );

  toggle(): void {
    this.language.setLanguage(this.targetLang());
  }
}
