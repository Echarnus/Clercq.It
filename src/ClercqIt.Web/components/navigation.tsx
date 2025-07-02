"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";

export function Navigation() {
  const pathname = usePathname();
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  const getCurrentPage = () => {
    if (!isHydrated) return null; // Don't determine active state until hydrated
    return pathname === "/"
      ? "home"
      : pathname === "/portfolio"
      ? "portfolio"
      : null;
  };

  const currentPage = getCurrentPage();

  return (
    <nav className="flex gap-6">
      <Link
        href="/"
        className={
          currentPage === "home"
            ? "text-slate-900 font-medium border-b-2 border-slate-900 dark:text-white dark:border-white"
            : "text-slate-600 hover:text-slate-900 transition-colors hover:border-b-2 hover:border-slate-600 dark:text-slate-400 dark:hover:text-white dark:hover:border-slate-400"
        }
      >
        Home
      </Link>
      <Link
        href="/portfolio"
        className={
          currentPage === "portfolio"
            ? "text-slate-900 font-medium border-b-2 border-slate-900 dark:text-white dark:border-white"
            : "text-slate-600 hover:text-slate-900 transition-colors hover:border-b-2 hover:border-slate-600 dark:text-slate-400 dark:hover:text-white dark:hover:border-slate-400"
        }
      >
        Portfolio
      </Link>
    </nav>
  );
}
