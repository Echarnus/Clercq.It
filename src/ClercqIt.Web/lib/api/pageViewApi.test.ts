// @vitest-environment node
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { trackPageView } from "./pageViewApi";

describe("page view API client", () => {
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
});
