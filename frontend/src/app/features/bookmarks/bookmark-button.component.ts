import { ChangeDetectionStrategy, Component, Input, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AuthService } from '../auth/auth.service';
import { BookmarksStore } from './bookmarks.store';

/**
 * Toggle button shown next to each Ayah. Behavior:
 *   • Anonymous click → navigate to /auth/sign-in?returnUrl=/surahs/X
 *     so the user lands back on the same Ayah after sign-in. (FR-034)
 *   • Authenticated click on an unbookmarked Ayah → POST + flip icon.
 *   • Authenticated click on an already-bookmarked Ayah → DELETE + flip.
 */
@Component({
  selector: 'app-bookmark-button',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <button
      type="button"
      class="bookmark-button"
      data-testid="bookmark-button"
      [class.is-bookmarked]="isBookmarked()"
      [attr.aria-pressed]="isBookmarked()"
      [attr.aria-label]="(isBookmarked() ? 'bookmarks.remove' : 'bookmarks.add') | translate"
      [disabled]="busy()"
      (click)="toggle()"
    >
      <span aria-hidden="true">{{ isBookmarked() ? '★' : '☆' }}</span>
    </button>
  `,
  styles: [
    `
      .bookmark-button {
        background: transparent;
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        padding: var(--space-1) var(--space-2);
        font-size: 1.125rem;
        line-height: 1;
        color: var(--color-muted);
        cursor: pointer;
      }
      .bookmark-button.is-bookmarked { color: var(--color-accent, gold); border-color: var(--color-accent, gold); }
      .bookmark-button[disabled] { opacity: 0.6; cursor: wait; }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookmarkButtonComponent {
  @Input({ required: true }) surahId!: number;
  @Input({ required: true }) numberInSurah!: number;

  private readonly auth = inject(AuthService);
  private readonly store = inject(BookmarksStore);
  private readonly router = inject(Router);

  readonly busy = signal(false);
  readonly isBookmarked = computed(() => this.store.isBookmarked(this.surahId, this.numberInSurah));

  async toggle(): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      const returnUrl = `/surahs/${this.surahId}?ayah=${this.numberInSurah}`;
      const pendingBookmark = `${this.surahId}:${this.numberInSurah}`;
      void this.router.navigate(['/auth/sign-in'], {
        queryParams: { returnUrl, pendingBookmark },
      });
      return;
    }

    this.busy.set(true);
    try {
      if (this.isBookmarked()) {
        const existing = this.store.findBookmark(this.surahId, this.numberInSurah);
        if (existing) await this.store.remove(existing.id);
      } else {
        await this.store.add({ surahId: this.surahId, numberInSurah: this.numberInSurah });
      }
    } catch {
      // Errors surface via the BookmarksPage banner; the button silently
      // resets so a transient failure doesn't leave it stuck.
    } finally {
      this.busy.set(false);
    }
  }
}
