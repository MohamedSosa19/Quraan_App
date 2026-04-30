import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService, TranslateLoader, TranslateNoOpLoader } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';

import { TafsirApiService, TafsirEntry } from './tafsir-api.service';
import { TafsirPanelComponent } from './tafsir-panel.component';
import { TafsirPanelService } from './tafsir-panel.service';

const ENTRY: TafsirEntry = {
  surahId: 1,
  numberInSurah: 2,
  source: { code: 'ibn-kathir-en', name: 'Tafsir Ibn Kathir', attribution: 'Public domain' },
  body: 'Praise belongs to Allah, the Lord of all worlds…',
};

describe('TafsirPanelComponent', () => {
  let api: jasmine.SpyObj<TafsirApiService>;
  let panel: TafsirPanelService;

  beforeEach(async () => {
    api = jasmine.createSpyObj<TafsirApiService>('TafsirApiService', ['getEntry']);

    await TestBed.configureTestingModule({
      imports: [TafsirPanelComponent],
      providers: [
        provideTranslateService({
          defaultLanguage: 'en',
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader },
        }),
        { provide: TafsirApiService, useValue: api },
      ],
    }).compileComponents();

    panel = TestBed.inject(TafsirPanelService);
    panel.close();
  });

  it('renders nothing when no Ayah is selected', () => {
    const fixture = TestBed.createComponent(TafsirPanelComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-panel"]')).toBeNull();
    expect(api.getEntry).not.toHaveBeenCalled();
  });

  it('shows the loaded body and attribution when API returns an entry', () => {
    api.getEntry.and.returnValue(of(ENTRY));
    const fixture = TestBed.createComponent(TafsirPanelComponent);
    fixture.detectChanges();

    panel.open(1, 2);
    fixture.detectChanges();

    expect(api.getEntry).toHaveBeenCalledWith(1, 2);
    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-body"]')?.textContent)
      .toContain('Praise belongs to Allah');
    // The attribution element renders the i18n key under TranslateNoOpLoader;
    // confirm it is present (the actual translated string is verified visually
    // and via i18n linting, not in unit tests).
    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-attribution"]')).toBeTruthy();
    expect(fixture.componentInstance.entry()?.source.name).toBe('Tafsir Ibn Kathir');
  });

  it('shows the not-available message on 404', () => {
    api.getEntry.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const fixture = TestBed.createComponent(TafsirPanelComponent);
    fixture.detectChanges();

    panel.open(1, 5);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-not-available"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-body"]')).toBeNull();
  });

  it('shows the loading state while the request is in flight', () => {
    // A Subject that never emits keeps the component in `loading`.
    api.getEntry.and.returnValue(new Subject<TafsirEntry>());
    const fixture = TestBed.createComponent(TafsirPanelComponent);
    fixture.detectChanges();

    panel.open(1, 2);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-loading"]')).toBeTruthy();
  });

  it('updates the panel content when a new Ayah is selected while open', () => {
    api.getEntry.and.returnValue(of(ENTRY));
    const fixture = TestBed.createComponent(TafsirPanelComponent);
    fixture.detectChanges();

    panel.open(1, 2);
    fixture.detectChanges();
    expect(api.getEntry).toHaveBeenCalledWith(1, 2);

    api.getEntry.and.returnValue(of({ ...ENTRY, numberInSurah: 3, body: 'Different body' }));
    panel.open(1, 3);
    fixture.detectChanges();

    expect(api.getEntry).toHaveBeenCalledWith(1, 3);
    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-body"]')?.textContent)
      .toContain('Different body');
  });

  it('close() returns the panel to the idle state', () => {
    api.getEntry.and.returnValue(of(ENTRY));
    const fixture = TestBed.createComponent(TafsirPanelComponent);
    fixture.detectChanges();
    panel.open(1, 2);
    fixture.detectChanges();

    fixture.componentInstance.close();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="tafsir-panel"]')).toBeNull();
  });
});
