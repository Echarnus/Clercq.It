import type React from "react";
import { cn } from "@/lib/utils";

/**
 * The one content width of the public site (issue #356). Header, footer and every page use it,
 * so the logo, the page content and the footer share the same left and right edges on every
 * route. Narrow long-form text inside it (e.g. `max-w-3xl`) instead of narrowing the container.
 */
export function PageContainer({
  as: Component = "div",
  className,
  children,
}: {
  as?: "div" | "section" | "nav";
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <Component className={cn("mx-auto w-full max-w-6xl px-4 sm:px-6 lg:px-8", className)}>
      {children}
    </Component>
  );
}
