import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import type { User } from "oidc-client-ts";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getUser, signIn, signOut } from "@/lib/auth";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { AdminSectionView } from "./AdminSectionView";
import { AdminShell } from "./AdminShell";

const nav = vi.hoisted(() => ({ pathname: "/admin/dashboard", replace: vi.fn(), push: vi.fn() }));

vi.mock("next/navigation", () => ({
  usePathname: () => nav.pathname,
  useRouter: () => ({ replace: nav.replace, push: nav.push }),
}));

vi.mock("@/lib/auth", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@/lib/auth")>()),
  getUser: vi.fn(),
  signIn: vi.fn(),
  signOut: vi.fn(),
}));

// A signed-in user whose access token carries these roles.
function signedInAs(...roles: string[]) {
  const payload = btoa(JSON.stringify({ preferred_username: "kenneth", roles }));
  vi.mocked(getUser).mockResolvedValue({
    access_token: `x.${payload}.y`,
    profile: {},
  } as unknown as User);
}

function renderSection(key: string, pathname = `/admin/dashboard/${key}`) {
  nav.pathname = pathname;
  render(
    <AdminShell>
      <AdminSectionView sectionKey={key} />
    </AdminShell>,
  );
}

const menu = () => screen.getByRole("navigation", { name: "Admin" });

describe("AdminShell", () => {
  beforeEach(() => {
    nav.replace.mockReset();
    nav.push.mockReset();
  });

  it("AdminShell_NoSession_SignsInAndComesBackToThisSection", async () => {
    // Arrange
    vi.mocked(getUser).mockResolvedValue(null);
    window.history.pushState({}, "", "/admin/dashboard/blogs");

    // Act
    renderSection("blogs");

    // Assert
    await waitFor(() => expect(signIn).toHaveBeenCalledWith("/admin/dashboard/blogs"));
    expect(screen.queryByRole("navigation")).not.toBeInTheDocument();
  });

  it("AdminShell_AllRoles_ShowsGroupsAndHighlightsTheCurrentSection", async () => {
    // Arrange
    signedInAs(
      "Admin.View",
      "Blogs.Contributor",
      "Projects.Contributor",
      "Certifications.Contributor",
    );

    // Act
    renderSection("settings");

    // Assert
    expect(await screen.findByRole("heading", { name: "System Settings" })).toBeVisible();
    for (const group of ["Portfolio", "Statistics", "Administration"]) {
      expect(within(menu()).getByRole("button", { name: group })).toBeVisible();
    }
    expect(within(menu()).getByRole("link", { name: "Settings" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    // MyZen needs its own role and the feature flag.
    expect(within(menu()).queryByRole("link", { name: "MyZen" })).not.toBeInTheDocument();
    // Settings lists the admin roles only.
    expect(
      screen.getByText(
        "Admin.View Blogs.Contributor Projects.Contributor Certifications.Contributor",
      ),
    ).toBeVisible();
  });

  it("AdminShell_OneRole_ShowsOnlyThatSectionAndItsGroup", async () => {
    signedInAs("Blogs.Contributor", "offline_access");

    renderSection("blogs");

    await screen.findByRole("heading", { name: "Blog Management" });
    expect(
      within(menu())
        .getAllByRole("link")
        .map((link) => link.textContent),
    ).toEqual(["Blogs"]);
    expect(
      within(menu())
        .getAllByRole("button")
        .map((b) => b.textContent),
    ).toEqual(["Portfolio"]);
  });

  it("AdminShell_MyZenOwnerWithFlagOn_ShowsMyZen", async () => {
    server.use(http.get(`${API}/api/features`, () => HttpResponse.json({ myzen: true })));
    signedInAs("Admin.View", "MyZen.Owner");

    renderSection("settings");

    expect(
      await within(await screen.findByRole("navigation", { name: "Admin" })).findByRole("link", {
        name: "MyZen",
      }),
    ).toHaveAttribute("href", "/admin/dashboard/myzen");
  });

  it("AdminShell_NoAdminRole_ShowsNoAccessWithTheRolesToAskFor", async () => {
    // Arrange
    const user = userEvent.setup();
    signedInAs("offline_access");

    // Act
    renderSection("overview", "/admin/dashboard");

    // Assert
    expect(await screen.findByText("No Access")).toBeVisible();
    expect(screen.getByText(/Blogs\.Contributor - Blogs/)).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Return to Login" }));
    expect(signOut).toHaveBeenCalled();
  });

  it("AdminShell_IndexWithoutAdminView_OpensTheFirstAllowedSection", async () => {
    signedInAs("Projects.Contributor");

    renderSection("overview", "/admin/dashboard");

    await waitFor(() => expect(nav.replace).toHaveBeenCalledWith("/admin/dashboard/projects"));
  });

  it("AdminShell_UnknownSection_SaysSo", async () => {
    signedInAs("Admin.View");

    renderSection("nope");

    expect(await screen.findByRole("heading", { name: "Section not found" })).toBeVisible();
    expect(within(menu()).queryByRole("link", { current: "page" })).not.toBeInTheDocument();
  });

  it("AdminShell_SectionWithoutRole_SaysWhichRoleIsNeeded", async () => {
    signedInAs("Admin.View");

    renderSection("blogs");

    expect(await screen.findByRole("heading", { name: "No access" })).toBeVisible();
    expect(screen.getByText(/needs one of these roles: Blogs\.Contributor/)).toBeVisible();
  });

  it("AdminShell_MenuButton_OpensTheDrawerAndALinkClosesIt", async () => {
    // Arrange
    const user = userEvent.setup();
    signedInAs("Admin.View");
    renderSection("settings");
    await screen.findByRole("heading", { name: "System Settings" });

    // Act
    await user.click(screen.getByRole("button", { name: "Open admin menu" }));

    // Assert
    const drawer = await screen.findByRole("dialog", { name: "Admin menu" });
    await user.click(within(drawer).getByRole("link", { name: "Visitors" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  });

  it("AdminShell_CollapseButton_FoldsTheSidebarToIcons", async () => {
    // Arrange
    const user = userEvent.setup();
    signedInAs("Admin.View");
    renderSection("settings");
    await screen.findByRole("heading", { name: "System Settings" });

    // Act
    await user.click(screen.getByRole("button", { name: "Collapse admin menu" }));

    // Assert: group buttons become separators; links keep their names.
    expect(within(menu()).queryByRole("button")).not.toBeInTheDocument();
    expect(within(menu()).getByRole("link", { name: "Settings" })).toHaveAttribute(
      "title",
      "Settings",
    );
    expect(screen.getByRole("button", { name: "Expand admin menu" })).toHaveAttribute(
      "aria-expanded",
      "false",
    );
  });

  it("AdminShell_SignInFails_GoesBackToTheLoginPageWithAnError", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    vi.mocked(getUser).mockRejectedValue(new Error("down"));

    renderSection("overview", "/admin/dashboard");

    await waitFor(() =>
      expect(nav.push).toHaveBeenCalledWith(expect.stringMatching(/^\/admin\?error=/)),
    );
  });
});
