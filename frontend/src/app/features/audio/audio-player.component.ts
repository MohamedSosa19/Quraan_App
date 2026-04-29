import { ChangeDetectionStrategy, Component, Input, computed, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { AudioPlayerService } from './audio-player.service';

@Component({
  selector: 'app-audio-player',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <section class="audio-player" aria-label="Audio recitation">
      @if (state() === 'error') {
        <div class="audio-player__error" role="alert">
          <span>{{ (errorKey() || 'audio.error.unreachable') | translate }}</span>
          <button type="button" data-testid="audio-retry" (click)="retry()">
            {{ 'audio.retry' | translate }}
          </button>
        </div>
      }
      <div class="audio-player__controls">
        @if (state() === 'playing') {
          <button type="button" data-testid="audio-pause" (click)="pause()">
            {{ 'audio.pause' | translate }}
          </button>
        } @else if (state() === 'paused') {
          <button type="button" data-testid="audio-resume" (click)="resume()">
            {{ 'audio.resume' | translate }}
          </button>
        } @else {
          <button
            type="button"
            data-testid="audio-play"
            [disabled]="state() === 'loading'"
            (click)="play()"
          >
            {{ 'audio.play' | translate }}
          </button>
        }
        <span class="audio-player__status" aria-live="polite">
          @if (state() === 'loading') {
            {{ 'common.loading' | translate }}
          } @else if (currentAyah() !== null) {
            {{ 'audio.ayahLabel' | translate: { n: currentAyah() } }}
          }
        </span>
      </div>
    </section>
  `,
  styles: [
    `
      .audio-player {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        padding-block: var(--space-3);
        padding-inline: var(--space-3);
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        margin-block-end: var(--space-3);
      }

      .audio-player__controls {
        display: flex;
        align-items: center;
        gap: var(--space-3);
      }

      .audio-player__error {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        color: #b3261e;
      }

      button {
        padding-block: var(--space-2);
        padding-inline: var(--space-3);
        border: 1px solid var(--color-border);
        background: var(--color-bg);
        color: var(--color-fg);
        border-radius: var(--radius-sm);
        cursor: pointer;

        &:disabled { opacity: 0.5; cursor: not-allowed; }
        &:hover:not(:disabled) { border-color: var(--color-accent); }
      }

      .audio-player__status {
        color: var(--color-muted);
        font-size: 0.875rem;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AudioPlayerComponent {
  private readonly player = inject(AudioPlayerService);

  @Input({ required: true }) surahId!: number;

  readonly state = this.player.state;
  readonly currentAyah = this.player.currentAyah;
  readonly errorKey = this.player.errorMessage;

  // Re-expose so the surah-reader's template can highlight matching ayahs.
  readonly playingAyah = computed(() => this.currentAyah());

  play(): void {
    void this.player.play(this.surahId);
  }
  pause(): void { this.player.pause(); }
  resume(): void { this.player.resume(); }
  retry(): void { this.player.retry(); }
}
