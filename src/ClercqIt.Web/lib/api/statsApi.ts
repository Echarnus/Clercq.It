import { z } from "zod";
import { authFetch } from "./adminApi";

// Visitor statistics (issue #429, docs/statistics.md): the admin page reads the
// totals. Page views are sent by pageViewApi.ts, which public pages load instead.

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

/** Statistics for the last `days` days, today included (needs the Admin.View role). */
export async function fetchSiteStatistics(days: number): Promise<SiteStatistics> {
  const response = await authFetch(`/api/stats?days=${days}`);
  if (!response.ok) {
    throw new Error(`Statistics request failed (${response.status})`);
  }
  return SiteStatisticsSchema.parse(await response.json());
}
