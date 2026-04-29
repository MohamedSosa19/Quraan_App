import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AudioApiService, AudioRecitation } from './audio-api.service';
import {
  AudioPlayerService,
  HOWL_FACTORY,
  HowlFactory,
  HowlInitOptions,
  HowlLike,
} from './audio-player.service';

class FakeHowl implements HowlLike {
  static last: FakeHowl | null = null;
  static seekValue = 0;
  static playCount = 0;
  static pauseCount = 0;

  constructor(public readonly options: HowlInitOptions) {
    FakeHowl.last = this;
  }

  play(): void {
    FakeHowl.playCount++;
    this.options.onplay?.();
  }
  pause(): void {
    FakeHowl.pauseCount++;
    this.options.onpause?.();
  }
  stop(): void { /* noop */ }
  unload(): void { /* noop */ }
  seek(): number { return FakeHowl.seekValue; }

  static reset(): void {
    FakeHowl.last = null;
    FakeHowl.seekValue = 0;
    FakeHowl.playCount = 0;
    FakeHowl.pauseCount = 0;
  }
}

const fakeFactory: HowlFactory = (opts) => new FakeHowl(opts);

const RECITATION: AudioRecitation = {
  surahId: 78,
  reciter: { code: 'ar.alafasy', name: 'Alafasy', arabicName: 'العفاسي', attribution: '©' },
  audioUrl: 'https://cdn.example/78.mp3',
  hasTimings: true,
  ayahTimings: [
    { numberInSurah: 1, fromMs: 0,    toMs: 1500 },
    { numberInSurah: 2, fromMs: 1500, toMs: 3200 },
    { numberInSurah: 3, fromMs: 3200, toMs: 5000 },
  ],
};

describe('AudioPlayerService', () => {
  let api: jasmine.SpyObj<AudioApiService>;
  let svc: AudioPlayerService;

  beforeEach(() => {
    FakeHowl.reset();
    api = jasmine.createSpyObj<AudioApiService>('AudioApiService', ['getRecitation']);
    api.getRecitation.and.returnValue(of(RECITATION));

    TestBed.configureTestingModule({
      providers: [
        AudioPlayerService,
        { provide: AudioApiService, useValue: api },
        { provide: HOWL_FACTORY, useValue: fakeFactory },
      ],
    });
    svc = TestBed.inject(AudioPlayerService);
  });

  afterEach(() => svc.dispose());

  it('starts in idle state with no current ayah', () => {
    expect(svc.state()).toBe('idle');
    expect(svc.currentAyah()).toBeNull();
  });

  it('play() fetches recitation, instantiates Howl, and transitions to playing', async () => {
    await svc.play(78);
    expect(api.getRecitation).toHaveBeenCalledWith(78, 'ar.alafasy');
    expect(FakeHowl.last).not.toBeNull();
    expect(FakeHowl.playCount).toBe(1);
    expect(svc.state()).toBe('playing');
    expect(svc.hasTimings()).toBeTrue();
  });

  it('pause() and resume() flip state without re-fetching', async () => {
    await svc.play(78);
    svc.pause();
    expect(svc.state()).toBe('paused');
    svc.resume();
    expect(svc.state()).toBe('playing');
    expect(api.getRecitation).toHaveBeenCalledTimes(1);
  });

  it('derives currentAyah from a 250 ms poll over the timings array (R-09)', async () => {
    jasmine.clock().install();
    try {
      await svc.play(78);
      FakeHowl.seekValue = 0.5;  // 500 ms → ayah 1
      jasmine.clock().tick(260);
      expect(svc.currentAyah()).toBe(1);

      FakeHowl.seekValue = 2.0;  // 2000 ms → ayah 2
      jasmine.clock().tick(260);
      expect(svc.currentAyah()).toBe(2);

      FakeHowl.seekValue = 4.0;  // 4000 ms → ayah 3
      jasmine.clock().tick(260);
      expect(svc.currentAyah()).toBe(3);
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('keeps currentAyah null when timings are absent (FR-013)', async () => {
    api.getRecitation.and.returnValue(of({ ...RECITATION, hasTimings: false, ayahTimings: [] }));
    jasmine.clock().install();
    try {
      await svc.play(78);
      FakeHowl.seekValue = 1.0;
      jasmine.clock().tick(260);
      expect(svc.currentAyah()).toBeNull();
      expect(svc.hasTimings()).toBeFalse();
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('emits onPlayStarted when Howl reports playback begin (used for SC-003 timing)', async () => {
    let fired = 0;
    svc.onPlayStarted.subscribe(() => fired++);
    await svc.play(78);
    expect(fired).toBe(1);
  });

  it('surfaces an error state when the API fails (FR-014)', async () => {
    api.getRecitation.and.returnValue(throwError(() => new Error('boom')));
    await expectAsync(svc.play(78)).toBeRejected();
    expect(svc.state()).toBe('error');
    expect(svc.errorMessage()).toBe('audio.error.unreachable');
  });
});
