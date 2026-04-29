import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService, TranslateLoader, TranslateNoOpLoader } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { SearchApiService, SearchResponse } from './search-api.service';
import { SearchPage } from './search.page';

const EMPTY_RESPONSE: SearchResponse = {
  query: '',
  surahMatches: [],
  ayahMatches: [],
  page: 1,
  pageSize: 20,
  totalAyahMatches: 0,
};

const POPULATED_RESPONSE: SearchResponse = {
  query: 'name',
  surahMatches: [
    { id: 1, arabicName: 'الفاتحة', transliteratedName: 'Al-Fatiha', englishName: 'The Opening', revelationPlace: 'Meccan', ayahCount: 7 },
  ],
  ayahMatches: [
    {
      id: 1, surahId: 1, numberInSurah: 1,
      arabicText: 'بسم الله', translationText: 'In the name of Allah',
      juzNumber: 1, hizbQuarter: 1, sajda: false,
      matchedIn: 'translation', highlightSnippet: 'In the name of Allah…',
    },
  ],
  page: 1, pageSize: 20, totalAyahMatches: 1,
};

describe('SearchPage', () => {
  let api: jasmine.SpyObj<SearchApiService>;
  let router: Router;
  let navigateSpy: jasmine.Spy;

  beforeEach(async () => {
    api = jasmine.createSpyObj<SearchApiService>('SearchApiService', ['search']);

    await TestBed.configureTestingModule({
      imports: [SearchPage],
      providers: [
        provideRouter([]),
        provideTranslateService({
          defaultLanguage: 'en',
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader },
        }),
        { provide: SearchApiService, useValue: api },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    navigateSpy = spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  });

  it('shows nothing initially (empty state)', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="search-no-results"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="search-ayah-matches"]')).toBeNull();
    expect(api.search).not.toHaveBeenCalled();
  });

  it('debounces input and calls the API after 300 ms', fakeAsync(() => {
    api.search.and.returnValue(of(POPULATED_RESPONSE));
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();

    fixture.componentInstance.onQueryChange('name');
    tick(299);
    expect(api.search).not.toHaveBeenCalled();
    tick(2);
    expect(api.search).toHaveBeenCalledWith('name', 1);
  }));

  it('renders surah and ayah match sections when results return', fakeAsync(() => {
    api.search.and.returnValue(of(POPULATED_RESPONSE));
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();
    fixture.componentInstance.onQueryChange('name');
    tick(310);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="search-surah-matches"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="search-ayah-matches"]')).toBeTruthy();
  }));

  it('shows the no-results message when both arrays are empty', fakeAsync(() => {
    api.search.and.returnValue(of({ ...EMPTY_RESPONSE, query: 'qwerty' }));
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();
    fixture.componentInstance.onQueryChange('qwerty');
    tick(310);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="search-no-results"]')).toBeTruthy();
  }));

  it('clicking an ayah result navigates to /surahs/{id} with ?ayah={n}', fakeAsync(() => {
    api.search.and.returnValue(of(POPULATED_RESPONSE));
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();
    fixture.componentInstance.onQueryChange('name');
    tick(310);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('[data-testid="search-ayah-result"]') as HTMLButtonElement;
    button.click();
    expect(navigateSpy).toHaveBeenCalledWith(['/surahs', 1], { queryParams: { ayah: 1 } });
  }));

  it('shows error banner when API rejects', fakeAsync(() => {
    api.search.and.returnValue(throwError(() => new Error('boom')));
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();
    fixture.componentInstance.onQueryChange('name');
    tick(310);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.status--error')).toBeTruthy();
  }));
});
