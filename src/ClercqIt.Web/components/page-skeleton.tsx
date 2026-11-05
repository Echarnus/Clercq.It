import { PageContainer } from "@/components/page-container";
import { Skeleton } from "@/components/ui/skeleton";

/**
 * The placeholder a public page shows while its data loads on navigation
 * (`loading.tsx`, issue #490): a heading, an intro and either a grid of cards
 * (list pages) or a block of text (detail pages).
 */
export function PageSkeleton({
  label,
  variant = "grid",
}: {
  label: string;
  variant?: "grid" | "article";
}) {
  return (
    <div role="status" aria-label={label}>
      <span className="sr-only">{label}</span>
      <PageContainer as="section" className="py-16">
        <div className="max-w-3xl space-y-4" aria-hidden>
          <Skeleton className="h-10 w-2/3" />
          <Skeleton className="h-6 w-full" />
          <Skeleton className="h-6 w-5/6" />
        </div>
      </PageContainer>
      <PageContainer as="section" className="pb-16">
        {variant === "grid" ? (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3" aria-hidden>
            {[0, 1, 2, 3, 4, 5].map((card) => (
              <div key={card} className="space-y-3">
                <Skeleton className="aspect-video w-full" />
                <Skeleton className="h-5 w-3/4" />
                <Skeleton className="h-4 w-full" />
              </div>
            ))}
          </div>
        ) : (
          <div className="max-w-3xl space-y-3" aria-hidden>
            <Skeleton className="aspect-video w-full" />
            {[0, 1, 2, 3, 4, 5].map((line) => (
              <Skeleton key={line} className="h-4 w-full" />
            ))}
          </div>
        )}
      </PageContainer>
    </div>
  );
}
