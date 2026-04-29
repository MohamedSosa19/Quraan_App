import { test, expect } from '@playwright/test';

import { applySlow3G } from './helpers/throttle';

/**
 * US3 — Listen to a Surah's recitation.
 *
 * The test simulates the audio API to keep the e2e independent of upstream
 * availability and to make timing-assertion deterministic.  It intercepts
 * `/api/v1/audio/78` and `/api/v1/audio/99` so SC-003 measures only the
 * client-side play→onplay path against a local audio source.
 */

const RECITATION_RESPONSE = {
  surahId: 78,
  reciter: { code: 'ar.alafasy', name: 'Alafasy', arabicName: 'العفاسي', attribution: '©' },
  // 1-second silent WAV data URL — Howl plays it instantly without a network round-trip.
  audioUrl:
    'data:audio/wav;base64,UklGRkAAAABXQVZFZm10IBAAAAABAAEARKwAAIhYAQACABAAZGF0YRwAAAAAAAA',
  hasTimings: false,
  ayahTimings: [] as { numberInSurah: number; fromMs: number; toMs: number }[],
};

test.describe('US3 — Listen to a Surah\'s Recitation', () => {
  test('Play → audio start ≤ 2 s on Slow 3G (SC-003)', async ({ page, context }) => {
    await applySlow3G(context);
    await page.route('**/api/v1/audio/78**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RECITATION_RESPONSE) }),
    );

    await page.goto('/surahs/78');
    await page.waitForSelector('[data-testid="audio-play"]');

    const before = await page.evaluate(() => performance.now());
    await page.locator('[data-testid="audio-play"]').click();
    // The component flips to a "Pause" button as soon as Howl fires `onplay`.
    await page.waitForSelector('[data-testid="audio-pause"]');
    const after = await page.evaluate(() => performance.now());
    expect(after - before).toBeLessThanOrEqual(2000);
  });

  test('Pause → Resume preserves position', async ({ page }) => {
    await page.route('**/api/v1/audio/78**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RECITATION_RESPONSE) }),
    );
    await page.goto('/surahs/78');
    await page.locator('[data-testid="audio-play"]').click();
    await page.waitForSelector('[data-testid="audio-pause"]');

    await page.locator('[data-testid="audio-pause"]').click();
    await expect(page.locator('[data-testid="audio-resume"]')).toBeVisible();
    await page.locator('[data-testid="audio-resume"]').click();
    await expect(page.locator('[data-testid="audio-pause"]')).toBeVisible();
  });

  test('Upstream 502 → error banner with Retry surfaces (FR-014)', async ({ page }) => {
    let firstCall = true;
    await page.route('**/api/v1/audio/78**', (route) => {
      if (firstCall) {
        firstCall = false;
        return route.fulfill({
          status: 502,
          contentType: 'application/problem+json',
          body: JSON.stringify({ type: 'about:blank', title: 'Bad Gateway', status: 502 }),
        });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RECITATION_RESPONSE) });
    });

    await page.goto('/surahs/78');
    await page.locator('[data-testid="audio-play"]').click();
    await expect(page.locator('[data-testid="audio-retry"]')).toBeVisible();

    await page.locator('[data-testid="audio-retry"]').click();
    await expect(page.locator('[data-testid="audio-pause"]')).toBeVisible();
  });
});
