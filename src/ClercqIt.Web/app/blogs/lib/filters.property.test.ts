import fc from "fast-check";
import { describe, expect, it } from "vitest";
import type { Blog } from "./api";
import { getArchive, matchesFilters, type BlogFilters } from "./filters";

// Property-based tests (issue #402): the blog filters take free text and
// query-string values, so they must cope with any input.

const MIN_DATE = new Date("2000-01-01T00:00:00Z");
const MAX_DATE = new Date("2100-12-31T23:59:59Z");

const blogArb: fc.Arbitrary<Blog> = fc.record({
  id: fc.uuid(),
  createdDate: fc.constant("2024-01-01T00:00:00Z"),
  publishDate: fc
    .date({ min: MIN_DATE, max: MAX_DATE, noInvalidDate: true })
    .map((date) => date.toISOString()),
  title: fc.string(),
  shortDescription: fc.string(),
  longDescription: fc.string(),
  image: fc.constant(""),
  tags: fc.array(fc.string({ minLength: 1, maxLength: 12 }), { maxLength: 4 }),
});

const filtersArb: fc.Arbitrary<BlogFilters> = fc.record({
  search: fc.string(),
  tags: fc.array(fc.string(), { maxLength: 3 }),
  year: fc.oneof(fc.constant(""), fc.integer({ min: 1990, max: 2110 }).map(String), fc.string()),
  month: fc.oneof(fc.constant(""), fc.integer({ min: 1, max: 12 }).map(String), fc.string()),
});

const none: BlogFilters = { search: "", tags: [], year: "", month: "" };

describe("getArchive (property)", () => {
  it("has every post's year and month, newest year first, months in order", () => {
    fc.assert(
      fc.property(fc.array(blogArb, { maxLength: 30 }), (blogs) => {
        const archive = getArchive(blogs);
        const years = archive.map((entry) => Number(entry.year));

        expect(years).toEqual([...years].sort((a, b) => b - a));
        for (const { months } of archive) {
          const numbers = months.map(Number);
          expect(numbers).toEqual([...numbers].sort((a, b) => a - b));
          expect(new Set(months).size).toBe(months.length);
        }
        for (const blog of blogs) {
          const date = new Date(blog.publishDate);
          const entry = archive.find((e) => e.year === String(date.getUTCFullYear()));
          expect(entry?.months).toContain(String(date.getUTCMonth() + 1));
        }
      }),
    );
  });
});

describe("matchesFilters (property)", () => {
  it("never throws, whatever the filters hold", () => {
    fc.assert(
      fc.property(blogArb, filtersArb, (blog, filters) => {
        expect(typeof matchesFilters(blog, filters)).toBe("boolean");
      }),
    );
  });

  it("matches every post when no filter is set", () => {
    fc.assert(fc.property(blogArb, (blog) => matchesFilters(blog, none)));
  });

  it("finds a post by any part of its title, in any case", () => {
    fc.assert(
      fc.property(blogArb, fc.nat(), fc.nat(), fc.boolean(), (blog, start, length, upper) => {
        const from = start % (blog.title.length + 1);
        const part = blog.title.slice(from, from + length);
        const search = upper ? part.toUpperCase() : part.toLowerCase();
        // Case folding is not reversible for every code point (e.g. "ß" -> "SS").
        fc.pre(search.toLowerCase() === part.toLowerCase());
        return matchesFilters(blog, { ...none, search });
      }),
    );
  });

  it("keeps a post only when the chosen year and month are its own", () => {
    fc.assert(
      fc.property(blogArb, filtersArb, (blog, filters) => {
        const date = new Date(blog.publishDate);
        const year = String(date.getUTCFullYear());
        const month = String(date.getUTCMonth() + 1);

        expect(matchesFilters(blog, { ...none, year: filters.year })).toBe(
          filters.year === "" || filters.year === year,
        );
        expect(matchesFilters(blog, { ...none, month: filters.month })).toBe(
          filters.month === "" || filters.month === month,
        );
      }),
    );
  });
});
