import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface LastReadPosition {
  surahId: number;
  numberInSurah: number;
  updatedAt: string;
}

const STORAGE_KEY = 'quraan.last-read.v1';

/**
 * R-13: anonymous users have their position cached in localStorage; signed-in
 * users round-trip through `PUT /api/v1/lastread/me` (last-write-wins by
 * `updatedAt`). On sign-in, `mergeOnSignIn()` replays the local value to the
 * server; the server returns the winning row.
 */
@Injectable({ providedIn: 'root' })
export class LastReadService {
  private readonly http = inject(HttpClient);

  readonly position = signal<LastReadPosition | null>(this.readLocal());

  /** Called by the surah-reader as the user navigates between Ayahs. */
  async record(surahId: number, numberInSurah: number): Promise<void> {
    const next: LastReadPosition = {
      surahId,
      numberInSurah,
      updatedAt: new Date().toISOString(),
    };
    this.position.set(next);
    this.writeLocal(next);

    // Always attempt the server write; the auth interceptor adds the bearer
    // when present, and a 401 for anonymous users is swallowed below.
    try {
      await firstValueFrom(this.http.put<LastReadPosition>('/api/v1/lastread/me', next));
    } catch {
      // Anonymous (401) or transient failure — local copy is the source of truth.
    }
  }

  /** Replay the local value to the server after sign-in (FR-035, R-13). */
  async mergeOnSignIn(): Promise<void> {
    const local = this.readLocal();
    if (local) {
      try {
        const winner = await firstValueFrom(
          this.http.put<LastReadPosition>('/api/v1/lastread/me', local),
        );
        this.position.set(winner);
        this.writeLocal(winner);
        return;
      } catch {
        // Fall through to a plain GET if PUT failed.
      }
    }
    try {
      const remote = await firstValueFrom(
        this.http.get<LastReadPosition | null>('/api/v1/lastread/me'),
      );
      if (remote) {
        this.position.set(remote);
        this.writeLocal(remote);
      }
    } catch {
      // No server position; keep the local one.
    }
  }

  /** Called from sign-out to wipe the cached position. */
  clear(): void {
    this.position.set(null);
    if (typeof localStorage !== 'undefined') localStorage.removeItem(STORAGE_KEY);
  }

  private readLocal(): LastReadPosition | null {
    if (typeof localStorage === 'undefined') return null;
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const parsed = JSON.parse(raw) as LastReadPosition;
      if (typeof parsed.surahId === 'number' && typeof parsed.numberInSurah === 'number') {
        return parsed;
      }
    } catch {
      /* fall through */
    }
    return null;
  }

  private writeLocal(pos: LastReadPosition): void {
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(pos));
  }
}
