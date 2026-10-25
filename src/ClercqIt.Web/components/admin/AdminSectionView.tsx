"use client";

import { useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Card, CardContent } from "@/components/ui/card";
import { ADMIN_NAV } from "@/components/admin/admin-sections";
import { ADMIN_ROLES, useAdminSession } from "@/components/admin/AdminShell";
import { ADMIN_BASE_PATH, canAccess, flattenNav, hrefOf } from "@/lib/adminNav";

function Notice({ title, text }: { title: string; text: string }) {
  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">{title}</h1>
      <p className="text-muted-foreground">{text}</p>
      <Link href={ADMIN_BASE_PATH} className="text-sm underline underline-offset-4">
        Back to the admin overview
      </Link>
    </div>
  );
}

/** One admin section by its `ADMIN_NAV` key: a heading, then the section's content. */
export function AdminSectionView({ sectionKey }: { sectionKey: string }) {
  const router = useRouter();
  const { username, access, nav } = useAdminSession();
  const item = flattenNav(ADMIN_NAV).find((candidate) => candidate.key === sectionKey);
  const allowed = item ? canAccess(item, access) : false;
  // Without the overview (no Admin.View), /admin/dashboard opens the first section the user has.
  const fallback = item?.isIndex && !allowed ? flattenNav(nav)[0] : undefined;

  useEffect(() => {
    if (fallback) router.replace(hrefOf(fallback));
  }, [fallback, router]);

  if (fallback) return null;
  if (!item) {
    return <Notice title="Section not found" text={`There is no admin section "${sectionKey}".`} />;
  }
  if (!allowed) {
    return (
      <Notice
        title="No access"
        text={`Your account cannot open ${item.label}. It needs one of these roles: ${item.roles.join(", ")}.`}
      />
    );
  }

  const Content = item.content;
  const context = {
    username,
    roles: access.roles.filter((role) => ADMIN_ROLES.includes(role)),
    nav,
  };

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-2xl font-bold tracking-tight">{item.title ?? item.label}</h1>
        <p className="text-sm text-muted-foreground">{item.description}</p>
      </div>
      {item.isIndex ? (
        <Content {...context} />
      ) : (
        <Card>
          <CardContent className="pt-6">
            <Content {...context} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
