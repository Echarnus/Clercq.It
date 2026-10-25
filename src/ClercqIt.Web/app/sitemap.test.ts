import { describe, expect, it, vi } from "vitest";
import robots from "./robots";
import sitemap from "./sitemap";

vi.mock("./portfolio/lib/api", () => ({
  fetchAllProjects: () =>
    Promise.resolve([{ id: "5cbc25fa-36da-b955-b8f6-e4e4fbfe446e", title: "Project Pica" }]),
}));
vi.mock("./blogs/lib/api", () => ({
  fetchAllBlogs: () =>
    Promise.resolve([
      {
        id: "22222222-2222-4222-8222-222222222222",
        title: "Hello world",
        publishDate: "2026-10-01T00:00:00Z",
      },
    ]),
}));

describe("sitemap", () => {
  it("lists the public pages and every project and blog by its readable URL", async () => {
    const urls = (await sitemap()).map((entry) => entry.url);
    expect(urls).toEqual([
      "https://www.clercq.it/",
      "https://www.clercq.it/portfolio",
      "https://www.clercq.it/blogs",
      "https://www.clercq.it/certifications",
      "https://www.clercq.it/portfolio/project-pica-5cbc25fa",
      "https://www.clercq.it/blogs/hello-world-22222222",
    ]);
  });
});

describe("robots", () => {
  it("keeps crawlers out of the admin and the API and points to the sitemap", () => {
    expect(robots()).toEqual({
      rules: { userAgent: "*", allow: "/", disallow: ["/admin", "/api/"] },
      sitemap: "https://www.clercq.it/sitemap.xml",
      host: "https://www.clercq.it",
    });
  });
});
