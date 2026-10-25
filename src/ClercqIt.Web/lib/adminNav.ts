import type React from "react";

// The admin menu is data (issue #449): one tree of groups and items. A new admin
// section is one item in `components/admin/admin-sections.tsx`; the sidebar, the
// routes, role filtering and the overview list all come from that tree.

export const ADMIN_BASE_PATH = "/admin/dashboard";

export type AdminIcon = React.ComponentType<{ className?: string; "aria-hidden"?: boolean }>;

/** What a section's content gets from the shell. */
export interface AdminSectionContext {
  username: string;
  roles: readonly string[];
  /** The menu as this user sees it. */
  nav: readonly AdminNavNode[];
}

export interface AdminNavItem {
  kind: "item";
  /** Unique across the tree; also the URL segment (`/admin/dashboard/<key>`). */
  key: string;
  label: string;
  icon: AdminIcon;
  /** The user needs at least one of these roles. */
  roles: readonly string[];
  /** Optional feature flag (lib/featureFlags) that must be on as well. */
  featureFlag?: string;
  /** One line for the overview and the section header. */
  description: string;
  /** The page heading; defaults to the label. */
  title?: string;
  /** The overview renders at `/admin/dashboard` itself. */
  isIndex?: boolean;
  content: React.ComponentType<AdminSectionContext>;
}

export interface AdminNavGroup {
  kind: "group";
  key: string;
  label: string;
  icon?: AdminIcon;
  children: readonly AdminNavNode[];
}

export type AdminNavNode = AdminNavItem | AdminNavGroup;

export interface AdminAccess {
  roles: readonly string[];
  /** Feature flags that are on. */
  flags: readonly string[];
}

/** The URL of an item. */
export function hrefOf(item: Pick<AdminNavItem, "key" | "isIndex">): string {
  return item.isIndex ? ADMIN_BASE_PATH : `${ADMIN_BASE_PATH}/${item.key}`;
}

export function canAccess(item: AdminNavItem, access: AdminAccess): boolean {
  const hasRole = item.roles.some((role) => access.roles.includes(role));
  const flagOn = !item.featureFlag || access.flags.includes(item.featureFlag);
  return hasRole && flagOn;
}

/** The tree with only the items the user may open; groups left empty are dropped. */
export function visibleNav(nodes: readonly AdminNavNode[], access: AdminAccess): AdminNavNode[] {
  return nodes.flatMap((node): AdminNavNode[] => {
    if (node.kind === "item") {
      return canAccess(node, access) ? [node] : [];
    }
    const children = visibleNav(node.children, access);
    return children.length > 0 ? [{ ...node, children }] : [];
  });
}

/** All items, depth first, in menu order. */
export function flattenNav(nodes: readonly AdminNavNode[]): AdminNavItem[] {
  return nodes.flatMap((node) => (node.kind === "item" ? [node] : flattenNav(node.children)));
}

/** Every role that opens at least one section. */
export function allRoles(nodes: readonly AdminNavNode[]): string[] {
  return [...new Set(flattenNav(nodes).flatMap((item) => item.roles))];
}

/**
 * The item for a path: an exact match, or the section a deeper path belongs to
 * (`/admin/dashboard/blogs/<id>` is Blogs). Trailing slashes are ignored.
 */
export function findActiveItem(
  nodes: readonly AdminNavNode[],
  pathname: string | null,
): AdminNavItem | null {
  if (!pathname) return null;
  const path = pathname.length > 1 ? pathname.replace(/\/+$/, "") : pathname;
  const items = flattenNav(nodes);
  return (
    items.find((item) => hrefOf(item) === path) ??
    items.find((item) => !item.isIndex && path.startsWith(`${hrefOf(item)}/`)) ??
    null
  );
}

/** The keys of the groups that contain an item, outermost first; empty when not found. */
export function groupPathOf(nodes: readonly AdminNavNode[], itemKey: string): string[] {
  for (const node of nodes) {
    if (node.kind === "group" && flattenNav(node.children).some((item) => item.key === itemKey)) {
      return [node.key, ...groupPathOf(node.children, itemKey)];
    }
  }
  return [];
}
