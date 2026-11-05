"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Award, FileText, FolderKanban, Settings } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { isAbortError } from "@/hooks/use-latest-request";
import { fetchAllBlogs, fetchAllCertifications, fetchAllProjects } from "@/lib/api/adminApi";
import { flattenNav, hrefOf, type AdminSectionContext } from "@/lib/adminNav";

function StatCard({
  title,
  value,
  hint,
  icon: Icon,
  valueClassName,
}: {
  title: string;
  /** Null while loading: the card shows a placeholder. */
  value: string | number | null;
  hint: string;
  icon: typeof FileText;
  valueClassName?: string;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" aria-hidden />
      </CardHeader>
      <CardContent>
        {value === null ? (
          <Skeleton className="h-8 w-12" aria-label={`Loading ${title}`} role="status" />
        ) : (
          <div className={valueClassName ?? "text-2xl font-bold"}>{value}</div>
        )}
        <p className="text-xs text-muted-foreground">{hint}</p>
      </CardContent>
    </Card>
  );
}

/** The admin landing page: content counts and the sections this user can open. */
export function AdminOverview({ nav }: AdminSectionContext) {
  // Null until the counts load; the cards show a placeholder meanwhile.
  const [stats, setStats] = useState<{
    blogs: number;
    projects: number;
    certifications: number;
  } | null>(null);

  useEffect(() => {
    // Leaving the overview aborts the three requests (issue #490).
    const controller = new AbortController();
    const { signal } = controller;
    Promise.all([
      fetchAllBlogs({ signal }),
      fetchAllProjects({ signal }),
      fetchAllCertifications({ signal }),
    ])
      .then(([blogs, projects, certifications]) => {
        if (signal.aborted) return;
        setStats({
          blogs: blogs.length,
          projects: projects.length,
          certifications: certifications.length,
        });
      })
      .catch((error: unknown) => {
        if (signal.aborted || isAbortError(error)) return;
        console.error("Failed to load stats:", error);
      });
    return () => controller.abort();
  }, []);

  const sections = flattenNav(nav).filter((item) => !item.isIndex);

  return (
    <div className="space-y-6">
      <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Total Blogs"
          value={stats?.blogs ?? null}
          hint="Published blog posts"
          icon={FileText}
        />
        <StatCard
          title="Total Projects"
          value={stats?.projects ?? null}
          hint="Portfolio projects"
          icon={FolderKanban}
        />
        <StatCard
          title="Certifications"
          value={stats?.certifications ?? null}
          hint="Professional certifications"
          icon={Award}
        />
        <StatCard
          title="System Status"
          value="Healthy"
          hint="All systems operational"
          icon={Settings}
          valueClassName="text-2xl font-bold text-green-600 dark:text-green-500"
        />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Welcome to the Admin Panel</CardTitle>
          <CardDescription>
            Manage your content, projects, and settings from this dashboard.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">Use the menu to move between sections:</p>
          <ul className="space-y-2 text-sm text-muted-foreground">
            {sections.map((item) => (
              <li key={item.key} className="flex items-start gap-2">
                <item.icon className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
                <span>
                  <Link
                    href={hrefOf(item)}
                    className="font-semibold text-foreground underline-offset-4 hover:underline"
                  >
                    {item.label}
                  </Link>
                  : {item.description}
                </span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
