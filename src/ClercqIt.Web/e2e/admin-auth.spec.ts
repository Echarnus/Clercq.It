import { test, expect } from '@playwright/test';

test.describe('Admin Authentication', () => {
  test.beforeEach(async ({ page }) => {
    // Clear localStorage before each test
    await page.goto('/admin');
    await page.evaluate(() => {
      localStorage.removeItem('admin_token');
      localStorage.removeItem('admin_user');
    });
  });

  test('should display login page', async ({ page }) => {
    await page.goto('/admin');

    // Check that login form is displayed - use text content matching
    await expect(page.getByText('Admin Login')).toBeVisible();
    await expect(page.locator('input#username')).toBeVisible();
    await expect(page.locator('input#password')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test('should show error with invalid credentials', async ({ page }) => {
    await page.goto('/admin');

    // Fill in invalid credentials
    await page.fill('input#username', 'wronguser');
    await page.fill('input#password', 'wrongpassword');

    // Click sign in
    await page.click('button[type="submit"]');

    // Wait for error message
    await expect(page.locator('[role="alert"], .text-destructive, [class*="AlertDescription"]')).toBeVisible({ timeout: 10000 });
  });

  test('should login with valid credentials', async ({ page }) => {
    await page.goto('/admin');

    // Fill in valid credentials
    await page.fill('input#username', 'admin');
    await page.fill('input#password', 'admin123');

    // Click sign in
    await page.click('button[type="submit"]');

    // Wait for redirect to dashboard
    await page.waitForURL('**/admin/dashboard', { timeout: 15000 });

    // Verify dashboard is displayed
    await expect(page.locator('text=Clercq.It Admin')).toBeVisible();
  });

  test('should persist session after login', async ({ page }) => {
    // First login
    await page.goto('/admin');
    await page.fill('input#username', 'admin');
    await page.fill('input#password', 'admin123');
    await page.click('button[type="submit"]');

    // Wait for dashboard
    await page.waitForURL('**/admin/dashboard', { timeout: 15000 });

    // Reload page
    await page.reload();

    // Should still be on dashboard
    await expect(page.locator('text=Clercq.It Admin')).toBeVisible();
  });

  test('should logout successfully', async ({ page }) => {
    // Login first
    await page.goto('/admin');
    await page.fill('input#username', 'admin');
    await page.fill('input#password', 'admin123');
    await page.click('button[type="submit"]');

    // Wait for dashboard
    await page.waitForURL('**/admin/dashboard', { timeout: 15000 });

    // Click logout button
    await page.click('button:has-text("Logout")');

    // Should redirect to login page
    await page.waitForURL('**/admin', { timeout: 10000 });
    await expect(page.getByText('Admin Login')).toBeVisible();
  });

  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Try to access dashboard directly
    await page.goto('/admin/dashboard');

    // Should redirect to login
    await page.waitForURL('**/admin', { timeout: 10000 });
    await expect(page.locator('input#username')).toBeVisible();
  });
});
