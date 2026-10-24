import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import * as api from "@/lib/api/statsApi";
import { StatisticsPanel, shortDate } from "./StatisticsPanel";

vi.mock("@/lib/api/statsApi", async (importOriginal) => {
  const actual = await importOriginal<typeof api>();
  return { ...actual, fetchSiteStatistics: vi.fn() };
});

const stats: api.SiteStatistics = {
  from: "2026-09-25",
  to: "2026-10-24",
  pageViews: 1234,
  uniquePageViews: 456,
  visitors: 78,
  days: [{ date: "2026-10-24", pageViews: 1234, uniquePageViews: 456, visitors: 78 }],
  topPages: [
    { path: "/", pageViews: 1000, uniquePageViews: 400 },
    { path: "/blogs", pageViews: 234, uniquePageViews: 56 },
  ],
};

describe("shortDate", () => {
  it("formats an ISO date as day and month", () => {
    expect(shortDate("2026-10-04")).toBe("4 Oct");
  });
});

describe("StatisticsPanel", () => {
  it("shows the totals and top pages for the last 30 days", async () => {
    vi.mocked(api.fetchSiteStatistics).mockResolvedValue(stats);

    render(<StatisticsPanel />);

    expect(await screen.findByText("1,234")).toBeInTheDocument();
    expect(screen.getByText("456")).toBeInTheDocument();
    expect(screen.getByText("78")).toBeInTheDocument();
    expect(screen.getByRole("cell", { name: "/blogs" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "30 days" })).toHaveAttribute("aria-pressed", "true");
    expect(api.fetchSiteStatistics).toHaveBeenCalledWith(30);
  });

  it("reloads for another period", async () => {
    const user = userEvent.setup();
    vi.mocked(api.fetchSiteStatistics).mockResolvedValue(stats);
    render(<StatisticsPanel />);
    await screen.findByText("1,234");

    await user.click(screen.getByRole("button", { name: "7 days" }));

    await waitFor(() => expect(api.fetchSiteStatistics).toHaveBeenLastCalledWith(7));
    expect(screen.getByRole("button", { name: "7 days" })).toHaveAttribute("aria-pressed", "true");
  });

  it("says so when there are no page views yet", async () => {
    vi.mocked(api.fetchSiteStatistics).mockResolvedValue({ ...stats, topPages: [] });

    render(<StatisticsPanel />);

    expect(await screen.findByText("No page views yet in this period.")).toBeInTheDocument();
  });

  it("shows an error when the statistics cannot be loaded", async () => {
    vi.mocked(api.fetchSiteStatistics).mockRejectedValue(
      new Error("Statistics request failed (500)"),
    );

    render(<StatisticsPanel />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Statistics request failed (500)");
  });
});
