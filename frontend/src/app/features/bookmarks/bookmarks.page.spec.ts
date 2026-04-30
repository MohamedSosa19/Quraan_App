import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateLoader, TranslateNoOpLoader } from '@ngx-translate/core';
import { of } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { Bookmark, BookmarksApiService } from './bookmarks-api.service';
import { BookmarksPage } from './bookmarks.page';
import { BookmarksStore } from './bookmarks.store';

const ITEM: Bookmark = {
  id: 'b1',
  ayahId: 1,
  surahId: 1,
  numberInSurah: 1,
  createdAt: '2026-04-30T00:00:00Z',
};

describe('BookmarksPage', () => {
  let api: jasmine.SpyObj<BookmarksApiService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<BookmarksApiService>('BookmarksApiService', ['list', 'create', 'remove']);
    api.list.and.returnValue(of({ items: [ITEM], page: 1, pageSize: 50, total: 1 }));
    api.remove.and.returnValue(of(void 0));

    const auth = { isAuthenticated: () => true } as Partial<AuthService>;

    await TestBed.configureTestingModule({
      imports: [BookmarksPage],
      providers: [
        provideRouter([]),
        provideTranslateService({
          defaultLanguage: 'en',
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader },
        }),
        { provide: BookmarksApiService, useValue: api },
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();

    // Reset the store between tests (it's a root-singleton).
    TestBed.inject(BookmarksStore).clear();
  });

  it('renders an empty state when the user has no bookmarks', async () => {
    api.list.and.returnValue(of({ items: [], page: 1, pageSize: 50, total: 0 }));
    const fixture = TestBed.createComponent(BookmarksPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="bookmarks-empty"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="bookmarks-list"]')).toBeNull();
  });

  it('lists existing bookmarks', async () => {
    const fixture = TestBed.createComponent(BookmarksPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.list).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[data-testid="bookmarks-list"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="bookmark-item"]')).toBeTruthy();
  });

  it('removes a bookmark on click', async () => {
    const fixture = TestBed.createComponent(BookmarksPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const removeBtn: HTMLButtonElement = fixture.nativeElement.querySelector('[data-testid="bookmark-remove"]');
    removeBtn.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.remove).toHaveBeenCalledWith('b1');
    expect(fixture.nativeElement.querySelector('[data-testid="bookmark-item"]')).toBeNull();
  });
});
