import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

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
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {}
