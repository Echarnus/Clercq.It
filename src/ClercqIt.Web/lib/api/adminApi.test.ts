// jsdom's File and FormData do not serialize through Node 24's fetch (content-length mismatch); the API client needs no DOM.
// @vitest-environment node
import { http, HttpResponse } from "msw";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { API, blog, certification, project } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { getAccessToken } from "../auth";
import {
  createBlog,
  createCertification,
  createProject,
  deleteBlog,
  deleteCertification,
  deleteProject,
  fetchAllBlogs,
  fetchAllCertifications,
  fetchAllProjects,
  fetchBlogById,
  fetchCertificationById,
  fetchProjectById,
  updateBlog,
  updateCertification,
  updateProject,
} from "./adminApi";

// The token comes from the in-memory sign-in session; lib/auth.test.ts covers that module.
vi.mock("../auth", () => ({ getAccessToken: vi.fn(() => Promise.resolve(null)) }));

const image = new File(["png"], "header.png", { type: "image/png" });

type Captured = { method: string; auth: string | null; form: FormData | null };

// Records the last write request so tests can assert on what the client sent.
function captureWrites(path: string, response: object | null = null) {
  const captured: Captured[] = [];
  const handler = async ({ request }: { request: Request }) => {
    const form = request.method === "DELETE" ? null : await request.formData();
    captured.push({ method: request.method, auth: request.headers.get("Authorization"), form });
    return response
      ? HttpResponse.json(response)
      : new HttpResponse(null, { status: request.method === "DELETE" ? 204 : 200 });
  };
  server.use(
    http.post(`${API}${path}`, handler),
    http.put(`${API}${path}/:id`, handler),
    http.delete(`${API}${path}/:id`, handler),
  );
  return captured;
}

describe("public reads", () => {
  it("fetches lists and single items", async () => {
    expect(await fetchAllBlogs()).toEqual([blog]);
    expect(await fetchBlogById(blog.id)).toEqual(blog);
    expect(await fetchAllProjects()).toEqual([project]);
    expect(await fetchProjectById(project.id)).toEqual(project);
    expect(await fetchAllCertifications()).toEqual([certification]);
    expect(await fetchCertificationById(certification.id)).toEqual(certification);
  });

  it("returns null for a missing item", async () => {
    expect(await fetchBlogById("missing")).toBeNull();
    expect(await fetchProjectById("missing")).toBeNull();
    expect(await fetchCertificationById("missing")).toBeNull();
  });

  it.each([
    ["blogs", fetchAllBlogs, () => fetchBlogById(blog.id)],
    ["projects", fetchAllProjects, () => fetchProjectById(project.id)],
    ["certifications", fetchAllCertifications, () => fetchCertificationById(certification.id)],
  ] as const)("handles %s errors, invalid data and network failures", async (path, list, one) => {
    vi.spyOn(console, "error").mockImplementation(() => {});

    server.use(http.get(`${API}/api/${path}*`, () => new HttpResponse(null, { status: 500 })));
    expect(await list()).toEqual([]);

    server.use(http.get(`${API}/api/${path}*`, () => HttpResponse.json({ id: "bad" })));
    expect(await list()).toEqual([]);
    expect(await one()).toBeNull();

    server.use(http.get(`${API}/api/${path}*`, () => HttpResponse.error()));
    expect(await list()).toEqual([]);
    expect(await one()).toBeNull();
  });
});

describe("writes without a token", () => {
  beforeEach(() => {
    vi.mocked(getAccessToken).mockResolvedValue(null);
  });

  it("throw before calling the API", async () => {
    await expect(deleteBlog(blog.id)).rejects.toThrow("Not authenticated");
    await expect(deleteProject(project.id)).rejects.toThrow("Not authenticated");
    await expect(deleteCertification(certification.id)).rejects.toThrow("Not authenticated");
  });
});

describe("writes with a token", () => {
  beforeEach(() => {
    vi.mocked(getAccessToken).mockResolvedValue("token-123");
  });

  it("create, update and delete blogs as multipart with a bearer token", async () => {
    const calls = captureWrites("/api/blogs", blog);
    await expect(
      createBlog({
        title: "t",
        shortDescription: "s",
        longDescription: "l",
        image,
        tags: ["a", "b"],
      }),
    ).resolves.toEqual(blog);
    await updateBlog(blog.id, {
      title: "t2",
      shortDescription: "s2",
      longDescription: "l2",
      tags: ["c"],
    });
    await updateBlog(blog.id, {
      title: "t3",
      shortDescription: "s3",
      longDescription: "l3",
      image,
      tags: [],
    });

    expect(calls.map((c) => c.method)).toEqual(["POST", "PUT", "PUT"]);
    expect(calls.every((c) => c.auth === "Bearer token-123")).toBe(true);
    expect(calls[0].form?.get("title")).toBe("t");
    expect(calls[1].form?.get("title")).toBe("t2");
    expect(calls[0].form?.get("tags")).toBe("a,b");
    expect((calls[0].form?.get("image") as Blob).type).toBe("image/png");
    expect(calls[1].form?.has("image")).toBe(false);
    expect(calls[2].form?.has("image")).toBe(true);

    server.use(http.delete(`${API}/api/blogs/:id`, () => new HttpResponse(null, { status: 204 })));
    await expect(deleteBlog(blog.id)).resolves.toBeUndefined();
  });

  it("create, update and delete projects", async () => {
    const calls = captureWrites("/api/projects", project);
    const data = {
      title: "t",
      shortDescription: "s",
      longDescription: "l",
      startDate: "2024-01-01",
      endDate: "2024-02-01",
      featured: true,
      skills: ["x", "y"],
    };
    await createProject(data);
    await createProject({ ...data, image });
    await updateProject(project.id, data);
    await updateProject(project.id, { ...data, image, featured: false });

    expect(calls.map((c) => c.method)).toEqual(["POST", "POST", "PUT", "PUT"]);
    expect(calls[0].form?.get("featured")).toBe("true");
    expect(calls[0].form?.get("skills")).toBe("x,y");
    expect(calls[0].form?.has("image")).toBe(false);
    expect(calls[1].form?.has("image")).toBe(true);
    expect(calls[3].form?.get("featured")).toBe("false");

    server.use(
      http.delete(`${API}/api/projects/:id`, () => new HttpResponse(null, { status: 204 })),
    );
    await expect(deleteProject(project.id)).resolves.toBeUndefined();
  });

  it("create, update and delete certifications with optional fields", async () => {
    const calls = captureWrites("/api/certifications", certification);
    const base = { title: "t", issuer: "i", issueDate: "2024-01-01", description: "d" };
    await createCertification({ ...base, image });
    await createCertification({
      ...base,
      image,
      expiryDate: "2026-01-01",
      credentialId: "id",
      credentialUrl: "https://x",
    });
    await updateCertification(certification.id, base);
    await updateCertification(certification.id, {
      ...base,
      image,
      expiryDate: "2026-01-01",
      credentialId: "id",
      credentialUrl: "https://x",
    });

    expect(calls[0].form?.has("expiryDate")).toBe(false);
    expect(calls[1].form?.get("credentialUrl")).toBe("https://x");
    expect(calls[2].form?.has("image")).toBe(false);
    expect(calls[3].form?.get("expiryDate")).toBe("2026-01-01");

    server.use(
      http.delete(`${API}/api/certifications/:id`, () => new HttpResponse(null, { status: 204 })),
    );
    await expect(deleteCertification(certification.id)).resolves.toBeUndefined();
  });

  const emptyProject = {
    title: "",
    shortDescription: "",
    longDescription: "",
    startDate: "",
    endDate: "",
    featured: false,
    skills: [],
  };
  const emptyCertification = { title: "", issuer: "", issueDate: "", description: "" };

  it.each([
    {
      path: "blogs",
      noun: "blog",
      create: () =>
        createBlog({ title: "", shortDescription: "", longDescription: "", image, tags: [] }),
      update: () =>
        updateBlog("x", { title: "", shortDescription: "", longDescription: "", tags: [] }),
      remove: () => deleteBlog("x"),
    },
    {
      path: "projects",
      noun: "project",
      create: () => createProject(emptyProject),
      update: () => updateProject("x", emptyProject),
      remove: () => deleteProject("x"),
    },
    {
      path: "certifications",
      noun: "certification",
      create: () => createCertification({ ...emptyCertification, image }),
      update: () => updateCertification("x", emptyCertification),
      remove: () => deleteCertification("x"),
    },
  ])(
    "surfaces the API error text or a default message for $path",
    async ({ path, noun, create, update, remove }) => {
      const fail = (body: string) => () => new HttpResponse(body, { status: 400 });
      server.use(
        http.post(`${API}/api/${path}`, fail("bad input")),
        http.put(`${API}/api/${path}/:id`, fail("")),
        http.delete(`${API}/api/${path}/:id`, fail("")),
      );

      await expect(create()).rejects.toThrow("bad input");
      await expect(update()).rejects.toThrow(`Failed to update ${noun}`);
      await expect(remove()).rejects.toThrow(`Failed to delete ${noun}`);
    },
  );
});
