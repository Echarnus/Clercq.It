// @vitest-environment node
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { fetchSiteStatistics, trackPageView } from "./statsApi";

vi.mock("../auth", () => ({ getAccessToken: vi.fn(() => Promise.resolve("token")) }));

const statistics = {
  from: "2026-10-18",
  to: "2026-10-24",
  pageViews: 15,
  uniquePageViews: 9,
  visitors: 6,
  days: [{ date: "2026-10-24", pageViews: 15, uniquePageViews: 9, visitors: 6 }],
  topPages: [{ path: "/", pageViews: 15, uniquePageViews: 9 }],
};

describe("statistics API client", () => {
  it("sends the page path as JSON", async () => {
    let body: unknown = null;
    server.use(
      http.post(`${API}/api/stats/pageview`, async ({ request }) => {
        body = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );

    await trackPageView("/blogs");

    expect(body).toEqual({ path: "/blogs" });
  });

  it("swallows a failed page view", async () => {
    server.use(http.post(`${API}/api/stats/pageview`, () => HttpResponse.error()));

    await expect(trackPageView("/")).resolves.toBeUndefined();
  });

  it("reads the statistics for a period with the access token", async () => {
    let url = "";
    let auth: string | null = null;
    server.use(
      http.get(`${API}/api/stats`, ({ request }) => {
        url = request.url;
        auth = request.headers.get("Authorization");
        return HttpResponse.json(statistics);
      }),
    );

    const result = await fetchSiteStatistics(7);

    expect(result).toEqual(statistics);
    expect(new URL(url).searchParams.get("days")).toBe("7");
    expect(auth).toBe("Bearer token");
  });

  it("throws when the API refuses", async () => {
    server.use(http.get(`${API}/api/stats`, () => new HttpResponse(null, { status: 403 })));

    await expect(fetchSiteStatistics(30)).rejects.toThrow("Statistics request failed (403)");
  });
});
