import { test, expect } from "@playwright/test";
import { signInAsAdmin } from "./auth";

// The admin side menu (issue #449): groups, one URL per section, phone drawer.
test.describe("Admin navigation", () => {
  test("opens a section from the menu and marks it current", async ({ page }) => {
    await signInAsAdmin(page);
    const menu = page.getByRole("navigation", { name: "Admin" });

    await menu.getByRole("link", { name: "Projects" }).click();

    await page.waitForURL("**/admin/dashboard/projects");
    await expect(menu.getByRole("link", { name: "Projects" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    await expect(page.getByRole("heading", { name: "Project Management" })).toBeVisible();
  });

  test("keeps the section after a reload", async ({ page }) => {
    await signInAsAdmin(page);
    await page
      .getByRole("navigation", { name: "Admin" })
      .getByRole("link", { name: "Settings" })
      .click();
    await page.waitForURL("**/admin/dashboard/settings");

    await page.reload();

    await page.waitForURL("**/admin/dashboard/settings", { timeout: 20000 });
    await expect(page.getByRole("heading", { name: "System Settings" })).toBeVisible();
  });

  test("opens the menu as a drawer on a phone", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await signInAsAdmin(page);

    await page.getByRole("button", { name: "Open admin menu" }).click();
    const drawer = page.getByRole("dialog", { name: "Admin menu" });
    await drawer.getByRole("link", { name: "Visitors" }).click();

    await page.waitForURL("**/admin/dashboard/visitors");
    await expect(drawer).toBeHidden();
  });
});
