import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { LastReadService } from '../../core/last-read.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  template: `
    <section class="home">
      <h1>{{ 'home.title' | translate }}</h1>
      <p class="home__subtitle">{{ 'home.subtitle' | translate }}</p>
      <a class="home__cta" routerLink="/surahs">
        {{ 'home.openSurahs' | translate }}
      </a>
      @if (lastRead.position(); as p) {
        <a
          class="home__continue"
          data-testid="home-continue-reading"
          [routerLink]="['/surahs', p.surahId]"
          [queryParams]="{ ayah: p.numberInSurah }"
        >
          {{ 'home.continueReading' | translate: { surah: p.surahId, ayah: p.numberInSurah } }}
        </a>
      }
    </section>
  `,
  styles: [
    `
      .home {
        padding-block: var(--space-8);
        padding-inline: var(--space-4);
        max-width: 48rem;
        margin-inline: auto;
        text-align: center;
      }

      h1 {
        color: var(--color-fg);
        margin-block-end: var(--space-3);
      }

      .home__subtitle {
        color: var(--color-muted);
        margin-block-end: var(--space-6);
      }

      .home__cta {
        display: inline-block;
        padding-block: var(--space-3);
        padding-inline: var(--space-6);
        background: var(--color-accent);
        color: #fff;
        border-radius: var(--radius-md);
        text-decoration: none;
        font-weight: 600;

        &:hover {
          filter: brightness(1.05);
        }
      }

      .home__continue {
        display: inline-block;
        margin-block-start: var(--space-4);
        padding-block: var(--space-2);
        padding-inline: var(--space-4);
        border: 1px solid var(--color-border, #d4d4d8);
        border-radius: var(--radius-md);
        color: var(--color-fg);
        text-decoration: none;
        font-weight: 500;

        &:hover {
          background: var(--color-surface-hover, rgba(0, 0, 0, 0.04));
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  protected readonly lastRead = inject(LastReadService);
}
