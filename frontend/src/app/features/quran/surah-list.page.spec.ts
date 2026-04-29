import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateLoader, TranslateNoOpLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { QuranApiService, SurahSummary } from './quran-api.service';
import { SurahListPage } from './surah-list.page';

function buildSurahs(): SurahSummary[] {
  const out: SurahSummary[] = [];
  for (let i = 1; i <= 114; i++) {
    out.push({
      id: i,
      arabicName: `سورة ${i}`,
      transliteratedName: `Surah ${i}`,
      englishName: `English ${i}`,
      revelationPlace: i <= 86 ? 'Meccan' : 'Medinan',
      ayahCount: i,
    });
  }
  return out;
}

describe('SurahListPage', () => {
  let apiSpy: jasmine.SpyObj<QuranApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj<QuranApiService>('QuranApiService', ['getSurahs']);
    apiSpy.getSurahs.and.returnValue(of(buildSurahs()));

    await TestBed.configureTestingModule({
      imports: [SurahListPage],
      providers: [
        provideRouter([]),
        provideTranslateService({
          defaultLanguage: 'en',
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader },
        }),
        { provide: QuranApiService, useValue: apiSpy },
      ],
    }).compileComponents();
  });

  it('renders 114 surahs from the API', async () => {
    const fixture = TestBed.createComponent(SurahListPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('.surah-list__item');
    expect(items.length).toBe(114);
  });

  it('binds Arabic and English names to each row', async () => {
    const fixture = TestBed.createComponent(SurahListPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const first = (fixture.nativeElement as HTMLElement).querySelector('.surah-list__item');
    expect(first?.querySelector('.surah-list__arabic')?.textContent).toContain('سورة 1');
    expect(first?.querySelector('.surah-list__transliterated')?.textContent).toContain('Surah 1');
    expect(first?.querySelector('.surah-list__english')?.textContent).toContain('English 1');
  });

  it('shows the load-failed banner on error', async () => {
    apiSpy.getSurahs.and.returnValue(
      new Observable((subscriber) => subscriber.error(new Error('boom'))),
    );

    const fixture = TestBed.createComponent(SurahListPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.status--error')).toBeTruthy();
  });
});
