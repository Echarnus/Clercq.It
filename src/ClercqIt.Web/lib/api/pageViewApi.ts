import { getApiUrl } from "./baseApi";

// The public half of the visitor statistics (issue #429, docs/statistics.md): one
// cookieless page view per navigation. Kept apart from statsApi.ts, which reads the
// totals with the admin's token, so public pages ship no admin code (issue #454).

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
