import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { BookmarksStore } from './bookmarks.store';

@Component({
  selector: 'app-bookmarks',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  templateUrl: './bookmarks.page.html',
  styleUrls: ['./bookmarks.page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookmarksPage {
  private readonly router = inject(Router);
  readonly store = inject(BookmarksStore);

  readonly loading = signal(false);
  readonly error = signal(false);

  readonly isEmpty = computed(() => this.store.loaded() && this.store.items().length === 0);

  constructor() {
    void this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    this.error.set(false);
    try {
      await this.store.load();
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  async remove(bookmarkId: string): Promise<void> {
    try {
      await this.store.remove(bookmarkId);
    } catch {
      this.error.set(true);
    }
  }

  open(surahId: number, numberInSurah: number): void {
    void this.router.navigate(['/surahs', surahId], { queryParams: { ayah: numberInSurah } });
  }
}
