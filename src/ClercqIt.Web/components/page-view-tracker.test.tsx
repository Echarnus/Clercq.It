import { render } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { PageViewTracker } from "./page-view-tracker";

const pathname = vi.hoisted((): { current: string | null } => ({ current: "/" }));

vi.mock("next/navigation", () => ({ usePathname: () => pathname.current }));

function recordPaths(): string[] {
  const paths: string[] = [];
  server.use(
    http.post(`${API}/api/stats/pageview`, async ({ request }) => {
      paths.push(((await request.json()) as { path: string }).path);
      return new HttpResponse(null, { status: 204 });
    }),
  );
  return paths;
}

describe("PageViewTracker", () => {
  it("counts a view for each page the visitor navigates to", async () => {
    const paths = recordPaths();
    pathname.current = "/blogs";
    const { rerender } = render(<PageViewTracker />);

    pathname.current = "/portfolio";
    rerender(<PageViewTracker />);

    await vi.waitFor(() => expect(paths).toEqual(["/blogs", "/portfolio"]));
  });

  it("sends nothing before the router knows the path", async () => {
    const paths = recordPaths();
    pathname.current = null;

    render(<PageViewTracker />);

    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(paths).toEqual([]);
  });
});
