import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { Bookmark, BookmarksApiService, CreateBookmarkRequest } from './bookmarks-api.service';

/**
 * Singleton store for the signed-in user's bookmarks. Read this from any
 * component that needs to know "is THIS Ayah bookmarked?" — e.g., the
 * BookmarkButton inside AyahComponent reads `bookmarkedAyahs()` to flip
 * its icon.
 *
 * Loads on demand from `GET /bookmarks`. Mutations call the API and update
 * the local cache so the UI flips immediately without a re-fetch.
 */
@Injectable({ providedIn: 'root' })
export class BookmarksStore {
  private readonly api = inject(BookmarksApiService);
  private readonly auth = inject(AuthService);

  readonly items = signal<Bookmark[]>([]);
  readonly loaded = signal(false);

  readonly bookmarkedAyahs = computed(() => {
    const set = new Set<string>();
    for (const b of this.items()) set.add(`${b.surahId}:${b.numberInSurah}`);
    return set;
  });

  isBookmarked(surahId: number, numberInSurah: number): boolean {
    return this.bookmarkedAyahs().has(`${surahId}:${numberInSurah}`);
  }

  findBookmark(surahId: number, numberInSurah: number): Bookmark | undefined {
    return this.items().find((b) => b.surahId === surahId && b.numberInSurah === numberInSurah);
  }

  async load(): Promise<void> {
    if (!this.auth.isAuthenticated()) return;
    const page = await firstValueFrom(this.api.list(1, 100));
    this.items.set(page.items);
    this.loaded.set(true);
  }

  async add(req: CreateBookmarkRequest): Promise<Bookmark> {
    const created = await firstValueFrom(this.api.create(req));
    this.items.set([created, ...this.items()]);
    return created;
  }

  async remove(bookmarkId: string): Promise<void> {
    await firstValueFrom(this.api.remove(bookmarkId));
    this.items.set(this.items().filter((b) => b.id !== bookmarkId));
  }

  clear(): void {
    this.items.set([]);
    this.loaded.set(false);
  }
}
