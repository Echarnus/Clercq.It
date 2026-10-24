import type { Metadata } from "next";

// SEO helpers: the public site URL, readable detail URLs, page metadata and JSON-LD (docs/seo.md).

// The canonical origin. SITE_URL is read at runtime on the server, so a preview can override it.
export const SITE_URL = (process.env.SITE_URL || "https://www.clercq.it").replace(/\/+$/, "");
export const SITE_NAME = "Clercq.It";
export const SITE_DESCRIPTION =
  "Developer and Solution Architect specializing in .NET, TypeScript, React, Angular, and Cloud Technologies";
export const AUTHOR_NAME = "Kenneth De Clercq";
export const AUTHOR_SAME_AS = [
  "https://www.linkedin.com/in/kenneth-de-clercq-2b834269/",
  "https://github.com/Echarnus",
];

// Shared preview image for pages without their own (public/og-image.png, 1200x630).
export const DEFAULT_OG_IMAGE = "/og-image.png";

const SHORT_ID_LENGTH = 8;
const MAX_SLUG_LENGTH = 80;
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const SHORT_ID_SUFFIX = new RegExp(`(?:^|-)([0-9a-f]{${SHORT_ID_LENGTH}})$`, "i");

/** Lowercase ASCII words joined by hyphens: "Café & .NET 10" becomes "cafe-net-10". */
export function slugify(text: string): string {
  const slug = text
    .normalize("NFKD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  if (slug.length <= MAX_SLUG_LENGTH) return slug;
  // Cut at a word boundary so a URL never ends in half a word.
  const cut = slug.slice(0, MAX_SLUG_LENGTH);
  const lastDash = cut.lastIndexOf("-");
  return (lastDash > 0 ? cut.slice(0, lastDash) : cut).replace(/-+$/, "");
}

interface Sluggable {
  id: string;
  title: string;
}

/** The last path segment of a detail page: the title slug plus the first 8 characters of the id. */
export function itemSegment(item: Sluggable): string {
  const shortId = item.id.slice(0, SHORT_ID_LENGTH).toLowerCase();
  const slug = slugify(item.title);
  return slug ? `${slug}-${shortId}` : shortId;
}

export const projectPath = (project: Sluggable) => `/portfolio/${itemSegment(project)}`;
export const blogPath = (blog: Sluggable) => `/blogs/${itemSegment(blog)}`;

export type ItemParam = { kind: "guid"; id: string } | { kind: "short"; shortId: string };

/** Reads a detail URL segment: an old full GUID, or a `<slug>-<short id>` segment. */
export function parseItemParam(segment: string): ItemParam | null {
  const value = decodeURIComponent(segment).trim();
  if (GUID.test(value)) return { kind: "guid", id: value.toLowerCase() };
  const match = SHORT_ID_SUFFIX.exec(value);
  return match ? { kind: "short", shortId: match[1].toLowerCase() } : null;
}

/**
 * Finds the item a detail URL points to. A full GUID is fetched directly; a short id is matched
 * against the list (the site has dozens of items, not thousands). When two ids share the short
 * id, the one whose full segment matches wins.
 */
export async function resolveItem<T extends Sluggable>(
  segment: string,
  fetchById: (id: string) => Promise<T | null>,
  fetchAll: () => Promise<T[]>,
): Promise<T | null> {
  const param = parseItemParam(segment);
  if (!param) return null;
  if (param.kind === "guid") return fetchById(param.id);

  const candidates = (await fetchAll()).filter((item) =>
    item.id.toLowerCase().startsWith(param.shortId),
  );
  return candidates.find((item) => itemSegment(item) === segment) ?? candidates[0] ?? null;
}

/** An absolute URL on the site, for JSON-LD (metadata resolves relative URLs itself). */
export const absoluteUrl = (path: string) =>
  /^https?:\/\//i.test(path) ? path : `${SITE_URL}${path.startsWith("/") ? "" : "/"}${path}`;

/** A plain-text summary that fits a meta description (about 160 characters). */
export function metaDescription(text: string, max = 160): string {
  const plain = text
    .replace(/[#*_`>[\]()!]/g, "")
    .replace(/\s+/g, " ")
    .trim();
  if (plain.length <= max) return plain;
  const cut = plain.slice(0, max - 1);
  const lastSpace = cut.lastIndexOf(" ");
  return `${(lastSpace > 0 ? cut.slice(0, lastSpace) : cut).replace(/[\s,.;:]+$/, "")}…`;
}

export const personJsonLd = () => ({
  "@type": "Person",
  "@id": `${SITE_URL}/#person`,
  name: AUTHOR_NAME,
  url: SITE_URL,
  jobTitle: "Developer and Solution Architect",
  sameAs: AUTHOR_SAME_AS,
});

export const breadcrumbJsonLd = (items: { name: string; path: string }[]) => ({
  "@type": "BreadcrumbList",
  itemListElement: items.map((item, index) => ({
    "@type": "ListItem",
    position: index + 1,
    name: item.name,
    item: absoluteUrl(item.path),
  })),
});

interface PageMetadataInput {
  title: string;
  description: string;
  path: string;
  type?: "website" | "article" | "profile";
  image?: string;
  imageAlt?: string;
  openGraph?: Record<string, unknown>;
}

/**
 * Title, description, canonical URL and the Open Graph and Twitter cards for one page. Next.js
 * merges metadata per top-level key, so each page sets the whole openGraph object here.
 */
export function pageMetadata({
  title,
  description,
  path,
  type = "website",
  image,
  imageAlt,
  openGraph,
}: PageMetadataInput): Metadata {
  const images = image
    ? [{ url: image, alt: imageAlt ?? title }]
    : [{ url: DEFAULT_OG_IMAGE, width: 1200, height: 630, alt: SITE_NAME }];
  return {
    title,
    description,
    alternates: { canonical: path },
    openGraph: {
      type,
      siteName: SITE_NAME,
      locale: "en_US",
      url: path,
      title,
      description,
      images,
      ...openGraph,
    },
    twitter: {
      card: image ? "summary" : "summary_large_image",
      title,
      description,
      images: images.map((i) => i.url),
    },
  };
}
