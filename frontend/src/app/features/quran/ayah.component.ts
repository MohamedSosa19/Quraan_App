import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

import { Ayah } from './quran-api.service';

@Component({
  selector: 'app-ayah',
  standalone: true,
  template: `
    @if (ayah) {
      <article class="ayah" [attr.data-ayah]="ayah.numberInSurah" [id]="'ayah-' + ayah.numberInSurah">
        <header class="ayah__header">
          <span class="ayah__number" aria-hidden="true">{{ ayah.numberInSurah }}</span>
        </header>
        <p class="ayah__arabic mushaf-font" lang="ar" dir="rtl">{{ ayah.arabicText }}</p>
        <p class="ayah__translation" lang="en" dir="ltr">{{ ayah.translationText }}</p>
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
        border-block-end: 1px solid var(--color-border);
      }

      .ayah__header {
        display: flex;
        align-items: flex-start;
      }

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

      @media (min-width: 48rem) {
        .ayah {
          grid-template-columns: auto 1fr 1fr;
        }

        .ayah__translation {
          margin-block-start: 0;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AyahComponent {
  @Input({ required: true }) ayah!: Ayah;
}
