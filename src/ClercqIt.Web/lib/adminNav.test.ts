import { describe, expect, it } from "vitest";
import {
  allRoles,
  canAccess,
  findActiveItem,
  flattenNav,
  groupPathOf,
  hrefOf,
  visibleNav,
  type AdminNavItem,
  type AdminNavNode,
} from "./adminNav";

const Icon = () => null;
const Content = () => null;

const item = (key: string, roles: string[], extra: Partial<AdminNavItem> = {}): AdminNavItem => ({
  kind: "item",
  key,
  label: key,
  icon: Icon,
  roles,
  description: key,
  content: Content,
  ...extra,
});

// Overview, a group with a subgroup, and a flagged item.
const nav: AdminNavNode[] = [
  item("overview", ["Admin.View"], { isIndex: true }),
  {
    kind: "group",
    key: "portfolio",
    label: "Portfolio",
    children: [
      item("blogs", ["Blogs.Contributor"]),
      {
        kind: "group",
        key: "media",
        label: "Media",
        children: [item("images", ["Blogs.Contributor", "Projects.Contributor"])],
      },
    ],
  },
  {
    kind: "group",
    key: "administration",
    label: "Administration",
    children: [item("myzen", ["MyZen.Owner"], { featureFlag: "MyZen" })],
  },
];

describe("hrefOf", () => {
  it("HrefOf_IndexItem_IsTheDashboardRoot", () => {
    expect(hrefOf({ key: "overview", isIndex: true })).toBe("/admin/dashboard");
  });

  it("HrefOf_Item_UsesItsKeyAsSegment", () => {
    expect(hrefOf({ key: "blogs" })).toBe("/admin/dashboard/blogs");
  });
});

describe("canAccess", () => {
  it("CanAccess_AnyListedRole_IsAllowed", () => {
    expect(canAccess(flattenNav(nav)[2], { roles: ["Projects.Contributor"], flags: [] })).toBe(
      true,
    );
  });

  it("CanAccess_FlagOff_IsDenied", () => {
    const myzen = item("myzen", ["MyZen.Owner"], { featureFlag: "MyZen" });
    expect(canAccess(myzen, { roles: ["MyZen.Owner"], flags: [] })).toBe(false);
    expect(canAccess(myzen, { roles: ["MyZen.Owner"], flags: ["MyZen"] })).toBe(true);
  });
});

describe("visibleNav", () => {
  it("VisibleNav_AllRoles_KeepsTheWholeTree", () => {
    const roles = allRoles(nav);
    expect(visibleNav(nav, { roles, flags: ["MyZen"] })).toEqual(nav);
  });

  it("VisibleNav_OneRole_DropsOtherItemsAndEmptyGroups", () => {
    // Act
    const result = visibleNav(nav, { roles: ["Projects.Contributor"], flags: [] });

    // Assert: only the nested "images" item is left, inside its two groups.
    expect(flattenNav(result).map((i) => i.key)).toEqual(["images"]);
    expect(result.map((n) => n.key)).toEqual(["portfolio"]);
  });

  it("VisibleNav_NoRoles_IsEmpty", () => {
    expect(visibleNav(nav, { roles: [], flags: ["MyZen"] })).toEqual([]);
  });
});

describe("flattenNav and allRoles", () => {
  it("FlattenNav_Tree_ListsItemsDepthFirst", () => {
    expect(flattenNav(nav).map((i) => i.key)).toEqual(["overview", "blogs", "images", "myzen"]);
  });

  it("AllRoles_Tree_ListsEachRoleOnce", () => {
    expect(allRoles(nav)).toEqual([
      "Admin.View",
      "Blogs.Contributor",
      "Projects.Contributor",
      "MyZen.Owner",
    ]);
  });
});

describe("findActiveItem", () => {
  it.each([
    ["/admin/dashboard", "overview"],
    ["/admin/dashboard/", "overview"],
    ["/admin/dashboard/blogs", "blogs"],
    ["/admin/dashboard/blogs/123", "blogs"],
    ["/admin/dashboard/images", "images"],
  ])("FindActiveItem_%s_Is%s", (path, key) => {
    expect(findActiveItem(nav, path)?.key).toBe(key);
  });

  it.each([["/admin/dashboard/unknown"], ["/admin/dashboard/blogsx"], ["/blogs"], [null]])(
    "FindActiveItem_%s_IsNull",
    (path) => {
      expect(findActiveItem(nav, path)).toBeNull();
    },
  );
});

describe("groupPathOf", () => {
  it("GroupPathOf_NestedItem_ListsGroupsOutermostFirst", () => {
    expect(groupPathOf(nav, "images")).toEqual(["portfolio", "media"]);
  });

  it("GroupPathOf_TopLevelOrUnknownItem_IsEmpty", () => {
    expect(groupPathOf(nav, "overview")).toEqual([]);
    expect(groupPathOf(nav, "nope")).toEqual([]);
  });
});
