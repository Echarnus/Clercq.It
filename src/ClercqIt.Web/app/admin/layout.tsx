import type React from "react";
import type { Metadata } from "next";
import "../globals.css";

export const metadata: Metadata = {
  title: "Admin - Clercq.It",
  description: "Admin panel for Clercq.It",
};

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}
