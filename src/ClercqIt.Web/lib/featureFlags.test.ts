import { http, HttpResponse, type JsonBodyType } from "msw";
import { describe, expect, it, vi } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { getAccessToken } from "./auth";
import { fetchFeatureFlags, isEnabled } from "./featureFlags";

vi.mock("./auth", () => ({ getAccessToken: vi.fn(() => Promise.resolve("token")) }));

const flags = (body: JsonBodyType, status = 200) =>
  server.use(http.get(`${API}/api/features`, () => HttpResponse.json(body, { status })));

describe("fetchFeatureFlags", () => {
  it("returns the flags from the API, asked with the admin's token", async () => {
    let auth: string | null = null;
    server.use(
      http.get(`${API}/api/features`, ({ request }) => {
        auth = request.headers.get("Authorization");
        return HttpResponse.json({ NewBlogEditor: true, Search: false });
      }),
    );

    await expect(fetchFeatureFlags()).resolves.toEqual({ NewBlogEditor: true, Search: false });
    expect(auth).toBe("Bearer token");
  });

  it("returns no flags when nobody is signed in", async () => {
    vi.mocked(getAccessToken).mockResolvedValueOnce(null);
    flags({ NewBlogEditor: true });

    await expect(fetchFeatureFlags()).resolves.toEqual({});
  });

  it.each([
    { name: "an error status", body: {}, status: 500 },
    { name: "an invalid body", body: { NewBlogEditor: "yes" }, status: 200 },
  ])("returns no flags on $name", async ({ body, status }) => {
    flags(body, status);

    await expect(fetchFeatureFlags()).resolves.toEqual({});
  });

  it("returns no flags when the API is unreachable", async () => {
    server.use(http.get(`${API}/api/features`, () => HttpResponse.error()));

    await expect(fetchFeatureFlags()).resolves.toEqual({});
  });
});

describe("isEnabled", () => {
  it.each([
    { flag: "NewBlogEditor", expected: true },
    { flag: "newblogeditor", expected: true },
    { flag: "Search", expected: false },
    { flag: "Unknown", expected: false },
  ])("is $expected for $flag", async ({ flag, expected }) => {
    flags({ NewBlogEditor: true, Search: false });

    await expect(isEnabled(flag)).resolves.toBe(expected);
  });

  it("is off by default", async () => {
    await expect(isEnabled("NewBlogEditor")).resolves.toBe(false);
  });
});
