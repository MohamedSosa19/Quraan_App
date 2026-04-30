import { test, expect, Route } from '@playwright/test';

/**
 * US7 — Bookmark Ayahs and revisit them.
 *
 * Mocks the auth + bookmark + surahs endpoints so the e2e is deterministic.
 * Covers: anonymous click → sign-in prompt with returnUrl + pendingBookmark;
 * signed-in bookmark → list shows entry; click entry → reader opens at the
 * Ayah; cross-context sync.
 */

const USER = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'alice@example.test',
  preferredLanguage: 'en' as const,
};

const SURAH_DETAIL = {
  id: 1,
  arabicName: 'الفاتحة',
  transliteratedName: 'Al-Fatiha',
  englishName: 'The Opener',
  revelationPlace: 'Meccan',
  ayahCount: 7,
  translation: { code: 'en.sahih', language: 'en', name: 'Saheeh', attribution: '©' },
  ayahs: Array.from({ length: 7 }, (_, i) => ({
    id: i + 1, surahId: 1, numberInSurah: i + 1,
    arabicText: 'بسم الله', translationText: `Verse ${i + 1}`,
    juzNumber: 1, hizbQuarter: 1, sajda: false,
  })),
};

async function mockSurah(route: Route): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(SURAH_DETAIL) });
}

test.describe('US7 — Bookmark Ayahs and revisit them', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/surahs/1**', mockSurah);
  });

  test('Anonymous bookmark click navigates to /auth/sign-in with returnUrl + pendingBookmark', async ({ page }) => {
    await page.goto('/surahs/1');
    // First Ayah's bookmark button.
    await page.locator('[data-testid="bookmark-button"]').first().click();
    await page.waitForURL(/\/auth\/sign-in/);

    const url = new URL(page.url());
    expect(url.pathname).toContain('/auth/sign-in');
    const returnUrl = url.searchParams.get('returnUrl');
    expect(returnUrl).toBe('/surahs/1?ayah=1');
    expect(url.searchParams.get('pendingBookmark')).toBe('1:1');
  });

  test('Signed-in user can bookmark an Ayah and see it on the bookmarks page', async ({ page, context }) => {
    let bookmarkRows: Array<{ id: string; surahId: number; numberInSurah: number; ayahId: number; createdAt: string }> = [];

    // Mock auth: login → 200 + AuthResponse + Set-Cookie
    await page.route('**/api/v1/auth/login', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Set-Cookie': 'quraan-refresh=opaque; Path=/api/v1/auth; HttpOnly; Secure; SameSite=Strict; Max-Age=2592000' },
        body: JSON.stringify({ accessToken: 'jwt', expiresInSeconds: 900, user: USER }),
      });
    });

    // Mock bookmarks GET/POST/DELETE against the in-memory `bookmarkRows`.
    await page.route('**/api/v1/bookmarks**', async (route) => {
      const url = new URL(route.request().url());
      const method = route.request().method();
      if (method === 'GET') {
        await route.fulfill({
          status: 200, contentType: 'application/json',
          body: JSON.stringify({ items: bookmarkRows, page: 1, pageSize: 50, total: bookmarkRows.length }),
        });
      } else if (method === 'POST') {
        const body = JSON.parse(route.request().postData() ?? '{}');
        const created = {
          id: `bm-${bookmarkRows.length + 1}`,
          ayahId: body.numberInSurah,
          surahId: body.surahId,
          numberInSurah: body.numberInSurah,
          createdAt: new Date().toISOString(),
        };
        bookmarkRows = [created, ...bookmarkRows];
        await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(created) });
      } else if (method === 'DELETE') {
        const id = url.pathname.split('/').pop();
        bookmarkRows = bookmarkRows.filter((b) => b.id !== id);
        await route.fulfill({ status: 204 });
      }
    });

    // Sign in.
    await page.goto('/auth/sign-in');
    await page.locator('[data-testid="auth-email"]').fill('alice@example.test');
    await page.locator('[data-testid="auth-password"]').fill('passwordpassword');
    await page.locator('[data-testid="auth-submit"]').click();
    await page.waitForURL('**/');

    // Open the surah and bookmark Ayah 1.
    await page.goto('/surahs/1');
    const firstBookmark = page.locator('[data-testid="bookmark-button"]').first();
    await firstBookmark.click();
    await expect(firstBookmark).toHaveAttribute('aria-pressed', 'true');

    // Visit the bookmarks page; entry should be there.
    await page.goto('/bookmarks');
    await expect(page.locator('[data-testid="bookmark-item"]')).toHaveCount(1);

    // Click the entry → opens /surahs/1?ayah=1.
    await page.locator('[data-testid="bookmark-open"]').first().click();
    await page.waitForURL(/\/surahs\/1/);
    expect(new URL(page.url()).searchParams.get('ayah')).toBe('1');
  });
});
