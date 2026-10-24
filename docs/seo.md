# SEO

How the public site tells search engines and link previews what each page is (issue #424). The code lives in `src/ClercqIt.Web/lib/seo.ts`.

## Readable detail URLs

Project and blog detail pages use the title plus a short id:

```text
/portfolio/certia-child-benefit-2-0-5cbc25fa
/blogs/hello-world-22222222
```

- The slug is the title in lowercase ASCII words joined by hyphens (`slugify()`), at most 80 characters, cut at a word boundary.
- The suffix is the first 8 characters of the item's GUID. The page finds the item by that suffix, so the slug part is only for people and search engines.
- Nothing is stored: the slug comes from the title, so there is no database column, migration or admin field. Change a title in the admin UI or through the MCP server and the URL follows.
- Every other spelling redirects permanently (HTTP 308, Next.js `permanentRedirect()`) to the canonical URL: the old full-GUID URLs (`/portfolio/5cbc25fa-36da-b955-b8f6-e4e4fbfe446e`), a stale slug after a title change, and uppercase ids.
- A segment without a valid short id, or a short id that matches nothing, is a 404.
- If two items ever share the first 8 characters of their id, the one whose full segment matches wins; the other is still reachable by its GUID URL.
- Build links with `projectPath()` and `blogPath()`, never from `id` directly.

Certifications have no detail page, so their URLs do not change.

## Page metadata

`src/ClercqIt.Web/app/layout.tsx` sets the site-wide defaults: `metadataBase`, the title template `%s | Clercq.It`, the description, `index, follow`, and the default Open Graph and Twitter cards.

Each public page calls `pageMetadata()` for its own title, description, canonical URL and cards. Next.js merges metadata per top-level key, so a page that sets `openGraph` replaces the whole object; `pageMetadata()` always fills it completely.

| Page | Title | Canonical |
| --- | --- | --- |
| `/` and `/home` | Kenneth De Clercq - Developer & Solution Architect | `/` |
| `/portfolio` | Portfolio | `/portfolio` |
| `/blogs` | Blog | `/blogs` |
| `/certifications` | Certifications | `/certifications` |
| Project detail | Project title | its slug URL |
| Blog detail | Post title | its slug URL |

Detail descriptions come from the short description (or the body), with Markdown stripped and cut to 160 characters (`metaDescription()`). The admin pages carry `noindex, nofollow`.

## Share images

Items with an image use it for Open Graph and a `summary` Twitter card. Everything else uses `src/ClercqIt.Web/public/og-image.png` (1200x630) with a `summary_large_image` card.

## Structured data (JSON-LD)

`src/ClercqIt.Web/components/json-ld.tsx` renders a `<script type="application/ld+json">`. It escapes `<` so content can never close the script element.

| Page | Types |
| --- | --- |
| Home | `Person`, `WebSite` |
| Project detail | `CreativeWork` (author `Person`), `BreadcrumbList` |
| Blog detail | `BlogPosting` (author and publisher `Person`), `BreadcrumbList` |

Check a page with the [Rich Results Test](https://search.google.com/test/rich-results) or the [Schema Markup Validator](https://validator.schema.org/).

## Sitemap and robots.txt

- `/sitemap.xml` (`src/ClercqIt.Web/app/sitemap.ts`) lists the four public pages plus every project and blog post by its readable URL. It is built per request, so content added through the admin UI or MCP shows up at once.
- `/robots.txt` (`src/ClercqIt.Web/app/robots.ts`) allows everything except `/admin` and `/api/`, and points to the sitemap. It is generated at build time.

nginx passes both to Next.js through its `location /` block; no change was needed.

## Site URL

Absolute URLs use `https://www.clercq.it`. Set `SITE_URL` on the server to override it (for example a preview environment). The robots file is static, so it keeps the build-time value.

## Headings and alt text

- Every public page has one `h1`. Section headings below it are `h2`; the footer column titles are `h2` too, so no heading level is skipped.
- Content images use the item title as alt text. Decorative SVG icons are `aria-hidden`.

## Check it locally

Build, start against a mock API and look at the result:

```bash
cd src/ClercqIt.Web && pnpm build && pnpm start
curl -sI http://localhost:3000/portfolio/<full guid>   # 308 to the slug URL
curl -s http://localhost:3000/sitemap.xml
curl -s http://localhost:3000/robots.txt
```

After a deploy, submit `https://www.clercq.it/sitemap.xml` in Google Search Console (owner step; it needs the owner's Google account).
