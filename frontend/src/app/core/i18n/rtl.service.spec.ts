import { TestBed } from '@angular/core/testing';
import { provideTranslateService, TranslateLoader, TranslateNoOpLoader } from '@ngx-translate/core';

import { LanguageService } from './language.service';
import { RtlService } from './rtl.service';

describe('RtlService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('dir');
    document.documentElement.removeAttribute('lang');

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService({
          defaultLanguage: 'en',
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader },
        }),
      ],
    });
  });

  afterEach(() => localStorage.clear());

  it('sets dir=ltr / lang=en initially when language is en', () => {
    TestBed.inject(RtlService);
    TestBed.flushEffects();
    expect(document.documentElement.getAttribute('dir')).toBe('ltr');
    expect(document.documentElement.getAttribute('lang')).toBe('en');
  });

  it('flips dir to rtl when LanguageService switches to ar', () => {
    const lang = TestBed.inject(LanguageService);
    TestBed.inject(RtlService);
    TestBed.flushEffects();

    lang.setLanguage('ar');
    TestBed.flushEffects();
    expect(document.documentElement.getAttribute('dir')).toBe('rtl');
    expect(document.documentElement.getAttribute('lang')).toBe('ar');
  });

  it('flips back to ltr when switching ar → en', () => {
    const lang = TestBed.inject(LanguageService);
    TestBed.inject(RtlService);
    lang.setLanguage('ar');
    TestBed.flushEffects();
    lang.setLanguage('en');
    TestBed.flushEffects();
    expect(document.documentElement.getAttribute('dir')).toBe('ltr');
  });
});
