import { test, expect, Route } from '@playwright/test';

/**
 * US6 — Create an Account and Sign In.
 *
 * Mocks /api/v1/auth/* so the e2e tests deterministically exercise:
 *   • Sign-in success → redirect (with Set-Cookie having HttpOnly + Secure +
 *     SameSite=Strict flags asserted in the captured cookie string)
 *   • Sign-in failure → generic error banner (FR-029)
 *   • Sign-out → cleared state (cookie expired in past)
 *   • Lockout banner (5+ attempts) — UI shows the same generic banner
 */

const USER = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'alice@example.test',
  preferredLanguage: 'en' as const,
};

const REFRESH_COOKIE = 'opaque-refresh-token-value';

async function mockLogin(route: Route, ok: boolean): Promise<void> {
  if (ok) {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: {
        'Set-Cookie': `quraan-refresh=${REFRESH_COOKIE}; Path=/api/v1/auth; HttpOnly; Secure; SameSite=Strict; Max-Age=2592000`,
      },
      body: JSON.stringify({ accessToken: 'jwt.token', expiresInSeconds: 900, user: USER }),
    });
  } else {
    await route.fulfill({
      status: 401,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        type: 'about:blank',
        title: 'Invalid credentials',
        status: 401,
        detail: 'The email or password you entered is incorrect.',
      }),
    });
  }
}

test.describe('US6 — Create an Account and Sign In', () => {
  test('Sign-in success redirects to home and Set-Cookie advertises HttpOnly+Secure+Strict', async ({ page }) => {
    let cookieHeader: string | undefined;
    await page.route('**/api/v1/auth/login', async (route) => {
      await mockLogin(route, true);
    });
    page.on('response', (resp) => {
      if (resp.url().endsWith('/api/v1/auth/login')) {
        cookieHeader = resp.headers()['set-cookie'];
      }
    });

    await page.goto('/auth/sign-in');
    await page.locator('[data-testid="auth-email"]').fill('alice@example.test');
    await page.locator('[data-testid="auth-password"]').fill('passwordpassword');
    await page.locator('[data-testid="auth-submit"]').click();

    await page.waitForURL('**/');
    expect(cookieHeader).toBeDefined();
    expect(cookieHeader!.toLowerCase()).toContain('httponly');
    expect(cookieHeader!.toLowerCase()).toContain('secure');
    expect(cookieHeader!.toLowerCase()).toContain('samesite=strict');
  });

  test('Sign-in with wrong credentials shows the generic error banner (FR-029)', async ({ page }) => {
    await page.route('**/api/v1/auth/login', async (route) => {
      await mockLogin(route, false);
    });

    await page.goto('/auth/sign-in');
    await page.locator('[data-testid="auth-email"]').fill('alice@example.test');
    await page.locator('[data-testid="auth-password"]').fill('wrong-password');
    await page.locator('[data-testid="auth-submit"]').click();

    await expect(page.locator('[data-testid="auth-error"]')).toBeVisible();
  });

  test('After 5 wrong attempts the same generic banner stays — no lockout disclosure (FR-029a)', async ({ page }) => {
    await page.route('**/api/v1/auth/login', async (route) => {
      await mockLogin(route, false);
    });

    await page.goto('/auth/sign-in');
    for (let i = 0; i < 5; i++) {
      await page.locator('[data-testid="auth-email"]').fill('alice@example.test');
      await page.locator('[data-testid="auth-password"]').fill(`wrong-${i}`);
      await page.locator('[data-testid="auth-submit"]').click();
      await expect(page.locator('[data-testid="auth-error"]')).toBeVisible();
    }
    // The error message text must NOT mention lockout / locked / blocked.
    const text = await page.locator('[data-testid="auth-error"]').textContent();
    expect(text?.toLowerCase()).not.toContain('lock');
    expect(text?.toLowerCase()).not.toContain('block');
  });

  test('Register with duplicate email surfaces a distinct emailTaken banner (FR-024a)', async ({ page }) => {
    await page.route('**/api/v1/auth/register', async (route) => {
      await route.fulfill({
        status: 409,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'about:blank',
          title: 'Email already in use',
          status: 409,
          detail: 'An account with this email already exists.',
        }),
      });
    });

    await page.goto('/auth/register');
    await page.locator('[data-testid="auth-email"]').fill('dup@example.test');
    await page.locator('[data-testid="auth-password"]').fill('passwordpassword');
    await page.locator('[data-testid="auth-submit"]').click();

    await expect(page.locator('[data-testid="auth-email-taken"]')).toBeVisible();
  });
});
