import { z } from "zod";
import { authFetch } from "./adminApi";
import { getApiUrl } from "./baseApi";

// Visitor statistics (issue #429, docs/statistics.md). The site sends one
// cookieless page view per navigation; the admin page reads the totals.

export const DailyStatisticsSchema = z.object({
  date: z.iso.date(),
  pageViews: z.number(),
  uniquePageViews: z.number(),
  visitors: z.number(),
});

export type DailyStatistics = z.infer<typeof DailyStatisticsSchema>;

export const PageStatisticsSchema = z.object({
  path: z.string(),
  pageViews: z.number(),
  uniquePageViews: z.number(),
});

export const SiteStatisticsSchema = z.object({
  from: z.iso.date(),
  to: z.iso.date(),
  pageViews: z.number(),
  uniquePageViews: z.number(),
  visitors: z.number(),
  days: z.array(DailyStatisticsSchema),
  topPages: z.array(PageStatisticsSchema),
});

export type SiteStatistics = z.infer<typeof SiteStatisticsSchema>;

/** Counts a view of `path`. Never throws: a lost page view must not break the page. */
export async function trackPageView(path: string): Promise<void> {
  try {
    await fetch(`${getApiUrl()}/api/stats/pageview`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ path }),
      // Lets the request finish when the visitor navigates away.
      keepalive: true,
    });
  } catch {
    // Offline, blocked by an extension, or the API is down: nothing to do.
  }
}

/** Statistics for the last `days` days, today included (needs the Admin.View role). */
export async function fetchSiteStatistics(days: number): Promise<SiteStatistics> {
  const response = await authFetch(`/api/stats?days=${days}`);
  if (!response.ok) {
    throw new Error(`Statistics request failed (${response.status})`);
  }
  return SiteStatisticsSchema.parse(await response.json());
}
