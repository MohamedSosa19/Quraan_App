import { test, expect, Route } from '@playwright/test';

/**
 * US4 — Find a Surah or Ayah by Searching.
 *
 * Mocks `/api/v1/search` per query so the e2e is deterministic and decoupled
 * from real seed data. Covers the six US4 acceptance scenarios from spec.md
 * and tasks.md (T123): English Ayah text, Arabic diacritic-insensitive,
 * transliterated Surah name, English Surah name, no-results, and result-click
 * navigation to the matched Ayah.
 */

const empty = (q: string) => ({
  query: q,
  surahMatches: [],
  ayahMatches: [],
  page: 1,
  pageSize: 20,
  totalAyahMatches: 0,
});

const ayahMatch = {
  query: 'mercy',
  surahMatches: [],
  ayahMatches: [
    {
      id: 1,
      surahId: 1,
      numberInSurah: 3,
      arabicText: 'الرَّحْمَٰنِ الرَّحِيمِ',
      translationText: 'The Most Gracious, the Most Merciful.',
      juzNumber: 1,
      hizbQuarter: 1,
      sajda: false,
      matchedIn: 'translation',
      highlightSnippet: 'The Most Gracious, the Most Merciful.',
    },
  ],
  page: 1,
  pageSize: 20,
  totalAyahMatches: 1,
};

const yaseen = {
  query: 'yaseen',
  surahMatches: [
    {
      id: 36,
      arabicName: 'يس',
      transliteratedName: 'Ya-Sin',
      englishName: 'Ya Sin (Yaseen)',
      revelationPlace: 'Meccan',
      ayahCount: 83,
    },
  ],
  ayahMatches: [],
  page: 1,
  pageSize: 20,
  totalAyahMatches: 0,
};

const fatihaArabic = {
  query: 'الفاتحة',
  surahMatches: [
    {
      id: 1,
      arabicName: 'الفاتحة',
      transliteratedName: 'Al-Fatiha',
      englishName: 'The Opening',
      revelationPlace: 'Meccan',
      ayahCount: 7,
    },
  ],
  ayahMatches: [],
  page: 1,
  pageSize: 20,
  totalAyahMatches: 0,
};

const rahmanDiacritics = {
  query: 'الرحمن',
  surahMatches: [],
  ayahMatches: [
    {
      id: 3,
      surahId: 1,
      numberInSurah: 3,
      arabicText: 'الرَّحْمَٰنِ الرَّحِيمِ',
      translationText: 'The Most Gracious, the Most Merciful.',
      juzNumber: 1,
      hizbQuarter: 1,
      sajda: false,
      matchedIn: 'arabic',
      highlightSnippet: 'الرَّحْمَٰنِ الرَّحِيمِ',
    },
  ],
  page: 1,
  pageSize: 20,
  totalAyahMatches: 1,
};

async function mockSearch(route: Route): Promise<void> {
  const url = new URL(route.request().url());
  const q = (url.searchParams.get('q') ?? '').toLowerCase();
  let body: unknown;
  if (q === 'mercy') body = ayahMatch;
  else if (q === 'yaseen') body = yaseen;
  else if (q.includes('الفاتحة')) body = fatihaArabic;
  else if (q.includes('الرحمن')) body = rahmanDiacritics;
  else body = empty(url.searchParams.get('q') ?? '');
  await route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(body),
  });
}

test.describe('US4 — Find a Surah or Ayah by Searching', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/search**', mockSearch);
  });

  test('English Ayah text "mercy" returns translation matches', async ({ page }) => {
    await page.goto('/search');
    await page.locator('[data-testid="search-input"]').fill('mercy');
    await expect(page.locator('[data-testid="search-ayah-matches"]')).toBeVisible();
    await expect(page.locator('[data-testid="search-ayah-result"]').first()).toContainText('Merciful');
  });

  test('Transliterated "Yaseen" returns Surah Ya-Sin', async ({ page }) => {
    await page.goto('/search');
    await page.locator('[data-testid="search-input"]').fill('Yaseen');
    const surahs = page.locator('[data-testid="search-surah-matches"]');
    await expect(surahs).toBeVisible();
    await expect(surahs).toContainText('Ya-Sin');
  });

  test('Arabic Surah name "الفاتحة" returns Al-Fatiha', async ({ page }) => {
    await page.goto('/search');
    await page.locator('[data-testid="search-input"]').fill('الفاتحة');
    const surahs = page.locator('[data-testid="search-surah-matches"]');
    await expect(surahs).toBeVisible();
    await expect(surahs).toContainText('Al-Fatiha');
  });

  test('Arabic "الرحمن" matches diacriticised text per FR-017', async ({ page }) => {
    await page.goto('/search');
    await page.locator('[data-testid="search-input"]').fill('الرحمن');
    const ayahs = page.locator('[data-testid="search-ayah-matches"]');
    await expect(ayahs).toBeVisible();
    // The match snippet preserves diacritics from the source text.
    await expect(ayahs).toContainText('الرَّحْمَٰنِ');
  });

  test('"qwertyuiop" shows the no-results message', async ({ page }) => {
    await page.goto('/search');
    await page.locator('[data-testid="search-input"]').fill('qwertyuiop');
    await expect(page.locator('[data-testid="search-no-results"]')).toBeVisible();
  });

  test('Clicking an Ayah result opens the Surah at that Ayah', async ({ page }) => {
    await page.goto('/search');
    await page.locator('[data-testid="search-input"]').fill('mercy');
    await page.locator('[data-testid="search-ayah-result"]').first().click();
    await expect(page).toHaveURL(/\/surahs\/1\?ayah=3$/);
  });
});
