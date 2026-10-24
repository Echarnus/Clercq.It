import type React from "react";
import type { Metadata } from "next";
import "../globals.css";

export const metadata: Metadata = {
  title: "Admin",
  description: "Admin panel for Clercq.It",
  // Keep the admin out of search results; robots.txt also disallows /admin.
  robots: { index: false, follow: false },
};

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return children;
}
