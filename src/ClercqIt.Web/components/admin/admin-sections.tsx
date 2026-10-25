import {
  Award,
  Activity,
  BarChart3,
  Briefcase,
  Clock,
  FileText,
  FolderKanban,
  LineChart,
  Settings,
  Shield,
} from "lucide-react";
import { AdminOverview } from "@/components/admin/AdminOverview";
import { AdminSettings } from "@/components/admin/AdminSettings";
import { BlogList } from "@/components/admin/BlogList";
import { CertificationList } from "@/components/admin/CertificationList";
import { MyZenPanel } from "@/components/admin/MyZenPanel";
import { ProjectList } from "@/components/admin/ProjectList";
import { StatisticsPanel } from "@/components/admin/StatisticsPanel";
import type { AdminNavNode } from "@/lib/adminNav";

/**
 * The admin menu (issue #449). Add a section by adding one item here: it gets a sidebar
 * entry, a URL (`/admin/dashboard/<key>`), role filtering and a line on the overview.
 * Groups may contain groups. Keys must be unique across the whole tree.
 */
export const ADMIN_NAV: readonly AdminNavNode[] = [
  {
    kind: "item",
    key: "overview",
    label: "Overview",
    icon: BarChart3,
    roles: ["Admin.View"],
    description: "Content counts and system status",
    isIndex: true,
    content: AdminOverview,
  },
  {
    kind: "group",
    key: "portfolio",
    label: "Portfolio",
    icon: Briefcase,
    children: [
      {
        kind: "item",
        key: "blogs",
        label: "Blogs",
        icon: FileText,
        roles: ["Blogs.Contributor"],
        title: "Blog Management",
        description: "Create, edit, and manage blog posts",
        content: BlogList,
      },
      {
        kind: "item",
        key: "projects",
        label: "Projects",
        icon: FolderKanban,
        roles: ["Projects.Contributor"],
        title: "Project Management",
        description: "Manage your portfolio projects",
        content: ProjectList,
      },
      {
        kind: "item",
        key: "certifications",
        label: "Certifications",
        icon: Award,
        roles: ["Certifications.Contributor"],
        title: "Certification Management",
        description: "Showcase your professional certifications",
        content: CertificationList,
      },
    ],
  },
  {
    kind: "group",
    key: "statistics",
    label: "Statistics",
    icon: Activity,
    children: [
      {
        // First-party visitor statistics (docs/statistics.md)
        kind: "item",
        key: "visitors",
        label: "Visitors",
        icon: LineChart,
        roles: ["Admin.View"],
        title: "Visitor statistics",
        description: "Page views and unique visitors on the public site",
        content: StatisticsPanel,
      },
    ],
  },
  {
    kind: "group",
    key: "administration",
    label: "Administration",
    icon: Shield,
    children: [
      {
        // The owner's own MyZen account (docs/myzen.md)
        kind: "item",
        key: "myzen",
        label: "MyZen",
        icon: Clock,
        roles: ["MyZen.Owner"],
        featureFlag: "MyZen",
        description: "Connect your MyZen account and see this month's time entries",
        content: () => <MyZenPanel />,
      },
      {
        kind: "item",
        key: "settings",
        label: "Settings",
        icon: Settings,
        roles: ["Admin.View"],
        title: "System Settings",
        description: "Authentication, your roles and system status",
        content: AdminSettings,
      },
    ],
  },
];
