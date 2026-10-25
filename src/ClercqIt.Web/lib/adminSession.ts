import { NextResponse, type NextRequest } from "next/server";
import { getApiUrl } from "./api/baseApi";

// The server-side gate in front of the admin dashboard (issue #454, docs/application-security.md).
// After sign-in the browser hands its access token to POST /admin/session, which keeps it in
// an httpOnly cookie scoped to /admin/dashboard. proxy.ts checks that cookie with the API on
// every dashboard request, so a visitor without an admin role never receives the dashboard's
// HTML or the names of its JavaScript chunks. The API still checks every call on its own.

export const ADMIN_SESSION_COOKIE = "clercqit_admin_session";
export const ADMIN_SESSION_PATH = "/admin/dashboard";
// Cap on the cookie's lifetime; the token's own expiry is usually much shorter.
const MAX_SESSION_SECONDS = 60 * 60;

export type AdminAccess = "granted" | "signed-out" | "forbidden" | "unavailable";

export const NO_ACCESS_MESSAGE = "Your account has no access to the admin.";
export const UNAVAILABLE_MESSAGE = "Sign-in is not available right now";

/** Asks the API whether `token` belongs to a user with an admin role (GET /api/auth/session). */
export async function checkAdminAccess(token: string | undefined): Promise<AdminAccess> {
  if (!token) return "signed-out";
  try {
    const response = await fetch(`${getApiUrl()}/api/auth/session`, {
      headers: { Authorization: `Bearer ${token}` },
      cache: "no-store",
    });
    if (response.ok) return "granted";
    if (response.status === 401) return "signed-out";
    if (response.status === 403) return "forbidden";
    return "unavailable";
  } catch {
    return "unavailable";
  }
}

/** Seconds until the token expires (its `exp` claim), capped; 0 for an expired or unreadable token. */
export function sessionMaxAge(token: string, now: number = Date.now()): number {
  try {
    const payload = token.split(".")[1] ?? "";
    const claims = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/"))) as {
      exp?: unknown;
    };
    if (typeof claims.exp !== "number") return 0;
    return Math.max(0, Math.min(MAX_SESSION_SECONDS, Math.floor(claims.exp - now / 1000)));
  } catch {
    return 0;
  }
}

function sessionCookie(value: string, maxAge: number) {
  return {
    name: ADMIN_SESSION_COOKIE,
    value,
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "strict" as const,
    path: ADMIN_SESSION_PATH,
    maxAge,
  };
}

function bearerToken(request: NextRequest): string | undefined {
  const header = request.headers.get("Authorization") ?? "";
  return header.startsWith("Bearer ")
    ? header.slice("Bearer ".length).trim() || undefined
    : undefined;
}

function refusalStatus(access: Exclude<AdminAccess, "granted">): number {
  if (access === "forbidden") return 403;
  if (access === "unavailable") return 503;
  return 401;
}

/** POST /admin/session: stores the caller's access token as the admin session, if the API accepts it. */
export async function startAdminSession(request: NextRequest): Promise<NextResponse> {
  const token = bearerToken(request);
  if (!token) return new NextResponse(null, { status: refusalStatus("signed-out") });
  const access = await checkAdminAccess(token);
  if (access !== "granted") return new NextResponse(null, { status: refusalStatus(access) });
  const response = new NextResponse(null, { status: 204 });
  response.cookies.set(sessionCookie(token, sessionMaxAge(token)));
  return response;
}

/** DELETE /admin/session: forgets the admin session (sign-out). */
export function endAdminSession(): NextResponse {
  const response = new NextResponse(null, { status: 204 });
  response.cookies.set(sessionCookie("", 0));
  return response;
}

/**
 * proxy.ts: lets a dashboard request through only for a live admin session, else sends the
 * visitor to the /admin sign-in page without serving any of the dashboard.
 */
export async function adminGate(request: NextRequest): Promise<NextResponse> {
  const access = await checkAdminAccess(request.cookies.get(ADMIN_SESSION_COOKIE)?.value);
  if (access === "granted") {
    const response = NextResponse.next();
    response.headers.set("Cache-Control", "private, no-store");
    return response;
  }

  const signIn = new URL("/admin", request.nextUrl);
  if (access === "forbidden") signIn.searchParams.set("error", NO_ACCESS_MESSAGE);
  if (access === "unavailable") signIn.searchParams.set("error", UNAVAILABLE_MESSAGE);
  const response = NextResponse.redirect(signIn, 307);
  response.headers.set("Cache-Control", "private, no-store");
  // A rejected token is useless; an API outage says nothing about the token.
  if (access !== "unavailable" && request.cookies.has(ADMIN_SESSION_COOKIE)) {
    response.cookies.set(sessionCookie("", 0));
  }
  return response;
}
