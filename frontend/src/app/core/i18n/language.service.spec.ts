import { TestBed } from '@angular/core/testing';
import { provideTranslateService, TranslateLoader, TranslateNoOpLoader, TranslateService } from '@ngx-translate/core';

import { LanguageService } from './language.service';

const STORAGE_KEY = 'quraan.lang';

function createTestBed(): void {
  TestBed.configureTestingModule({
    providers: [
      provideTranslateService({
        defaultLanguage: 'en',
        loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader },
      }),
    ],
  });
}

describe('LanguageService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
    createTestBed();
  });

  afterEach(() => localStorage.clear());

  it('defaults to en when no preference is stored', () => {
    spyOnProperty(navigator, 'language', 'get').and.returnValue('en-US');
    const svc = TestBed.inject(LanguageService);
    expect(svc.currentLang()).toBe('en');
  });

  it('honors a previously stored preference', () => {
    localStorage.setItem(STORAGE_KEY, 'ar');
    TestBed.resetTestingModule();
    createTestBed();
    const svc = TestBed.inject(LanguageService);
    expect(svc.currentLang()).toBe('ar');
  });

  it('setLanguage("ar") updates the signal and persists to localStorage', async () => {
    const svc = TestBed.inject(LanguageService);
    svc.setLanguage('ar');
    // Effects run on a microtask boundary in TestBed; flush them.
    TestBed.flushEffects();
    expect(svc.currentLang()).toBe('ar');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('ar');
  });

  it('switching from ar back to en re-persists', () => {
    localStorage.setItem(STORAGE_KEY, 'ar');
    TestBed.resetTestingModule();
    createTestBed();
    const svc = TestBed.inject(LanguageService);
    svc.setLanguage('en');
    TestBed.flushEffects();
    expect(localStorage.getItem(STORAGE_KEY)).toBe('en');
  });

  it('forwards the active language to TranslateService.use', () => {
    const translate = TestBed.inject(TranslateService);
    const useSpy = spyOn(translate, 'use').and.callThrough();
    const svc = TestBed.inject(LanguageService);
    svc.setLanguage('ar');
    TestBed.flushEffects();
    expect(useSpy).toHaveBeenCalledWith('ar');
  });
});
