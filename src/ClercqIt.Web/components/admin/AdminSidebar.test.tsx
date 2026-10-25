import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { AdminNavItem, AdminNavNode } from "@/lib/adminNav";
import { AdminSidebar } from "./AdminSidebar";

const Icon = () => null;
const item = (key: string, label: string, isIndex = false): AdminNavItem => ({
  kind: "item",
  key,
  label,
  icon: Icon,
  roles: ["Admin.View"],
  description: label,
  content: () => null,
  isIndex,
});

const nav: AdminNavNode[] = [
  item("overview", "Overview", true),
  {
    kind: "group",
    key: "portfolio",
    label: "Portfolio",
    children: [
      item("blogs", "Blogs"),
      { kind: "group", key: "media", label: "Media", children: [item("images", "Images")] },
    ],
  },
  {
    kind: "group",
    key: "administration",
    label: "Administration",
    children: [item("settings", "Settings")],
  },
];

const menu = () => screen.getByRole("navigation", { name: "Admin" });

describe("AdminSidebar", () => {
  it("AdminSidebar_ActiveKey_MarksOnlyThatLinkAsCurrentPage", () => {
    render(<AdminSidebar nav={nav} activeKey="blogs" />);

    const blogs = within(menu()).getByRole("link", { name: "Blogs" });
    expect(blogs).toHaveAttribute("aria-current", "page");
    expect(blogs).toHaveAttribute("href", "/admin/dashboard/blogs");
    expect(within(menu()).getByRole("link", { name: "Overview" })).toHaveAttribute(
      "href",
      "/admin/dashboard",
    );
    expect(within(menu()).getByRole("link", { name: "Overview" })).not.toHaveAttribute(
      "aria-current",
    );
  });

  it("AdminSidebar_GroupButton_FoldsAndUnfoldsItsItems", async () => {
    // Arrange
    const user = userEvent.setup();
    render(<AdminSidebar nav={nav} activeKey="overview" />);
    const group = screen.getByRole("button", { name: "Administration" });
    expect(group).toHaveAttribute("aria-expanded", "true");

    // Act
    await user.click(group);

    // Assert
    expect(group).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("link", { name: "Settings" })).not.toBeInTheDocument();
    await user.click(group);
    expect(screen.getByRole("link", { name: "Settings" })).toBeVisible();
  });

  it("AdminSidebar_GroupWithCurrentPage_StaysOpen", async () => {
    const user = userEvent.setup();
    render(<AdminSidebar nav={nav} activeKey="images" />);

    await user.click(screen.getByRole("button", { name: "Media" }));

    expect(screen.getByRole("link", { name: "Images" })).toBeVisible();
  });

  it("AdminSidebar_Collapsed_ShowsIconLinksWithNamesAndNoGroupButtons", () => {
    render(<AdminSidebar nav={nav} activeKey="blogs" collapsed />);

    expect(screen.queryByRole("button")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Blogs" })).toHaveAttribute("title", "Blogs");
    expect(screen.getAllByRole("separator")).toHaveLength(3);
  });

  it("AdminSidebar_ArrowKeys_MoveFocusThroughTheMenu", async () => {
    // Arrange
    const user = userEvent.setup();
    render(<AdminSidebar nav={nav} activeKey="overview" />);
    screen.getByRole("link", { name: "Overview" }).focus();

    // Act / Assert
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("button", { name: "Portfolio" })).toHaveFocus();
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("link", { name: "Blogs" })).toHaveFocus();
    await user.keyboard("{End}");
    expect(screen.getByRole("link", { name: "Settings" })).toHaveFocus();
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("link", { name: "Overview" })).toHaveFocus();
    await user.keyboard("{ArrowUp}");
    expect(screen.getByRole("link", { name: "Settings" })).toHaveFocus();
    await user.keyboard("{Home}");
    expect(screen.getByRole("link", { name: "Overview" })).toHaveFocus();
  });

  it("AdminSidebar_LinkClicked_CallsOnNavigate", async () => {
    const user = userEvent.setup();
    const onNavigate = vi.fn();
    render(<AdminSidebar nav={nav} activeKey="overview" onNavigate={onNavigate} />);

    await user.click(screen.getByRole("link", { name: "Blogs" }));

    expect(onNavigate).toHaveBeenCalledOnce();
  });
});
