"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type React from "react";
import { usePathname, useRouter } from "next/navigation";
import { LogOut, Menu, PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { PageContainer } from "@/components/page-container";
import { ThemeToggle } from "@/components/theme-toggle";
import { ADMIN_NAV } from "@/components/admin/admin-sections";
import { AdminSidebar } from "@/components/admin/AdminSidebar";
import { useSidebarCollapsed } from "@/hooks/use-sidebar-collapsed";
import {
  allRoles,
  findActiveItem,
  flattenNav,
  visibleNav,
  type AdminAccess,
  type AdminNavNode,
} from "@/lib/adminNav";
import { claimsOf, getUser, signIn, signOut } from "@/lib/auth";
import { isEnabled } from "@/lib/featureFlags";
import { cn } from "@/lib/utils";

export interface AdminSession {
  username: string;
  access: AdminAccess;
  /** `ADMIN_NAV` filtered to what this user may open. */
  nav: readonly AdminNavNode[];
}

const AdminSessionContext = createContext<AdminSession | null>(null);

/** The signed-in admin session; only inside `AdminShell`. */
export function useAdminSession(): AdminSession {
  const session = useContext(AdminSessionContext);
  if (!session) throw new Error("useAdminSession must be used inside AdminShell");
  return session;
}

/** Every role that opens an admin section. */
export const ADMIN_ROLES = allRoles(ADMIN_NAV);
const FEATURE_FLAGS = [...new Set(flattenNav(ADMIN_NAV).flatMap((item) => item.featureFlag ?? []))];

async function enabledFlags(): Promise<string[]> {
  const enabled = await Promise.all(
    FEATURE_FLAGS.map(async (flag) => ((await isEnabled(flag)) ? [flag] : [])),
  );
  return enabled.flat();
}

function NoAccess({ onLogout }: { onLogout: () => void }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md shadow-xl">
        <CardHeader>
          <CardTitle className="text-center text-2xl">No Access</CardTitle>
          <CardDescription className="text-center">
            Your account does not have any admin permissions assigned.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-center text-sm text-muted-foreground">
            Please contact an administrator to request access to the admin panel. You will need one
            of the following roles:
          </p>
          <ul className="list-inside list-disc space-y-1 text-sm text-muted-foreground">
            {ADMIN_ROLES.map((role) => (
              <li key={role}>
                {role} -{" "}
                {flattenNav(ADMIN_NAV)
                  .filter((item) => item.roles.includes(role))
                  .map((item) => item.label)
                  .join(", ")}
              </li>
            ))}
          </ul>
          <Button onClick={onLogout} className="w-full">
            <LogOut className="mr-2 h-4 w-4" />
            Return to Login
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}

/**
 * The admin frame (issue #449): signs the user in, then shows a header, the side menu
 * (an icon rail when collapsed, a drawer on phones and tablets) and the current section.
 */
export function AdminShell({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [session, setSession] = useState<{ username: string; access: AdminAccess } | null>(null);
  const [collapsed, setCollapsed] = useSidebarCollapsed();
  // The drawer is open for the page it was opened on, so following a link closes it.
  const [drawerOpenOn, setDrawerOpenOn] = useState<string | null>(null);

  useEffect(() => {
    // Tokens live in memory (lib/auth.ts). Without a session, go through the Keycloak
    // login and come back to this same section.
    let cancelled = false;
    const load = async () => {
      const user = await getUser();
      if (cancelled) return;
      if (!user) {
        await signIn(window.location.pathname);
        return;
      }
      const { username, roles } = claimsOf(user);
      const flags = await enabledFlags();
      if (cancelled) return;
      setSession({ username, access: { roles, flags } });
    };
    load().catch((error: unknown) => {
      console.error("Sign-in failed:", error);
      router.push(`/admin?error=${encodeURIComponent("Sign-in is not available right now")}`);
    });
    return () => {
      cancelled = true;
    };
  }, [router]);

  const nav = useMemo(() => (session ? visibleNav(ADMIN_NAV, session.access) : []), [session]);
  const value = useMemo(() => (session ? { ...session, nav } : null), [session, nav]);

  const handleLogout = () => {
    void signOut();
  };

  if (!value) {
    return null;
  }

  if (nav.length === 0) {
    return <NoAccess onLogout={handleLogout} />;
  }

  const activeKey = findActiveItem(nav, pathname)?.key ?? null;
  const drawerOpen = drawerOpenOn !== null && drawerOpenOn === pathname;

  return (
    <AdminSessionContext.Provider value={value}>
      <div className="min-h-screen bg-background text-foreground">
        <header className="sticky top-0 z-40 border-b border-border bg-background/80 backdrop-blur-xs">
          <div className="flex h-16 items-center gap-2 px-4 sm:px-6">
            <Button
              variant="ghost"
              size="icon"
              className="lg:hidden"
              aria-label="Open admin menu"
              aria-expanded={drawerOpen}
              aria-controls="admin-nav-drawer"
              onClick={() => setDrawerOpenOn(pathname)}
            >
              <Menu className="h-5 w-5" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="hidden lg:inline-flex"
              aria-label={collapsed ? "Expand admin menu" : "Collapse admin menu"}
              aria-expanded={!collapsed}
              aria-controls="admin-nav-sidebar"
              onClick={() => setCollapsed(!collapsed)}
            >
              {collapsed ? (
                <PanelLeftOpen className="h-5 w-5" />
              ) : (
                <PanelLeftClose className="h-5 w-5" />
              )}
            </Button>
            <div className="min-w-0 flex-1">
              <p className="truncate text-lg leading-tight font-bold">Clercq.It Admin</p>
              <p className="hidden truncate text-sm text-muted-foreground sm:block">
                Content Management System {value.username && `• Welcome, ${value.username}`}
              </p>
            </div>
            <ThemeToggle />
            <Button variant="outline" onClick={handleLogout} className="flex items-center gap-2">
              <LogOut className="h-4 w-4" />
              <span className="hidden sm:inline">Logout</span>
              <span className="sr-only sm:hidden">Logout</span>
            </Button>
          </div>
        </header>

        <div className="flex">
          <aside
            className={cn(
              "sticky top-16 hidden h-[calc(100dvh-4rem)] shrink-0 overflow-y-auto border-r border-border bg-card transition-[width] lg:block",
              collapsed ? "w-16" : "w-64",
            )}
          >
            <AdminSidebar
              id="admin-nav-sidebar"
              nav={nav}
              activeKey={activeKey}
              collapsed={collapsed}
            />
          </aside>

          <Sheet open={drawerOpen} onOpenChange={(open) => setDrawerOpenOn(open ? pathname : null)}>
            <SheetContent id="admin-nav-drawer" side="left" className="w-72 overflow-y-auto p-0">
              <SheetHeader className="border-b border-border px-6 py-4 text-left">
                <SheetTitle>Admin menu</SheetTitle>
                <SheetDescription className="sr-only">Sections of the admin panel</SheetDescription>
              </SheetHeader>
              <AdminSidebar
                nav={nav}
                activeKey={activeKey}
                onNavigate={() => setDrawerOpenOn(null)}
              />
            </SheetContent>
          </Sheet>

          <main className="min-w-0 flex-1 py-8">
            <PageContainer>{children}</PageContainer>
          </main>
        </div>
      </div>
    </AdminSessionContext.Provider>
  );
}
