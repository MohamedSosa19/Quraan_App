import { test, expect } from "@playwright/test";

// Placeholder e2e — replaced by per-user-story specs (us1-read-quran.spec.ts, etc.)
// once Phase 3 begins. Kept so `npm run e2e` is non-empty after Phase 1.

test("homepage loads", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveTitle(/.+/);
});
