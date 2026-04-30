import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { provideTranslateService, TranslateLoader, TranslateNoOpLoader } from '@ngx-translate/core';
import { BehaviorSubject, of } from 'rxjs';
import { convertToParamMap, ParamMap } from '@angular/router';

import { signal } from '@angular/core';

import { AudioPlayerService } from '../audio/audio-player.service';
import { TafsirApiService } from '../tafsir/tafsir-api.service';
import { Ayah, QuranApiService, SurahDetail } from './quran-api.service';
import { SurahReaderPage } from './surah-reader.page';

function buildAyahs(count: number): Ayah[] {
  const out: Ayah[] = [];
  for (let i = 1; i <= count; i++) {
    out.push({
      id: i,
      surahId: 1,
      numberInSurah: i,
      arabicText: `آية ${i}`,
      translationText: `Verse ${i}`,
      juzNumber: 1,
      hizbQuarter: 1,
      sajda: false,
    });
  }
  return out;
}

function buildDetail(ayahCount = 7): SurahDetail {
  return {
    id: 1,
    arabicName: 'الفاتحة',
    transliteratedName: 'Al-Fatiha',
    englishName: 'The Opening',
    revelationPlace: 'Meccan',
    ayahCount,
    translation: { code: 'en.sahih', language: 'en', name: 'Saheeh', attribution: 'x' },
    ayahs: buildAyahs(ayahCount),
  };
}

describe('SurahReaderPage', () => {
  let apiSpy: jasmine.SpyObj<QuranApiService>;
  let router: jasmine.SpyObj<Router>;
  let paramMap$: BehaviorSubject<ParamMap>;
  let queryParamMap$: BehaviorSubject<ParamMap>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj<QuranApiService>('QuranApiService', ['getSurah']);
    apiSpy.getSurah.and.returnValue(of(buildDetail()));

    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.returnValue(Promise.resolve(true));

    paramMap$ = new BehaviorSubject<ParamMap>(convertToParamMap({ surahId: '2' }));
    queryParamMap$ = new BehaviorSubject<ParamMap>(convertToParamMap({}));

    const audioStub: Partial<AudioPlayerService> = {
      state: signal('idle'),
      currentSurahId: signal<number | null>(null),
      currentAyah: signal<number | null>(null),
      hasTimings: signal(false),
      errorMessage: signal<string | null>(null),
    } as Partial<AudioPlayerService>;

    await TestBed.configureTestingModule({
      imports: [SurahReaderPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService({
          defaultLanguage: 'en',
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader },
        }),
        { provide: QuranApiService, useValue: apiSpy },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { paramMap: paramMap$, queryParamMap: queryParamMap$ },
        },
        { provide: AudioPlayerService, useValue: audioStub },
        {
          provide: TafsirApiService,
          useValue: jasmine.createSpyObj<TafsirApiService>('TafsirApiService', ['getEntry']),
        },
      ],
    }).compileComponents();
  });

  it('loads the requested surah from route params', async () => {
    const fixture = TestBed.createComponent(SurahReaderPage);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(apiSpy.getSurah).toHaveBeenCalledWith(2);
    expect(fixture.componentInstance.surahId()).toBe(2);
  });

  it('navigates to the next surah on goNext()', async () => {
    const fixture = TestBed.createComponent(SurahReaderPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.goNext();
    expect(router.navigate).toHaveBeenCalledWith(['/surahs', 3]);
  });

  it('navigates to the previous surah on goPrev()', async () => {
    const fixture = TestBed.createComponent(SurahReaderPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.goPrev();
    expect(router.navigate).toHaveBeenCalledWith(['/surahs', 1]);
  });

  it('jump() scrolls to the requested ayah element', async () => {
    const fixture = TestBed.createComponent(SurahReaderPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const stub = document.createElement('div');
    stub.id = 'ayah-3';
    spyOn(stub, 'scrollIntoView');
    spyOn(document, 'getElementById').and.returnValue(stub);

    fixture.componentInstance.jumpInput.set('3');
    fixture.componentInstance.jump();

    expect(document.getElementById).toHaveBeenCalledWith('ayah-3');
    expect(stub.scrollIntoView).toHaveBeenCalled();
  });

  it('clamps surahId at 1 when route says 0', async () => {
    paramMap$.next(convertToParamMap({ surahId: '0' }));
    const fixture = TestBed.createComponent(SurahReaderPage);
    fixture.detectChanges();
    expect(fixture.componentInstance.surahId()).toBe(1);
  });
});
