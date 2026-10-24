"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";
import { trackPageView } from "@/lib/api/statsApi";

/**
 * Counts one page view per navigation on the public site (docs/statistics.md).
 * No cookie, no local storage: the API only receives the path.
 */
export function PageViewTracker() {
  const pathname = usePathname();

  useEffect(() => {
    if (pathname) {
      void trackPageView(pathname);
    }
  }, [pathname]);

  return null;
}
