import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

import { applySlow3G } from './helpers/throttle';

test.describe('US1 — Read the Quran in Arabic with English translation', () => {
  test('Surah list shows 114 entries', async ({ page, context }) => {
    await applySlow3G(context);
    await page.goto('/surahs');
    const items = page.locator('.surah-list__item');
    await expect(items).toHaveCount(114);
  });

  test('Al-Fatiha renders 7 ayahs bilingually with correct lang attributes', async ({ page, context }) => {
    await applySlow3G(context);
    await page.goto('/surahs/1');

    const ayahs = page.locator('app-ayah .ayah');
    await expect(ayahs).toHaveCount(7);

    const firstArabic = page.locator('app-ayah .ayah__arabic').first();
    await expect(firstArabic).toHaveAttribute('lang', 'ar');
    await expect(firstArabic).toHaveAttribute('dir', 'rtl');
    await expect(firstArabic).toHaveClass(/mushaf-font/);

    const firstTranslation = page.locator('app-ayah .ayah__translation').first();
    await expect(firstTranslation).toHaveAttribute('lang', 'en');
  });

  test('next-surah navigation lands on Al-Baqarah', async ({ page }) => {
    await page.goto('/surahs/1');
    await page.getByRole('button', { name: /next/i }).click();
    await expect(page).toHaveURL(/\/surahs\/2$/);
  });

  test('jump-to-ayah scrolls to the requested ayah', async ({ page }) => {
    await page.goto('/surahs/1');
    await page.getByLabel(/jump to ayah/i).fill('5');
    await page.getByRole('button', { name: /^Go$/ }).click();
    const target = page.locator('#ayah-5');
    await expect(target).toBeVisible();
  });

  test('axe-core: no critical accessibility violations on the surah reader', async ({ page }) => {
    await page.goto('/surahs/1');
    const result = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();
    const critical = result.violations.filter((v) => v.impact === 'critical');
    expect(critical).toEqual([]);
  });
});
