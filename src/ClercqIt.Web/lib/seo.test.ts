import { describe, expect, it, vi } from "vitest";
import {
  blogPath,
  breadcrumbJsonLd,
  itemSegment,
  metaDescription,
  pageMetadata,
  parseItemParam,
  projectPath,
  resolveItem,
  slugify,
} from "./seo";

const project = { id: "5cbc25fa-36da-b955-b8f6-e4e4fbfe446e", title: "Certia: Child Benefit 2.0" };

describe("slugify", () => {
  it.each([
    ["Certia: Child Benefit 2.0", "certia-child-benefit-2-0"],
    ["Café & .NET 10", "cafe-net-10"],
    ["  --Hello   World--  ", "hello-world"],
    ["日本語", ""],
  ])("turns %j into %j", (title, slug) => {
    expect(slugify(title)).toBe(slug);
  });

  it("cuts long titles at a word boundary", () => {
    const slug = slugify("word ".repeat(40));
    expect(slug.length).toBeLessThanOrEqual(80);
    expect(slug.endsWith("word")).toBe(true);
  });
});

describe("detail paths", () => {
  it("joins the slug and the first 8 characters of the id", () => {
    expect(itemSegment(project)).toBe("certia-child-benefit-2-0-5cbc25fa");
    expect(projectPath(project)).toBe("/portfolio/certia-child-benefit-2-0-5cbc25fa");
    expect(blogPath(project)).toBe("/blogs/certia-child-benefit-2-0-5cbc25fa");
  });

  it("falls back to the short id when the title has no usable characters", () => {
    expect(itemSegment({ id: project.id, title: "日本語" })).toBe("5cbc25fa");
  });
});

describe("parseItemParam", () => {
  it("recognises an old full GUID", () => {
    expect(parseItemParam(project.id.toUpperCase())).toEqual({ kind: "guid", id: project.id });
  });

  it("reads the short id from a slug segment", () => {
    expect(parseItemParam("certia-child-benefit-2-0-5cbc25fa")).toEqual({
      kind: "short",
      shortId: "5cbc25fa",
    });
    expect(parseItemParam("5CBC25FA")).toEqual({ kind: "short", shortId: "5cbc25fa" });
  });

  it.each(["certia", "certia-5cbc25f", "certia-5cbc25fz", ""])("rejects %j", (segment) => {
    expect(parseItemParam(segment)).toBeNull();
  });
});

describe("resolveItem", () => {
  const other = { id: "5cbc25fa-0000-4000-8000-000000000000", title: "Other" };

  it("fetches a GUID directly", async () => {
    const fetchById = vi.fn().mockResolvedValue(project);
    const fetchAll = vi.fn();
    await expect(resolveItem(project.id, fetchById, fetchAll)).resolves.toBe(project);
    expect(fetchById).toHaveBeenCalledWith(project.id);
    expect(fetchAll).not.toHaveBeenCalled();
  });

  it("matches the short id, preferring the exact segment when two ids share it", async () => {
    const fetchAll = vi.fn().mockResolvedValue([other, project]);
    await expect(resolveItem(itemSegment(project), vi.fn(), fetchAll)).resolves.toBe(project);
    await expect(resolveItem("stale-title-5cbc25fa", vi.fn(), fetchAll)).resolves.toBe(other);
  });

  it("returns null for unknown or malformed segments", async () => {
    const fetchAll = vi.fn().mockResolvedValue([project]);
    await expect(resolveItem("nothing-12345678", vi.fn(), fetchAll)).resolves.toBeNull();
    await expect(resolveItem("no-id", vi.fn(), fetchAll)).resolves.toBeNull();
  });
});

describe("metaDescription", () => {
  it("strips Markdown and whitespace", () => {
    expect(metaDescription("**Bold**  and [a link](x)\n# Heading")).toBe(
      "Bold and a linkx Heading",
    );
  });

  it("shortens long text at a word boundary with an ellipsis", () => {
    const text = metaDescription("lorem ipsum ".repeat(30));
    expect(text.length).toBeLessThanOrEqual(160);
    expect(text.endsWith("ipsum…") || text.endsWith("lorem…")).toBe(true);
  });
});

describe("pageMetadata", () => {
  it("sets the canonical URL and the default share image", () => {
    const metadata = pageMetadata({ title: "Blog", description: "Posts", path: "/blogs" });
    expect(metadata.alternates?.canonical).toBe("/blogs");
    expect(metadata.openGraph).toMatchObject({
      url: "/blogs",
      siteName: "Clercq.It",
      images: [{ url: "/og-image.png" }],
    });
    expect(metadata.twitter).toMatchObject({ card: "summary_large_image" });
  });

  it("uses the item image when there is one", () => {
    const metadata = pageMetadata({
      title: "A",
      description: "B",
      path: "/a",
      image: "https://cdn.example/a.png",
    });
    expect(metadata.openGraph).toMatchObject({ images: [{ url: "https://cdn.example/a.png" }] });
    expect(metadata.twitter).toMatchObject({ card: "summary" });
  });
});

describe("breadcrumbJsonLd", () => {
  it("numbers the items and makes the URLs absolute", () => {
    expect(breadcrumbJsonLd([{ name: "Home", path: "/" }])).toEqual({
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: "https://www.clercq.it/" },
      ],
    });
  });
});
