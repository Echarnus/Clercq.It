# UX and page layout

How the public site is laid out, and the UX review that set these rules (issue #356).

## Layout rules

- **One content width.** Every public page, the header and the footer wrap their content in `PageContainer` (`src/ClercqIt.Web/components/page-container.tsx`): `max-w-6xl` (1152px) with a `px-4 sm:px-6 lg:px-8` gutter. The logo, the page content and the footer share the same left and right edges on every route.
- **Do not narrow the container per page.** Narrow the text inside it instead. Intro paragraphs use `max-w-3xl`; centered blocks (the home hero, the 404 page) use `mx-auto max-w-3xl`.
- **Page shell.** `src/ClercqIt.Web/components/conditional-layout.tsx` renders the header, one `<main>` with the page background, and the footer. Pages render only their sections; they do not set their own background or font.
- **No layout shift.** `src/ClercqIt.Web/app/globals.css` sets `scrollbar-gutter: stable` on `html`, so short pages (no scrollbar) and long pages (scrollbar) have the same content width.
- **One font.** The system font stack is the Tailwind `--font-sans` token in `globals.css`. Do not add inline `fontFamily` styles.
- **List pages.** Portfolio and Certifications show a two-column card grid from `lg`; Blogs shows posts in three columns of a four-column grid with the filters in the fourth.
- **Detail pages.** Blog and project details use the same four-column grid: the title and body take three columns, a details card (dates, tags, skills) takes the fourth, starts at the same top edge as the body card (both sit in the row below the title) and stays in view while you scroll. On phones the details card sits between the title and the body.
- **Sticky elements** sit below the sticky header: use `lg:top-28`, not `top-4`.
- **Headings and titles.** One `<h1>` per page (the page or item title). The logo is a link to the homepage, not a heading. Top-level pages export `metadata.title`; the root layout turns it into `<title> | Clercq.It`.
- **Navigation.** The nav highlights the section of the current page, including detail pages (`/blogs/<id>` highlights Blogs), and marks it with `aria-current="page"` (`getActiveNavKey` in `src/ClercqIt.Web/components/navigation.tsx`). Top-level pages have no "Back to Home" button; detail pages keep their back link.

Admin pages (`/admin`) render without the public header and footer; see [Admin navigation](#admin-navigation).

## Admin navigation

The admin dashboard uses a side menu instead of a tab bar, so it scales as sections are added (issue #449).

- **One list.** `ADMIN_NAV` in `src/ClercqIt.Web/components/admin/admin-sections.tsx` is the whole menu: groups (Portfolio, Statistics, Administration), which can hold items or further groups, and items with a key, label, icon, roles, optional feature flag, description and content component. The helpers in `src/ClercqIt.Web/lib/adminNav.ts` filter it by role and flag, drop empty groups and find the current item.
- **Add a section** by adding one item. It gets a menu entry, the URL `/admin/dashboard/<key>`, role filtering, a heading and a line on the Overview. Keys are unique across the tree.
- **URLs.** Overview is `/admin/dashboard`; every other section is `/admin/dashboard/<key>` (`src/ClercqIt.Web/app/admin/dashboard/[section]/page.tsx`). A reload or a shared link signs in and comes back to the same section. Unknown keys show "Section not found"; a section your roles do not open says which role it needs.
- **Shell.** `src/ClercqIt.Web/components/admin/AdminShell.tsx` (used by `src/ClercqIt.Web/app/admin/dashboard/layout.tsx`) signs the user in and renders a sticky header (title, theme picker, logout), the menu and the section in a `PageContainer`, so admin content has the same `max-w-6xl` width as the public site.
- **Desktop (from `lg`).** The menu is a sticky 256px pane. The header button folds it to a 64px icon rail (labels move to tooltips, groups become dividers); the choice is kept in `localStorage` (`admin-sidebar-collapsed`).
- **Phones and tablets.** The menu button opens the same menu as a drawer from the left; following a link closes it.
- **Current section.** Highlighted with the primary colour and marked `aria-current="page"`. A group that holds the current section cannot be folded away.
- **Keyboard and screen readers.** The menu is a `nav` labelled "Admin". Group headers are buttons with `aria-expanded`; Up and Down move between links and group buttons, Home and End jump to the first and last. Icons are hidden from screen readers.
- **Themes.** The menu uses the theme tokens (`bg-card`, `bg-primary`, `text-muted-foreground`, `border-border`), so it follows the light and dark slate themes.

## Themes

The site has a light and a dark theme (issue #410). Both use the Tailwind slate scale, so the public pages, the shadcn components and the admin area look the same.

- **Picker.** The header button (`src/ClercqIt.Web/components/theme-toggle.tsx`) opens a menu with Light, Dark and System. System is the default and follows the OS setting. `next-themes` keeps the choice in `localStorage` (key `theme`) and sets the `dark` class and `color-scheme` on `<html>` before the page paints, so there is no flash and native controls (scrollbars, date pickers) match.
- **Tokens.** The colours are HSL tokens in `src/ClercqIt.Web/app/globals.css` (`:root` for light, `.dark` for dark), exposed to Tailwind through `@theme` (`bg-background`, `text-muted-foreground`, `border-border` and so on). Light uses slate-50 pages, white cards and slate-900 text and buttons; dark uses slate-900 pages, slate-800 cards and slate-50 text. There is no pure black.
- **New UI uses the tokens.** Prefer `bg-card`, `text-foreground`, `text-muted-foreground` and `border-border` over hard-coded `slate-*` pairs with `dark:` overrides. Older pages still use slate classes; they match the tokens, so convert them when you touch them.
- **Browser chrome.** The root layout exports `viewport.themeColor`: white for a light OS, slate-900 (`#0f172a`) for a dark one.
- **Check both themes.** Screenshot a UI change in light and dark, for example with Playwright `newPage({ colorScheme: "dark" })`.

## UX review (2026-10-23)

Measured at 1440px wide before the change: the logo started at 96px from the left, while page content started at 272px (Portfolio, Certifications, details, 404), 144px (Blogs) or both 272px and 96px on one page (home). The content edge jumped on every navigation.

Fixed in #356:

| Finding | Fix |
| --- | --- |
| Content width switched between `max-w-4xl`, `max-w-6xl` and the header's `container` | One `PageContainer` everywhere |
| Page shifted sideways between short and long pages | `scrollbar-gutter: stable` |
| Fonts switched between the header (system font) and pages (inline Arial) | One `--font-sans` stack |
| Browser title read "Clercqlt", same on every page | "Clercq.It", per-page titles |
| Logo was an `<h1>` and not a link; detail pages had two `<h1>`s | Logo links home; one `<h1>` per page |
| No nav item active on detail pages | Section highlight and `aria-current` |
| No `<main>` landmark | The layout renders one `<main>` |
| Blogs: filters slid under the sticky header | `lg:top-28` |
| Blogs: with any filter set, the pager stayed on page 1 | Reset the page in the filter handlers, not during render |
| Portfolio and Certifications cards had uneven heights and a gap under the image | Cards fill the grid row; the image stretches |
| Phone: the closed menu's shadow showed on the right edge | Shadow only while the menu is open |
| "Back to Home" duplicated the nav on top-level pages | Removed there |

Fixed in #367:

| Finding | Fix |
| --- | --- |
| Markdown bodies showed as plain text (`**bold**`, raw links); `prose` classes had no plugin | `Markdown` component (`react-markdown` + `remark-gfm`) and `@tailwindcss/typography` |
| Detail pages had no browser title of their own | `generateMetadata` on blog and project pages, sharing the fetch through React `cache` |
| Blog date filter offered ten years and twelve months, with or without posts; picking a year alone filtered to January | Years and months come from the posts; the month is optional |
| Blog search only matched the short description | Search matches the short description and the body |
| Footer "Featured Projects" swapped its only link for the list once loaded | The list loads above a fixed "View all projects" link |

Fixed in #368: blog posts have a `title` (required, max 200). It is the card heading, the detail page `<h1>` and the browser title; the short description is the summary under it. The `AddBlogTitle` migration fills existing posts with their short description.

## Markdown

Bodies of projects, blog posts and certifications render through `src/ClercqIt.Web/components/markdown.tsx`: GitHub Flavored Markdown (tables, task lists, strikethrough), styled with the Tailwind typography plugin. Raw HTML in a body is shown as text, never rendered. Links to other sites open in a new tab.

## Checking a layout change

`pnpm build` and `pnpm start` need the API for real content. Without it, pages render their empty states. To check widths, compare the left edge of the logo (`header a`) with the page sections in the browser dev tools at 1440px and 390px; they should match on every route.
