import { ChangeDetectionStrategy, Component, Input, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { TafsirPanelService } from '../tafsir/tafsir-panel.service';
import { Ayah } from './quran-api.service';

@Component({
  selector: 'app-ayah',
  standalone: true,
  imports: [TranslateModule],
  template: `
    @if (ayah) {
      <article
        class="ayah"
        [class.is-playing]="isPlaying"
        [attr.data-ayah]="ayah.numberInSurah"
        [id]="'ayah-' + ayah.numberInSurah"
      >
        <header class="ayah__header">
          <span class="ayah__number" aria-hidden="true">{{ ayah.numberInSurah }}</span>
        </header>
        <p class="ayah__arabic mushaf-font" lang="ar" dir="rtl">{{ ayah.arabicText }}</p>
        <p class="ayah__translation" lang="en" dir="ltr">{{ ayah.translationText }}</p>
        <footer class="ayah__actions">
          <button
            type="button"
            class="ayah__tafsir-button"
            data-testid="ayah-tafsir-button"
            (click)="openTafsir()"
          >
            {{ 'tafsir.open' | translate }}
          </button>
        </footer>
      </article>
    }
  `,
  styles: [
    `
      .ayah {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: var(--space-3);
        padding-block: var(--space-3);
        padding-inline: var(--space-2);
        border-block-end: 1px solid var(--color-border);
        transition: background-color 0.2s ease;
      }

      .ayah.is-playing {
        background: color-mix(in srgb, var(--color-accent) 8%, transparent);
        border-inline-start: 3px solid var(--color-accent);
      }

      .ayah__header { display: flex; align-items: flex-start; }

      .ayah__number {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        min-inline-size: 2rem;
        block-size: 2rem;
        border-radius: 999rem;
        background: var(--color-surface);
        color: var(--color-muted);
        font-weight: 600;
        font-variant-numeric: tabular-nums;
      }

      .ayah__arabic {
        font-size: 1.5rem;
        line-height: 2.1;
        margin: 0;
        color: var(--color-fg);
      }

      .ayah__translation {
        margin: 0;
        margin-block-start: var(--space-2);
        color: var(--color-muted);
      }

      .ayah__actions {
        grid-column: 1 / -1;
        display: flex;
        justify-content: flex-end;
        margin-block-start: var(--space-2);
      }

      .ayah__tafsir-button {
        background: transparent;
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        padding: var(--space-1) var(--space-3);
        font-size: 0.875rem;
        color: var(--color-muted);
        cursor: pointer;
      }

      .ayah__tafsir-button:hover {
        color: var(--color-accent);
        border-color: var(--color-accent);
      }

      @media (min-width: 48rem) {
        .ayah { grid-template-columns: auto 1fr 1fr; }
        .ayah__translation { margin-block-start: 0; }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AyahComponent {
  @Input({ required: true }) ayah!: Ayah;
  @Input() isPlaying = false;

  private readonly tafsirPanel = inject(TafsirPanelService);

  openTafsir(): void {
    this.tafsirPanel.open(this.ayah.surahId, this.ayah.numberInSurah);
  }
}
