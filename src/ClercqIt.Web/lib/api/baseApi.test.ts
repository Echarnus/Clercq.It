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

  it("always uses a full URL on the server", () => {
    vi.stubEnv("NODE_ENV", "production");
    vi.stubGlobal("window", undefined);
    try {
      expect(getApiUrl()).toBe(API);
    } finally {
      vi.unstubAllGlobals();
    }
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
