"use client";

import { useEffect, useState } from "react";
import { Eye, MousePointerClick, Users } from "lucide-react";
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { fetchSiteStatistics, type SiteStatistics } from "@/lib/api/statsApi";

// Visitor statistics (issue #429, docs/statistics.md).

export const PERIODS = [7, 30, 90] as const;
export type Period = (typeof PERIODS)[number];

const number = new Intl.NumberFormat("en-GB");

/** "24 Oct" for an ISO date, read as a UTC calendar day. */
export function shortDate(isoDate: string): string {
  return new Date(`${isoDate}T00:00:00Z`).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    timeZone: "UTC",
  });
}

export function StatisticsPanel() {
  const [period, setPeriod] = useState<Period>(30);
  const [stats, setStats] = useState<SiteStatistics | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetchSiteStatistics(period)
      .then((result) => {
        if (cancelled) return;
        setStats(result);
        setError(null);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Could not load statistics");
      });
    return () => {
      cancelled = true;
    };
  }, [period]);

  const chartData = stats?.days.map((d) => ({
    date: shortDate(d.date),
    "Page views": d.pageViews,
    Visitors: d.visitors,
  }));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm text-muted-foreground">
          {stats
            ? `${shortDate(stats.from)} – ${shortDate(stats.to)} (UTC days, today included)`
            : "Loading…"}
        </p>
        <div className="flex gap-2" role="group" aria-label="Period">
          {PERIODS.map((p) => (
            <Button
              key={p}
              size="sm"
              variant={p === period ? "default" : "outline"}
              aria-pressed={p === period}
              onClick={() => setPeriod(p)}
            >
              {p} days
            </Button>
          ))}
        </div>
      </div>

      {error && (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}

      {stats && (
        <>
          <div className="grid gap-6 md:grid-cols-3">
            <StatCard
              title="Page views"
              value={stats.pageViews}
              hint="Every page load"
              icon={<Eye className="h-4 w-4 text-muted-foreground" />}
            />
            <StatCard
              title="Unique page views"
              value={stats.uniquePageViews}
              hint="Each page once per visitor per day"
              icon={<MousePointerClick className="h-4 w-4 text-muted-foreground" />}
            />
            <StatCard
              title="Unique visitors"
              value={stats.visitors}
              hint="Summed per day: a returning visitor counts again"
              icon={<Users className="h-4 w-4 text-muted-foreground" />}
            />
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Trend</CardTitle>
              <CardDescription>Page views and unique visitors per day</CardDescription>
            </CardHeader>
            <CardContent>
              <div
                className="h-72 w-full"
                role="img"
                aria-label={`Daily page views and visitors over the last ${period} days`}
              >
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={chartData} margin={{ top: 8, right: 16, bottom: 0, left: 0 }}>
                    <CartesianGrid
                      vertical={false}
                      stroke="hsl(var(--border))"
                      strokeDasharray="3 3"
                    />
                    <XAxis
                      dataKey="date"
                      tickLine={false}
                      axisLine={false}
                      minTickGap={24}
                      tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
                    />
                    <YAxis
                      allowDecimals={false}
                      tickLine={false}
                      axisLine={false}
                      width={40}
                      tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
                    />
                    <Tooltip
                      contentStyle={{
                        background: "hsl(var(--popover))",
                        border: "1px solid hsl(var(--border))",
                        borderRadius: 8,
                        color: "hsl(var(--popover-foreground))",
                      }}
                    />
                    <Legend />
                    <Line
                      type="monotone"
                      dataKey="Page views"
                      stroke="hsl(var(--chart-1))"
                      strokeWidth={2}
                      dot={false}
                      activeDot={{ r: 4 }}
                    />
                    <Line
                      type="monotone"
                      dataKey="Visitors"
                      stroke="hsl(var(--chart-3))"
                      strokeWidth={2}
                      dot={false}
                      activeDot={{ r: 4 }}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Top pages</CardTitle>
              <CardDescription>The most viewed pages in this period</CardDescription>
            </CardHeader>
            <CardContent>
              {stats.topPages.length === 0 ? (
                <p className="text-sm text-muted-foreground">No page views yet in this period.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Page</TableHead>
                      <TableHead className="text-right">Views</TableHead>
                      <TableHead className="text-right">Unique views</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {stats.topPages.map((page) => (
                      <TableRow key={page.path}>
                        <TableCell className="font-mono text-sm break-all">{page.path}</TableCell>
                        <TableCell className="text-right tabular-nums">
                          {number.format(page.pageViews)}
                        </TableCell>
                        <TableCell className="text-right tabular-nums">
                          {number.format(page.uniquePageViews)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>

          <p className="text-xs text-muted-foreground">
            Counted on the site itself, without cookies. Visitors are a daily hash of IP address and
            browser; neither is stored. Bots and admin pages are not counted.
          </p>
        </>
      )}
    </div>
  );
}

function StatCard({
  title,
  value,
  hint,
  icon,
}: {
  title: string;
  value: number;
  hint: string;
  icon: React.ReactNode;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        {icon}
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold tabular-nums">{number.format(value)}</div>
        <p className="text-xs text-muted-foreground">{hint}</p>
      </CardContent>
    </Card>
  );
}
