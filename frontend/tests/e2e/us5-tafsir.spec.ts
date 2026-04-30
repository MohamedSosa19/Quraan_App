import { test, expect, Route } from '@playwright/test';

/**
 * US5 — Read Tafsir for an Ayah.
 *
 * Mocks `/api/v1/tafsir/{surahId}/{n}` so the e2e exercises the Surah-2 has
 * Tafsir / Ayah-5 missing path required by FR-023 deterministically.
 */

const TAFSIR_2 = {
  surahId: 1,
  numberInSurah: 2,
  source: {
    code: 'ibn-kathir-en',
    name: 'Tafsir Ibn Kathir (Mubarakpuri abridged)',
    attribution: 'Public-domain English digest by Mawlana Safi-ur-Rahman Mubarakpuri.',
  },
  body: 'All praise belongs to Allah, the Lord of all the worlds. He is the Sustainer and Cherisher of every created thing.',
};

const TAFSIR_3 = {
  ...TAFSIR_2,
  numberInSurah: 3,
  body: 'The Most Gracious, the Most Merciful — His mercy encompasses everything in existence.',
};

async function mockTafsir(route: Route): Promise<void> {
  const url = new URL(route.request().url());
  // Path: /api/v1/tafsir/{surahId}/{n}
  const parts = url.pathname.split('/').filter(Boolean);
  const n = Number(parts[parts.length - 1]);
  if (n === 2) {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(TAFSIR_2) });
  } else if (n === 3) {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(TAFSIR_3) });
  } else {
    await route.fulfill({
      status: 404,
      contentType: 'application/problem+json',
      body: JSON.stringify({ type: 'about:blank', title: 'Tafsir not available', status: 404 }),
    });
  }
}

test.describe('US5 — Read Tafsir for an Ayah', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/tafsir/**', mockTafsir);
  });

  test('Opening Tafsir on Ayah 2 shows body + attribution', async ({ page }) => {
    await page.goto('/surahs/1');
    const tafsirButtons = page.locator('[data-testid="ayah-tafsir-button"]');
    await tafsirButtons.nth(1).click(); // Ayah 2 (index 1)

    const panel = page.locator('[data-testid="tafsir-panel"]');
    await expect(panel).toBeVisible();
    await expect(panel.locator('[data-testid="tafsir-body"]')).toContainText('All praise belongs to Allah');
    await expect(panel.locator('[data-testid="tafsir-attribution"]')).toBeVisible();
  });

  test('Selecting a different Ayah while panel is open updates content', async ({ page }) => {
    await page.goto('/surahs/1');
    const tafsirButtons = page.locator('[data-testid="ayah-tafsir-button"]');

    await tafsirButtons.nth(1).click();
    await expect(page.locator('[data-testid="tafsir-body"]')).toContainText('All praise');

    await tafsirButtons.nth(2).click(); // Ayah 3
    await expect(page.locator('[data-testid="tafsir-body"]')).toContainText('Most Gracious');
  });

  test('Opening Tafsir on Ayah without entry shows "not available"', async ({ page }) => {
    await page.goto('/surahs/1');
    const tafsirButtons = page.locator('[data-testid="ayah-tafsir-button"]');
    await tafsirButtons.nth(4).click(); // Ayah 5 — mocked to 404

    await expect(page.locator('[data-testid="tafsir-not-available"]')).toBeVisible();
    await expect(page.locator('[data-testid="tafsir-body"]')).not.toBeVisible();
  });

  test('Close button dismisses the panel', async ({ page }) => {
    await page.goto('/surahs/1');
    await page.locator('[data-testid="ayah-tafsir-button"]').nth(1).click();
    await expect(page.locator('[data-testid="tafsir-panel"]')).toBeVisible();

    await page.locator('[data-testid="tafsir-close"]').click();
    await expect(page.locator('[data-testid="tafsir-panel"]')).not.toBeVisible();
  });
});
