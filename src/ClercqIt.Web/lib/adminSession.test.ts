// @vitest-environment node
import { http, HttpResponse } from "msw";
import { NextRequest } from "next/server";
import { describe, expect, it } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import {
  ADMIN_SESSION_COOKIE,
  adminGate,
  checkAdminAccess,
  endAdminSession,
  sessionMaxAge,
  startAdminSession,
} from "./adminSession";

const toBase64Url = (value: object) =>
  btoa(JSON.stringify(value)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
const NOW = Date.UTC(2026, 9, 25, 12, 0, 0);
const jwt = (claims: object) => `header.${toBase64Url(claims)}.signature`;
const liveToken = () => jwt({ exp: Math.floor(Date.now() / 1000) + 300 });

/** The API's GET /api/auth/session answers with `status`; returns the tokens it saw. */
function apiSession(status: number | "error"): (string | null)[] {
  const seen: (string | null)[] = [];
  server.use(
    http.get(`${API}/api/auth/session`, ({ request }) => {
      seen.push(request.headers.get("Authorization"));
      return status === "error" ? HttpResponse.error() : new HttpResponse(null, { status });
    }),
  );
  return seen;
}

const dashboard = (cookie?: string) =>
  new NextRequest("http://localhost:3000/admin/dashboard/blogs", {
    headers: cookie ? { cookie: `${ADMIN_SESSION_COOKIE}=${cookie}` } : {},
  });

describe("checkAdminAccess", () => {
  it("is signed-out without a token, without asking the API", async () => {
    const seen = apiSession(204);

    await expect(checkAdminAccess(undefined)).resolves.toBe("signed-out");
    expect(seen).toEqual([]);
  });

  it.each([
    { status: 204, access: "granted" },
    { status: 401, access: "signed-out" },
    { status: 403, access: "forbidden" },
    { status: 500, access: "unavailable" },
    { status: "error" as const, access: "unavailable" },
  ])("is $access when the API answers $status", async ({ status, access }) => {
    const seen = apiSession(status);

    await expect(checkAdminAccess("abc")).resolves.toBe(access);
    expect(seen).toEqual(["Bearer abc"]);
  });
});

describe("sessionMaxAge", () => {
  it("lasts until the token expires", () => {
    expect(sessionMaxAge(jwt({ exp: NOW / 1000 + 300 }), NOW)).toBe(300);
  });

  it("is capped at an hour", () => {
    expect(sessionMaxAge(jwt({ exp: NOW / 1000 + 86_400 }), NOW)).toBe(3600);
  });

  it.each([
    { name: "an expired token", token: jwt({ exp: NOW / 1000 - 10 }) },
    { name: "a token without exp", token: jwt({}) },
    { name: "a malformed token", token: "not-a-jwt" },
  ])("is 0 for $name", ({ token }) => {
    expect(sessionMaxAge(token, NOW)).toBe(0);
  });
});

describe("startAdminSession", () => {
  const post = (authorization?: string) =>
    new NextRequest("http://localhost:3000/admin/session", {
      method: "POST",
      headers: authorization ? { Authorization: authorization } : {},
    });

  it("stores an accepted token in an httpOnly cookie for the dashboard only", async () => {
    apiSession(204);
    const token = liveToken();

    const response = await startAdminSession(post(`Bearer ${token}`));

    expect(response.status).toBe(204);
    const cookie = response.headers.get("set-cookie") ?? "";
    expect(cookie).toContain(`${ADMIN_SESSION_COOKIE}=${token}`);
    expect(cookie).toContain("Path=/admin/dashboard");
    expect(cookie).toContain("HttpOnly");
    expect(cookie.toLowerCase()).toContain("samesite=strict");
    expect(cookie).toMatch(/Max-Age=(29\d|300)/);
  });

  it.each([undefined, "Basic abc", "Bearer "])(
    "is 401 without a bearer token (%s)",
    async (auth) => {
      const seen = apiSession(204);

      const response = await startAdminSession(post(auth));

      expect(response.status).toBe(401);
      expect(response.headers.get("set-cookie")).toBeNull();
      expect(seen).toEqual([]);
    },
  );

  it.each([
    { status: 401, expected: 401 },
    { status: 403, expected: 403 },
    { status: 500, expected: 503 },
  ])("sets no cookie when the API answers $status", async ({ status, expected }) => {
    apiSession(status);

    const response = await startAdminSession(post("Bearer abc"));

    expect(response.status).toBe(expected);
    expect(response.headers.get("set-cookie")).toBeNull();
  });
});

describe("endAdminSession", () => {
  it("clears the cookie", () => {
    const cookie = endAdminSession().headers.get("set-cookie") ?? "";

    expect(cookie).toContain(`${ADMIN_SESSION_COOKIE}=;`);
    expect(cookie).toContain("Path=/admin/dashboard");
    expect(cookie).toContain("Max-Age=0");
  });
});

describe("adminGate", () => {
  it("lets a live admin session through, uncached", async () => {
    const seen = apiSession(204);

    const response = await adminGate(dashboard("abc"));

    expect(response.headers.get("x-middleware-next")).toBe("1");
    expect(response.headers.get("cache-control")).toBe("private, no-store");
    expect(seen).toEqual(["Bearer abc"]);
  });

  it("sends a visitor without a session to the sign-in page, serving nothing", async () => {
    const seen = apiSession(204);

    const response = await adminGate(dashboard());

    expect(response.status).toBe(307);
    expect(response.headers.get("location")).toBe("http://localhost:3000/admin");
    expect(response.headers.get("set-cookie")).toBeNull();
    expect(seen).toEqual([]);
    await expect(response.text()).resolves.toBe("");
  });

  it("drops an expired session", async () => {
    apiSession(401);

    const response = await adminGate(dashboard("old"));

    expect(response.headers.get("location")).toBe("http://localhost:3000/admin");
    expect(response.headers.get("set-cookie")).toContain("Max-Age=0");
  });

  it("tells an account without an admin role it has no access", async () => {
    apiSession(403);

    const response = await adminGate(dashboard("abc"));

    expect(response.status).toBe(307);
    expect(new URL(response.headers.get("location") ?? "").searchParams.get("error")).toBe(
      "Your account has no access to the admin.",
    );
    expect(response.headers.get("set-cookie")).toContain("Max-Age=0");
  });

  it("keeps the session when the API is down", async () => {
    apiSession(503);

    const response = await adminGate(dashboard("abc"));

    expect(new URL(response.headers.get("location") ?? "").searchParams.get("error")).toBe(
      "Sign-in is not available right now",
    );
    expect(response.headers.get("set-cookie")).toBeNull();
  });
});
