import { http, HttpResponse } from "msw";
import { afterEach, describe, expect, it, vi } from "vitest";
import { API, blog, project } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import {
  fetchBlog,
  fetchBlogs,
  fetchProject,
  fetchProjects,
  getApiUrl,
  getBlogsBaseUrl,
  getProjectsBaseUrl,
  BlogSchema,
  ProjectSchema,
} from "./baseApi";

describe("API URLs", () => {
  afterEach(() => vi.unstubAllEnvs());

  it("uses localhost:5001 outside production when no URL is configured", () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    expect(getApiUrl()).toBe(API);
    expect(getProjectsBaseUrl()).toBe(`${API}/api/projects`);
    expect(getBlogsBaseUrl()).toBe(`${API}/api/blogs`);
  });

  it("uses NEXT_PUBLIC_API_URL when set", () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://api.example.com");
    expect(getApiUrl()).toBe("https://api.example.com");
  });

  it("uses relative URLs in the browser in production", () => {
    vi.stubEnv("NODE_ENV", "production");
    expect(getApiUrl()).toBe("");
  });

  describe("on the server", () => {
    afterEach(() => vi.unstubAllGlobals());

    it("uses API_INTERNAL_URL first", () => {
      vi.stubGlobal("window", undefined);
      vi.stubEnv("NODE_ENV", "production");
      vi.stubEnv("API_INTERNAL_URL", "http://localhost:5000");
      vi.stubEnv("NEXT_PUBLIC_API_URL", "https://api.example.com");
      expect(getApiUrl()).toBe("http://localhost:5000");
    });

    it("falls back to NEXT_PUBLIC_API_URL", () => {
      vi.stubGlobal("window", undefined);
      vi.stubEnv("API_INTERNAL_URL", "");
      vi.stubEnv("NEXT_PUBLIC_API_URL", "https://api.example.com");
      expect(getApiUrl()).toBe("https://api.example.com");
    });

    it("uses the container API port in production when nothing is configured", () => {
      vi.stubGlobal("window", undefined);
      vi.stubEnv("NODE_ENV", "production");
      vi.stubEnv("API_INTERNAL_URL", "");
      vi.stubEnv("NEXT_PUBLIC_API_URL", "");
      expect(getApiUrl()).toBe("http://localhost:5000");
    });

    it("uses the Aspire API port in development when nothing is configured", () => {
      vi.stubGlobal("window", undefined);
      vi.stubEnv("NODE_ENV", "development");
      vi.stubEnv("API_INTERNAL_URL", "");
      vi.stubEnv("NEXT_PUBLIC_API_URL", "");
      expect(getApiUrl()).toBe(API);
    });
  });
});

describe("fetchProjects / fetchProject", () => {
  it("returns validated projects", async () => {
    expect(await fetchProjects("")).toEqual([project]);
    expect(await fetchProjects("/featured")).toEqual([project]);
    expect(await fetchProject(`/${project.id}`)).toEqual(project);
  });

  it("returns empty results when the API fails", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    server.use(http.get(`${API}/api/projects*`, () => new HttpResponse(null, { status: 500 })));
    expect(await fetchProjects("")).toEqual([]);
    expect(await fetchProject(`/${project.id}`)).toBeNull();
  });

  it("rejects data that does not match the schema", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    server.use(http.get(`${API}/api/projects*`, () => HttpResponse.json({ id: "not-a-uuid" })));
    expect(await fetchProjects("")).toEqual([]);
    expect(await fetchProject(`/${project.id}`)).toBeNull();
  });

  it("returns empty results on a network error", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    server.use(http.get(`${API}/api/projects*`, () => HttpResponse.error()));
    expect(await fetchProjects("")).toEqual([]);
    expect(await fetchProject(`/${project.id}`)).toBeNull();
  });
});

describe("fetchBlogs / fetchBlog", () => {
  it("returns validated blogs", async () => {
    expect(await fetchBlogs("")).toEqual([blog]);
    expect(await fetchBlog(`/${blog.id}`)).toEqual(blog);
  });

  it("returns empty results when the API fails", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    server.use(http.get(`${API}/api/blogs*`, () => new HttpResponse(null, { status: 503 })));
    expect(await fetchBlogs("")).toEqual([]);
    expect(await fetchBlog(`/${blog.id}`)).toBeNull();
  });

  it("rejects data that does not match the schema", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    server.use(http.get(`${API}/api/blogs*`, () => HttpResponse.json([{ id: 1 }])));
    expect(await fetchBlogs("")).toEqual([]);
    server.use(http.get(`${API}/api/blogs*`, () => HttpResponse.json({ id: 1 })));
    expect(await fetchBlog(`/${blog.id}`)).toBeNull();
  });

  it("returns empty results on a network error", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    server.use(http.get(`${API}/api/blogs*`, () => HttpResponse.error()));
    expect(await fetchBlogs("")).toEqual([]);
    expect(await fetchBlog(`/${blog.id}`)).toBeNull();
  });
});

describe("schemas", () => {
  it("accept any .NET Guid, not only RFC 9562 UUIDs (zod 4's uuid() is stricter)", () => {
    const seededId = "00000000-0000-0000-0000-000000000001";

    expect(ProjectSchema.safeParse({ ...project, id: seededId }).success).toBe(true);
    expect(BlogSchema.safeParse({ ...blog, id: seededId }).success).toBe(true);
  });
});
