import { test, expect } from "@playwright/test";
import { signIn, signInAsAdmin } from "./auth";

test.describe("Admin Authentication", () => {
  test("should display the sign-in page without a password field", async ({ page }) => {
    await page.goto("/admin");

    await expect(page.getByText("Admin Login")).toBeVisible();
    await expect(page.getByRole("button", { name: "Sign In" })).toBeVisible();
    await expect(page.locator("input[type=password]")).toHaveCount(0);
  });

  test("should show the Keycloak error with invalid credentials", async ({ page }) => {
    await signIn(page, "wronguser", "wrongpassword");

    await expect(page.locator("#input-error, .kc-feedback-text, [id*=error]").first()).toBeVisible({
      timeout: 10000,
    });
  });

  test("should sign in through Keycloak", async ({ page }) => {
    await signInAsAdmin(page);
  });

  test("should keep the session after a reload", async ({ page }) => {
    await signInAsAdmin(page);

    await page.reload();

    // Tokens are in memory only; the Keycloak session signs in again without a prompt.
    await page.waitForURL("**/admin/dashboard", { timeout: 20000 });
    await expect(page.locator("text=Clercq.It Admin")).toBeVisible();
  });

  test("should logout successfully", async ({ page }) => {
    await signInAsAdmin(page);

    await page.click('button:has-text("Logout")');

    await page.waitForURL("**/admin", { timeout: 10000 });
    await expect(page.getByText("Admin Login")).toBeVisible();
  });

  test("should send unauthenticated users to the Keycloak login", async ({ page }) => {
    await page.goto("/admin/dashboard");

    await page.waitForURL(/\/realms\/clercqit\/protocol\/openid-connect\/auth/, { timeout: 10000 });
    await expect(page.locator("input#username")).toBeVisible();
  });
});
