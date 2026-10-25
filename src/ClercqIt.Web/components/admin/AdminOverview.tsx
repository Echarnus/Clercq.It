"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Award, FileText, FolderKanban, Settings } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
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
  value: string | number;
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
        <div className={valueClassName ?? "text-2xl font-bold"}>{value}</div>
        <p className="text-xs text-muted-foreground">{hint}</p>
      </CardContent>
    </Card>
  );
}

/** The admin landing page: content counts and the sections this user can open. */
export function AdminOverview({ nav }: AdminSectionContext) {
  const [stats, setStats] = useState({ blogs: 0, projects: 0, certifications: 0 });

  useEffect(() => {
    const loadStats = async () => {
      try {
        const [blogs, projects, certifications] = await Promise.all([
          fetchAllBlogs(),
          fetchAllProjects(),
          fetchAllCertifications(),
        ]);
        setStats({
          blogs: blogs.length,
          projects: projects.length,
          certifications: certifications.length,
        });
      } catch (error) {
        console.error("Failed to load stats:", error);
      }
    };
    void loadStats();
  }, []);

  const sections = flattenNav(nav).filter((item) => !item.isIndex);

  return (
    <div className="space-y-6">
      <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Total Blogs"
          value={stats.blogs}
          hint="Published blog posts"
          icon={FileText}
        />
        <StatCard
          title="Total Projects"
          value={stats.projects}
          hint="Portfolio projects"
          icon={FolderKanban}
        />
        <StatCard
          title="Certifications"
          value={stats.certifications}
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
