# Visitor statistics

The admin dashboard has a **Statistics** tab (role `Admin.View`) with page views,
unique page views and unique visitors for the last 7, 30 or 90 days, a daily
trend and the ten most viewed pages. Issue #429.

The site counts visits itself, in its own PostgreSQL database. It sets no
cookie, stores no IP address or user agent, and sends nothing to a third party,
so no consent banner is needed.

## Why not Scaleway Cockpit logs

Scaleway Cockpit (Loki) only holds what the container writes to stdout. nginx
in the container does not write structured access logs there, logs are kept for
a limited time, and the app and the agents cannot query Cockpit. Counting from
logs would also count every bot and asset request. First-party counting is
cheaper (a few small tables in the existing database), keeps history as long as
we want, and needs nothing from the Scaleway console.

## How a page view is counted

1. `components/page-view-tracker.tsx` runs on every public page (not under
   `/admin`). On each navigation it sends the path with
   `POST /api/stats/pageview` (`lib/api/statsApi.ts`). The request has a JSON
   body `{ "path": "/blogs" }` and nothing else.
2. `RecordPageViewCommandHandler`
   (`src/Clercq.It.Application/Features/Statistics/RecordPageViewCommand.cs`)
   drops the view when the user agent looks like a bot, crawler, link preview,
   monitor, headless browser or HTTP library, when there is no IP address or user
   agent, and for `/admin` and `/api` paths. Paths are stored lower case without
   a trailing slash. The validator accepts only a plain path of at most 200
   characters (no query string), so a bad request gets a 400.
3. `EfSiteStatistics` (`src/Clercq.It.Infrastructure/Statistics/EfSiteStatistics.cs`)
   computes the visitor as the hex `SHA-256(daily salt, IP address, user agent)`. The
   salt is 32 random bytes per UTC day, kept in `StatsSalts`. It then adds one
   view to the row for (day, path, visitor) in `StatsPageVisits`.

The IP address is the first `X-Forwarded-For` entry (Scaleway's edge and then
nginx append to it), or the connection address. A client can fake that header;
the worst it can do is inflate the visitor count.

The beacon needs JavaScript, so most bots never send one. The endpoint has no
`api` rate limit policy of its own (every page view calls it); the global limit
still applies.

## Rollup and retention

After a UTC day ends, its rows are rolled up:

- `StatsDailyPages`: views and unique views per day and page.
- `StatsDays`: views, unique views and unique visitors per day.

Then that day's `StatsPageVisits` rows and its salt are deleted. Without the
salt nobody can recompute a hash from an IP address, so hashes never live
longer than the day plus the time until the next rollup.

The container scales to zero, so there is no timer. The rollup runs on the first
page view of a new day (once per container instance) and before every
statistics request. A PostgreSQL advisory lock keeps two instances from rolling
up the same day twice.

Daily totals are kept 400 days (`EfSiteStatistics.RetentionDays`); the rollup
deletes older ones.

## What the numbers mean

| Number | Meaning |
| --- | --- |
| Page views | Every page load or in-site navigation |
| Unique page views | Each page counted once per visitor per day |
| Unique visitors | Unique visitors per day, summed over the period |

The visitor hash changes every day, so someone who comes back on another day
counts again in a 7, 30 or 90 day total. A different browser or network (for
example phone on 4G and laptop on Wi-Fi) is a different visitor. Days are UTC
days. The owner's own visits to public pages count; admin pages do not.

## API

| Endpoint | Auth | What it does |
| --- | --- | --- |
| `POST /api/stats/pageview` | public | Counts one page view; always 204 unless the path is invalid (400) |
| `GET /api/stats?days=30` | `Admin.View` | Totals, daily trend and top pages; `days` 1 to 400, default 30 |

Both go through MediatR (`RecordPageViewCommand`, `GetSiteStatisticsQuery`) in
`src/ClercqIt.Api/Features/StatisticsEndpoints.cs`.

## Tests

- `tests/Clercq.It.Application.Tests/StatisticsHandlersTests.cs` and
  `StatisticsValidatorsTests.cs`: bot and path filtering, zero-filled days, totals, top pages.
- `tests/ClercqIt.Api.Tests/Statistics/EfSiteStatisticsTests.cs`: daily salts,
  rollup, deleting hashes and salts, retention, against real PostgreSQL.
- `tests/ClercqIt.Api.Tests/Statistics/StatisticsEndpointTests.cs`: the endpoints, 400, 401 and 403.
- `src/ClercqIt.Web/components/page-view-tracker.test.tsx`,
  `src/ClercqIt.Web/components/admin/StatisticsPanel.test.tsx` and
  `src/ClercqIt.Web/lib/api/statsApi.test.ts`.
