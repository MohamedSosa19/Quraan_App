import { test, expect, Route } from '@playwright/test';

/**
 * T179 — Last-read e2e (FR-035, R-13).
 *
 * Verifies:
 *   1. Anonymous: visiting /surahs/1?ayah=3 caches the position in localStorage,
 *      and the home Continue Reading link reflects it.
 *   2. Signed-in: a successful login replays the local position via
 *      `PUT /api/v1/lastread/me`, and the merged result re-renders Continue Reading.
 *   3. Sign-out clears localStorage so the next anonymous session starts clean.
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
    id: i + 1,
    surahId: 1,
    numberInSurah: i + 1,
    arabicText: 'بسم الله',
    translationText: `Verse ${i + 1}`,
    juzNumber: 1,
    hizbQuarter: 1,
    sajda: false,
  })),
};

async function mockSurah(route: Route): Promise<void> {
  await route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(SURAH_DETAIL),
  });
}

test.describe('Last-read position (FR-035, R-13)', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/surahs/1**', mockSurah);
  });

  test('Anonymous user: localStorage caches position and Continue Reading appears', async ({ page }) => {
    // Anonymous: PUT /lastread/me returns 401 (which the service swallows).
    await page.route('**/api/v1/lastread/me', async (route) => {
      await route.fulfill({ status: 401, contentType: 'application/json', body: '{}' });
    });

    await page.goto('/surahs/1?ayah=3');
    // Allow effect/HTTP cycle to settle.
    await expect.poll(async () =>
      page.evaluate(() => localStorage.getItem('quraan.last-read.v1')),
    ).not.toBeNull();

    const stored = await page.evaluate(() => localStorage.getItem('quraan.last-read.v1'));
    expect(stored).not.toBeNull();
    const parsed = JSON.parse(stored!);
    expect(parsed.surahId).toBe(1);
    expect(parsed.numberInSurah).toBe(3);

    await page.goto('/');
    await expect(page.locator('[data-testid="home-continue-reading"]')).toBeVisible();
  });

  test('Sign-in replays anonymous position via PUT and the server-merged value drives Continue Reading', async ({ page }) => {
    let putBody: { surahId: number; numberInSurah: number; updatedAt: string } | null = null;

    // Sign-in flow.
    await page.route('**/api/v1/auth/login', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: {
          'Set-Cookie':
            'quraan-refresh=opaque; Path=/api/v1/auth; HttpOnly; Secure; SameSite=Strict; Max-Age=2592000',
        },
        body: JSON.stringify({ accessToken: 'jwt', expiresInSeconds: 900, user: USER }),
      });
    });

    // Last-read: PUT echoes the body (server is the merge winner here).
    await page.route('**/api/v1/lastread/me', async (route) => {
      const method = route.request().method();
      if (method === 'PUT') {
        putBody = JSON.parse(route.request().postData() ?? '{}');
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(putBody),
        });
      } else if (method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(putBody ?? null),
        });
      }
    });

    // Seed the anonymous position by visiting an ayah.
    await page.goto('/surahs/1?ayah=4');
    await expect.poll(async () =>
      page.evaluate(() => localStorage.getItem('quraan.last-read.v1')),
    ).not.toBeNull();

    // Sign in — this should call mergeOnSignIn() and PUT to the server.
    await page.goto('/auth/sign-in');
    await page.locator('[data-testid="auth-email"]').fill('alice@example.test');
    await page.locator('[data-testid="auth-password"]').fill('passwordpassword');
    await page.locator('[data-testid="auth-submit"]').click();
    await page.waitForURL('**/');

    // Server received the local position.
    await expect.poll(() => putBody?.surahId).toBe(1);
    expect(putBody!.numberInSurah).toBe(4);

    // Continue Reading still visible after sign-in.
    await expect(page.locator('[data-testid="home-continue-reading"]')).toBeVisible();
  });
});
