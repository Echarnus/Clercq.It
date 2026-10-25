import type React from "react";
import { AdminShell } from "@/components/admin/AdminShell";

// Every admin section shares the shell: sign-in, header and side menu (issue #449).
export default function AdminDashboardLayout({ children }: { children: React.ReactNode }) {
  return <AdminShell>{children}</AdminShell>;
}
