import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * US2 — Bilingual UI with RTL support.
 *
 * Coverage:
 *   - Toggle from EN (LTR) → AR (RTL) updates `<html dir>` and `<html lang>`
 *   - No full page reload (network shows zero document navigations after the click)
 *   - Quran view's Arabic text remains lang="ar" / dir="rtl" / mushaf-font (FR-009)
 *   - Choice persists across reloads (localStorage round-trip)
 *   - SC-009 timing: dir flip happens within 500 ms
 *   - SC-007 axe-core: no critical violations on either locale
 *
 * The audio-continuity clause from SC-009 is asserted in US3 once
 * AudioPlayerService lands (T115); kept as a TODO here.
 */

test.describe('US2 — Use the App in Arabic or English with RTL Support', () => {
  test('toggle EN → AR flips dir within 500 ms with no document navigation', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('html')).toHaveAttribute('dir', 'ltr');

    // Count document navigations to assert "no reload".
    let navigations = 0;
    page.on('framenavigated', (frame) => {
      if (frame === page.mainFrame()) navigations++;
    });

    const before = await page.evaluate(() => performance.now());
    await page.locator('[data-testid="language-toggle"]').click();
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    const after = await page.evaluate(() => performance.now());

    expect(after - before).toBeLessThanOrEqual(500);
    expect(navigations).toBe(0);
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar');
  });

  test('language choice persists across page reload', async ({ page, context }) => {
    await page.goto('/');
    await page.locator('[data-testid="language-toggle"]').click();
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');

    await page.reload();
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');

    const stored = await context.storageState();
    const origin = stored.origins.find((o) => o.localStorage.some((kv) => kv.name === 'quraan.lang'));
    expect(origin).toBeDefined();
  });

  test('Quran Arabic text always renders lang=ar regardless of UI locale', async ({ page }) => {
    await page.goto('/surahs/1');
    const arabic = page.locator('app-ayah .ayah__arabic').first();
    await expect(arabic).toHaveAttribute('lang', 'ar');
    await expect(arabic).toHaveAttribute('dir', 'rtl');
    await expect(arabic).toHaveClass(/mushaf-font/);

    // Switch UI to Arabic — Arabic Quran text should still be lang=ar (FR-009).
    await page.locator('[data-testid="language-toggle"]').click();
    await expect(arabic).toHaveAttribute('lang', 'ar');
    await expect(arabic).toHaveClass(/mushaf-font/);

    // English translation continues to render lang=en even when UI is Arabic.
    const translation = page.locator('app-ayah .ayah__translation').first();
    await expect(translation).toHaveAttribute('lang', 'en');
  });

  test('axe-core: no critical violations in EN locale', async ({ page }) => {
    await page.goto('/');
    const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
    expect(result.violations.filter((v) => v.impact === 'critical')).toEqual([]);
  });

  test('axe-core: no critical violations in AR locale', async ({ page }) => {
    await page.goto('/');
    await page.locator('[data-testid="language-toggle"]').click();
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
    expect(result.violations.filter((v) => v.impact === 'critical')).toEqual([]);
  });
});
