"use client";

import { useState, type KeyboardEvent } from "react";
import Link from "next/link";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";
import { groupPathOf, hrefOf, type AdminNavGroup, type AdminNavNode } from "@/lib/adminNav";

interface AdminSidebarProps {
  nav: readonly AdminNavNode[];
  /** The key of the current item, if any. */
  activeKey: string | null;
  /** Icon-only rail: group headers become separators, labels move to tooltips. */
  collapsed?: boolean;
  /** Called after a link is followed, e.g. to close the phone drawer. */
  onNavigate?: () => void;
  id?: string;
}

const FOCUSABLE = "[data-admin-nav-focus]";

// Each level of nesting moves one step in.
const INDENTS = ["pl-3", "pl-5", "pl-8", "pl-11"] as const;
const indent = (depth: number) => INDENTS[Math.min(depth, INDENTS.length - 1)];

/**
 * Up/Down move between the visible links and group buttons, Home/End jump to the
 * first/last one. Tab, Enter and Space keep their native behaviour.
 */
export function handleNavKeyDown(event: KeyboardEvent<HTMLElement>) {
  const keys = ["ArrowDown", "ArrowUp", "Home", "End"];
  if (!keys.includes(event.key)) return;
  const menu = event.currentTarget.closest("nav");
  if (!menu) return;
  const targets = Array.from(menu.querySelectorAll<HTMLElement>(FOCUSABLE));
  const current = targets.indexOf(document.activeElement as HTMLElement);
  const last = targets.length - 1;
  const next =
    event.key === "Home"
      ? 0
      : event.key === "End"
        ? last
        : event.key === "ArrowDown"
          ? current >= last
            ? 0
            : current + 1
          : current <= 0
            ? last
            : current - 1;
  event.preventDefault();
  targets.at(next)?.focus();
}

/** The admin side menu (issue #449): groups, subgroups and items from `ADMIN_NAV`. */
export function AdminSidebar({
  nav,
  activeKey,
  collapsed = false,
  onNavigate,
  id,
}: AdminSidebarProps) {
  // Groups start open. A closed group that holds the current page opens again,
  // so the highlighted item is never hidden.
  const [closedGroups, setClosedGroups] = useState<ReadonlySet<string>>(new Set());
  const activeGroups = activeKey ? groupPathOf(nav, activeKey) : [];

  const toggle = (key: string) =>
    setClosedGroups((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });

  const isOpen = (group: AdminNavGroup) =>
    collapsed || !closedGroups.has(group.key) || activeGroups.includes(group.key);

  const renderNodes = (nodes: readonly AdminNavNode[], depth: number) => (
    <ul className={cn("space-y-1", depth > 0 && !collapsed && "mt-1")}>
      {nodes.map((node) =>
        node.kind === "item" ? (
          <li key={node.key}>
            <Link
              href={hrefOf(node)}
              data-admin-nav-focus=""
              onKeyDown={handleNavKeyDown}
              aria-current={node.key === activeKey ? "page" : undefined}
              aria-label={collapsed ? node.label : undefined}
              title={collapsed ? node.label : undefined}
              onClick={onNavigate}
              className={cn(
                "flex items-center gap-3 rounded-md py-2 text-sm transition-colors",
                "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-hidden",
                collapsed ? "justify-center px-2" : cn("pr-3", indent(depth)),
                node.key === activeKey
                  ? "bg-primary font-medium text-primary-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground",
              )}
            >
              <node.icon className="h-4 w-4 shrink-0" aria-hidden />
              {!collapsed && <span className="truncate">{node.label}</span>}
            </Link>
          </li>
        ) : (
          <li key={node.key}>
            {collapsed ? (
              <div
                className="my-2 border-t border-border"
                role="separator"
                aria-label={node.label}
              />
            ) : (
              <button
                type="button"
                data-admin-nav-focus=""
                onKeyDown={handleNavKeyDown}
                aria-expanded={isOpen(node)}
                aria-controls={`admin-nav-group-${node.key}`}
                onClick={() => toggle(node.key)}
                className={cn(
                  "flex w-full items-center gap-2 rounded-md py-2 pr-3 text-xs font-semibold tracking-wide text-muted-foreground uppercase",
                  indent(depth),
                  "hover:bg-accent hover:text-accent-foreground",
                  "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-hidden",
                  depth > 0 && "normal-case",
                )}
              >
                {node.icon && <node.icon className="h-4 w-4 shrink-0" aria-hidden />}
                <span className="flex-1 truncate text-left">{node.label}</span>
                <ChevronDown
                  className={cn(
                    "h-4 w-4 shrink-0 transition-transform",
                    !isOpen(node) && "-rotate-90",
                  )}
                  aria-hidden
                />
              </button>
            )}
            <div id={`admin-nav-group-${node.key}`} hidden={!isOpen(node)}>
              {renderNodes(node.children, depth + 1)}
            </div>
          </li>
        ),
      )}
    </ul>
  );

  return (
    <nav id={id} aria-label="Admin" className="p-3">
      {renderNodes(nav, 0)}
    </nav>
  );
}
