import type { Blog } from "./api";

export const MONTH_NAMES = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
];

export interface BlogFilters {
  search: string;
  tags: string[];
  /** Four-digit year, or "" for any year. */
  year: string;
  /** Month number 1-12 as a string, or "" for any month of the year. */
  month: string;
}

// Publish dates are ISO timestamps in UTC; read them in UTC so the filter does not shift a
// post into the previous or next month depending on the visitor's time zone.
const yearOf = (blog: Blog) => String(new Date(blog.publishDate).getUTCFullYear());
const monthOf = (blog: Blog) => String(new Date(blog.publishDate).getUTCMonth() + 1);

/** The years that have posts, newest first, each with its months that have posts, in order. */
export function getArchive(blogs: Blog[]): { year: string; months: string[] }[] {
  const archive = new Map<string, Set<string>>();
  for (const blog of blogs) {
    const months = archive.get(yearOf(blog)) ?? new Set<string>();
    months.add(monthOf(blog));
    archive.set(yearOf(blog), months);
  }
  return [...archive.entries()]
    .sort(([a], [b]) => Number(b) - Number(a))
    .map(([year, months]) => ({
      year,
      months: [...months].sort((a, b) => Number(a) - Number(b)),
    }));
}

export function matchesFilters(blog: Blog, filters: BlogFilters): boolean {
  const search = filters.search.trim().toLowerCase();
  if (
    search &&
    !blog.shortDescription.toLowerCase().includes(search) &&
    !blog.longDescription.toLowerCase().includes(search)
  ) {
    return false;
  }

  if (filters.tags.length > 0 && !filters.tags.some((tag) => blog.tags.includes(tag))) {
    return false;
  }

  if (filters.year && yearOf(blog) !== filters.year) return false;
  if (filters.month && monthOf(blog) !== filters.month) return false;

  return true;
}
