import type { BrowserContext, CDPSession } from '@playwright/test';

/**
 * Apply Slow-3G network conditions to a Chromium context via the DevTools
 * Protocol. Mirrors Chrome DevTools' "Slow 3G" preset (≈400 kbps down,
 * 400 ms RTT) per spec.md §Network performance baseline.
 */
export async function applySlow3G(context: BrowserContext): Promise<CDPSession | null> {
  const page = context.pages()[0] ?? (await context.newPage());
  // Only Chromium supports CDP; webkit/firefox tests must skip these helpers.
  if (context.browser()?.browserType().name() !== 'chromium') return null;
  const cdp = await context.newCDPSession(page);
  await cdp.send('Network.enable');
  await cdp.send('Network.emulateNetworkConditions', {
    offline: false,
    latency: 400,
    downloadThroughput: (400 * 1024) / 8,
    uploadThroughput: (400 * 1024) / 8,
  });
  return cdp;
}
