import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

/**
 * A spinner that screen readers announce as a status, for a view that is
 * loading (issue #490). `label` is read out and, with `showLabel`, shown.
 */
export function LoadingSpinner({
  label = "Loading",
  showLabel = false,
  className,
}: {
  label?: string;
  showLabel?: boolean;
  className?: string;
}) {
  return (
    <div
      role="status"
      className={cn("flex items-center gap-2 text-sm text-muted-foreground", className)}
    >
      <Loader2 className="h-5 w-5 animate-spin" aria-hidden />
      <span className={showLabel ? undefined : "sr-only"}>{label}</span>
    </div>
  );
}
