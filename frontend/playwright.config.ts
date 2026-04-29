import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: true,
  forbidOnly: !!process.env["CI"],
  retries: process.env["CI"] ? 2 : 0,
  workers: process.env["CI"] ? 2 : undefined,
  reporter: process.env["CI"]
    ? [["github"], ["html", { open: "never" }]]
    : [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: process.env["E2E_BASE_URL"] ?? "http://localhost:4200",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [
    {
      name: "chromium-en",
      use: { ...devices["Desktop Chrome"], locale: "en-US" },
    },
    {
      name: "chromium-ar",
      use: { ...devices["Desktop Chrome"], locale: "ar-EG" },
    },
    {
      name: "mobile-chromium-slow3g",
      use: {
        ...devices["Pixel 5"],
        // Spec.md §Network performance baseline (SC-001/003/009): Slow 3G
        contextOptions: {
          // Network throttling applied via per-test page.route() helpers in
          // tests/e2e/helpers/throttle.ts when timing assertions are required.
        },
      },
    },
    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] },
    },
  ],
  webServer: process.env["E2E_NO_SERVER"]
    ? undefined
    : {
        command: "npm run start",
        url: "http://localhost:4200",
        reuseExistingServer: !process.env["CI"],
        timeout: 120_000,
      },
});
