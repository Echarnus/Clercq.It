import { describe, expect, it } from "vitest";
import type { Blog } from "./api";
import { getArchive, matchesFilters, type BlogFilters } from "./filters";

const blog = (publishDate: string, overrides: Partial<Blog> = {}): Blog => ({
  id: "22222222-2222-4222-8222-222222222222",
  createdDate: publishDate,
  publishDate,
  title: "Shipping on every merge",
  shortDescription: "Trunk-based delivery",
  longDescription: "How we deploy **every merge** to production.",
  image: "",
  tags: ["devops"],
  ...overrides,
});

const none: BlogFilters = { search: "", tags: [], year: "", month: "" };

describe("getArchive", () => {
  it("lists only years and months that have posts, newest year first", () => {
    const archive = getArchive([
      blog("2023-11-05T00:00:00Z"),
      blog("2024-03-01T00:00:00Z"),
      blog("2024-01-15T00:00:00Z"),
      blog("2024-03-20T00:00:00Z"),
    ]);

    expect(archive).toEqual([
      { year: "2024", months: ["1", "3"] },
      { year: "2023", months: ["11"] },
    ]);
  });

  it("reads dates in UTC", () => {
    expect(getArchive([blog("2024-01-31T23:30:00Z")])).toEqual([{ year: "2024", months: ["1"] }]);
  });
});

describe("matchesFilters", () => {
  const post = blog("2024-03-20T00:00:00Z");

  it("matches everything without filters", () => {
    expect(matchesFilters(post, none)).toBe(true);
  });

  it("searches the title, the summary and the body, ignoring case", () => {
    expect(matchesFilters(post, { ...none, search: "shipping" })).toBe(true);
    expect(matchesFilters(post, { ...none, search: "TRUNK" })).toBe(true);
    expect(matchesFilters(post, { ...none, search: "every merge" })).toBe(true);
    expect(matchesFilters(post, { ...none, search: "kubernetes" })).toBe(false);
  });

  it("matches any selected tag", () => {
    expect(matchesFilters(post, { ...none, tags: ["dotnet", "devops"] })).toBe(true);
    expect(matchesFilters(post, { ...none, tags: ["dotnet"] })).toBe(false);
  });

  it("filters by year alone, or by year and month", () => {
    expect(matchesFilters(post, { ...none, year: "2024" })).toBe(true);
    expect(matchesFilters(post, { ...none, year: "2023" })).toBe(false);
    expect(matchesFilters(post, { ...none, year: "2024", month: "3" })).toBe(true);
    expect(matchesFilters(post, { ...none, year: "2024", month: "4" })).toBe(false);
  });
});
