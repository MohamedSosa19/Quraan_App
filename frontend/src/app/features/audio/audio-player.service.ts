import { Injectable, InjectionToken, OnDestroy, inject, signal } from '@angular/core';
import { Howl } from 'howler';
import { Observable, Subject, firstValueFrom } from 'rxjs';

import { AudioApiService, AudioRecitation, AyahTiming } from './audio-api.service';

export type AudioState = 'idle' | 'loading' | 'playing' | 'paused' | 'error';

const POLL_INTERVAL_MS = 250; // R-09

export interface HowlLike {
  play(): void;
  pause(): void;
  stop(): void;
  unload(): void;
  seek(): number;
}

export interface HowlInitOptions {
  src: string[];
  html5: boolean;
  onplay?: () => void;
  onpause?: () => void;
  onend?: () => void;
  onloaderror?: () => void;
  onplayerror?: () => void;
}

export type HowlFactory = (opts: HowlInitOptions) => HowlLike;

/**
 * Injection token for the Howl factory. Tests provide a fake; production
 * resolves to the real `howler` library.
 */
export const HOWL_FACTORY = new InjectionToken<HowlFactory>('HOWL_FACTORY', {
  providedIn: 'root',
  factory: () => (opts) => new Howl(opts) as unknown as HowlLike,
});

@Injectable({ providedIn: 'root' })
export class AudioPlayerService implements OnDestroy {
  private readonly api = inject(AudioApiService);
  private readonly howlFactory = inject(HOWL_FACTORY);

  readonly state = signal<AudioState>('idle');
  readonly currentSurahId = signal<number | null>(null);
  readonly currentAyah = signal<number | null>(null);
  readonly hasTimings = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  private howl: HowlLike | null = null;
  private timings: AyahTiming[] = [];
  private pollHandle: ReturnType<typeof setInterval> | null = null;
  private readonly playStarted$ = new Subject<void>();

  /** Emits each time Howl reports playback has begun (used for SC-003 timing in e2e). */
  get onPlayStarted(): Observable<void> {
    return this.playStarted$.asObservable();
  }

  async play(surahId: number, reciter = 'ar.alafasy'): Promise<void> {
    if (this.currentSurahId() !== surahId) {
      this.dispose();
      this.currentSurahId.set(surahId);
    } else if (this.howl && this.state() === 'paused') {
      this.resume();
      return;
    }

    this.state.set('loading');
    this.errorMessage.set(null);

    let recitation: AudioRecitation;
    try {
      recitation = await firstValueFrom(this.api.getRecitation(surahId, reciter));
    } catch (err) {
      this.failWith('audio.error.unreachable');
      throw err;
    }

    this.timings = recitation.ayahTimings ?? [];
    this.hasTimings.set(recitation.hasTimings);

    this.howl = this.howlFactory({
      src: [recitation.audioUrl],
      html5: true,
      onplay: () => {
        this.state.set('playing');
        this.startPolling();
        this.playStarted$.next();
      },
      onpause: () => {
        this.state.set('paused');
        this.stopPolling();
      },
      onend: () => {
        this.state.set('idle');
        this.stopPolling();
        this.currentAyah.set(null);
      },
      onloaderror: () => this.failWith('audio.error.unreachable'),
      onplayerror: () => this.failWith('audio.error.unreachable'),
    });

    this.howl.play();
  }

  pause(): void {
    if (this.howl && this.state() === 'playing') this.howl.pause();
  }

  resume(): void {
    if (this.howl && this.state() === 'paused') this.howl.play();
  }

  retry(): void {
    const id = this.currentSurahId();
    if (id !== null) {
      this.dispose();
      void this.play(id);
    }
  }

  /** Test seam — stop and release any pending Howl + interval. */
  dispose(): void {
    this.stopPolling();
    if (this.howl) {
      try { this.howl.stop(); } catch { /* ignore */ }
      try { this.howl.unload(); } catch { /* ignore */ }
      this.howl = null;
    }
    this.timings = [];
    this.hasTimings.set(false);
    this.currentAyah.set(null);
    this.errorMessage.set(null);
    this.state.set('idle');
  }

  ngOnDestroy(): void {
    this.dispose();
    this.playStarted$.complete();
  }

  private failWith(messageKey: string): void {
    this.state.set('error');
    this.errorMessage.set(messageKey);
    this.stopPolling();
  }

  private startPolling(): void {
    this.stopPolling();
    this.pollHandle = setInterval(() => {
      if (!this.howl || !this.hasTimings()) return;
      const seekMs = this.howl.seek() * 1000;
      const found = this.findAyahAt(seekMs);
      if (found !== this.currentAyah()) this.currentAyah.set(found);
    }, POLL_INTERVAL_MS);
  }

  private stopPolling(): void {
    if (this.pollHandle !== null) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }

  private findAyahAt(seekMs: number): number | null {
    for (const t of this.timings) {
      if (seekMs >= t.fromMs && seekMs < t.toMs) return t.numberInSurah;
    }
    return null;
  }
}
