import { expect, type Page } from "@playwright/test";

// Admin sign-in goes through the Keycloak login page (authorization code + PKCE,
// issue #257). Local realm users have their username as password.
export async function signIn(page: Page, username = "admin", password = username) {
  await page.goto("/admin");
  await page.getByRole("button", { name: "Sign In" }).click();
  await page.waitForURL(/\/realms\/clercqit\/protocol\/openid-connect\/auth/);
  await page.fill("input#username", username);
  await page.fill("input#password", password);
  await page.click("#kc-login");
}

export async function signInAsAdmin(page: Page) {
  await signIn(page);
  await page.waitForURL("**/admin/dashboard", { timeout: 20000 });
  await expect(page.locator("text=Clercq.It Admin")).toBeVisible();
}
