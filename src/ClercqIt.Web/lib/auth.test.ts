import { http, HttpResponse } from "msw";
import type { User } from "oidc-client-ts";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import {
  AdminAccessError,
  claimsOf,
  completeSignIn,
  fetchAuthConfig,
  getAccessToken,
  getUser,
  resetAuth,
  signIn,
  signOut,
} from "./auth";

const userManager = vi.hoisted(() => ({
  signinRedirect: vi.fn(),
  signinRedirectCallback: vi.fn(),
  getUser: vi.fn(),
  removeUser: vi.fn(),
  signoutRedirect: vi.fn(),
  events: { addUserLoaded: vi.fn() },
}));
const UserManager = vi.hoisted(() =>
  vi.fn(function () {
    return userManager;
  }),
);

vi.mock("oidc-client-ts", async (importOriginal) => ({
  ...(await importOriginal<typeof import("oidc-client-ts")>()),
  UserManager,
}));

const toBase64Url = (value: object) =>
  btoa(JSON.stringify(value)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
const token = (claims: object) => `header.${toBase64Url(claims)}.signature`;
const user = (overrides: Partial<User> = {}) =>
  ({
    access_token: token({ preferred_username: "admin", roles: ["Admin.View"] }),
    id_token: "id-token",
    expired: false,
    profile: { preferred_username: "profile-name" },
    ...overrides,
  }) as unknown as User;

beforeEach(() => resetAuth());
afterEach(() => vi.clearAllMocks());

describe("fetchAuthConfig", () => {
  it("returns the issuer and client id from the API", async () => {
    await expect(fetchAuthConfig()).resolves.toEqual({
      authority: "http://localhost:8080/realms/clercqit",
      clientId: "clercqit-web",
    });
  });

  it("throws when the API fails", async () => {
    server.use(http.get(`${API}/api/auth/config`, () => new HttpResponse(null, { status: 503 })));

    await expect(fetchAuthConfig()).rejects.toThrow("Sign-in is not available (503)");
  });

  it("throws on an invalid config", async () => {
    server.use(http.get(`${API}/api/auth/config`, () => HttpResponse.json({ authority: "x" })));

    await expect(fetchAuthConfig()).rejects.toThrow();
  });
});

describe("signIn", () => {
  it("starts the code flow with PKCE against the configured issuer, tokens in memory", async () => {
    await signIn("/admin/dashboard?tab=blogs");

    expect(UserManager).toHaveBeenCalledOnce();
    const [settings] = UserManager.mock.calls[0] as unknown as [Record<string, unknown>];
    expect(settings).toMatchObject({
      authority: "http://localhost:8080/realms/clercqit",
      client_id: "clercqit-web",
      redirect_uri: "http://localhost:3000/admin/callback",
      post_logout_redirect_uri: "http://localhost:3000/admin",
      response_type: "code",
    });
    expect(userManager.signinRedirect).toHaveBeenCalledWith({
      state: "/admin/dashboard?tab=blogs",
    });
  });

  it.each(["https://evil.example", "//evil.example/admin", "/blogs"])(
    "ignores the unsafe return address %s",
    async (returnTo) => {
      await signIn(returnTo);

      expect(userManager.signinRedirect).toHaveBeenCalledWith({ state: "/admin/dashboard" });
    },
  );

  it("creates the UserManager once and retries after a failed config load", async () => {
    server.use(
      http.get(`${API}/api/auth/config`, () => new HttpResponse(null, { status: 500 }), {
        once: true,
      }),
    );

    await expect(signIn()).rejects.toThrow();
    await signIn();
    await signIn();

    expect(UserManager).toHaveBeenCalledOnce();
    expect(userManager.signinRedirect).toHaveBeenCalledTimes(2);
  });
});

const SESSION = "http://localhost:3000/admin/session";

function recordSessionCalls(status = 204): { method: string; auth: string | null }[] {
  const calls: { method: string; auth: string | null }[] = [];
  const record = ({ request }: { request: Request }) => {
    calls.push({ method: request.method, auth: request.headers.get("Authorization") });
    return new HttpResponse(null, { status });
  };
  server.use(http.post(SESSION, record), http.delete(SESSION, record));
  return calls;
}

describe("completeSignIn", () => {
  it("hands the access token to the server as the admin session", async () => {
    const calls = recordSessionCalls();
    const signedIn = user({ state: "/admin/dashboard" });
    userManager.signinRedirectCallback.mockResolvedValue(signedIn);

    await completeSignIn();

    expect(calls).toEqual([{ method: "POST", auth: `Bearer ${signedIn.access_token}` }]);
  });

  it("refuses an account without an admin role", async () => {
    recordSessionCalls(403);
    userManager.signinRedirectCallback.mockResolvedValue(user());

    await expect(completeSignIn()).rejects.toBeInstanceOf(AdminAccessError);
  });

  it("fails when the server cannot start the session", async () => {
    recordSessionCalls(503);
    userManager.signinRedirectCallback.mockResolvedValue(user());

    await expect(completeSignIn()).rejects.toThrow("The admin session could not start (503)");
  });

  it("returns the stored return address", async () => {
    userManager.signinRedirectCallback.mockResolvedValue(user({ state: "/admin/dashboard?x=1" }));

    await expect(completeSignIn()).resolves.toBe("/admin/dashboard?x=1");
  });

  it("falls back to the dashboard for anything else", async () => {
    userManager.signinRedirectCallback.mockResolvedValue(user({ state: undefined }));

    await expect(completeSignIn()).resolves.toBe("/admin/dashboard");
  });
});

describe("getUser and getAccessToken", () => {
  it("return the signed-in user and token", async () => {
    const signedIn = user();
    userManager.getUser.mockResolvedValue(signedIn);

    await expect(getUser()).resolves.toBe(signedIn);
    await expect(getAccessToken()).resolves.toBe(signedIn.access_token);
  });

  it.each([
    { name: "no user", value: null },
    { name: "an expired token", value: user({ expired: true }) },
  ])("return null for $name", async ({ value }) => {
    userManager.getUser.mockResolvedValue(value);

    await expect(getUser()).resolves.toBeNull();
    await expect(getAccessToken()).resolves.toBeNull();
  });
});

describe("token renewal", () => {
  it("renews the admin session once per new token", async () => {
    const calls = recordSessionCalls();
    await signIn();
    const [onUserLoaded] = userManager.events.addUserLoaded.mock.calls[0] as [(u: User) => void];
    const renewed = user({ access_token: token({ roles: ["Admin.View"], n: 2 }) });

    onUserLoaded(renewed);
    onUserLoaded(renewed);

    await vi.waitFor(() =>
      expect(calls).toEqual([{ method: "POST", auth: `Bearer ${renewed.access_token}` }]),
    );
  });

  it("logs a failed renewal instead of throwing", async () => {
    recordSessionCalls(503);
    const log = vi.spyOn(console, "error").mockImplementation(() => undefined);
    await signIn();
    const [onUserLoaded] = userManager.events.addUserLoaded.mock.calls[0] as [(u: User) => void];

    onUserLoaded(user());

    await vi.waitFor(() =>
      expect(log).toHaveBeenCalledWith("Admin session renewal failed:", expect.any(Error)),
    );
  });
});

describe("signOut", () => {
  it("forgets the tokens and the admin session, and ends the Keycloak session", async () => {
    const calls = recordSessionCalls();
    userManager.getUser.mockResolvedValue(user());

    await signOut();

    expect(calls).toEqual([{ method: "DELETE", auth: null }]);

    expect(userManager.removeUser).toHaveBeenCalledOnce();
    expect(userManager.signoutRedirect).toHaveBeenCalledWith({ id_token_hint: "id-token" });
  });
});

describe("claimsOf", () => {
  it("reads the username and roles from the access token", () => {
    expect(claimsOf(user())).toEqual({ username: "admin", roles: ["Admin.View"] });
  });

  it("falls back to the profile when the token has no username or roles", () => {
    expect(claimsOf(user({ access_token: token({}) }))).toEqual({
      username: "profile-name",
      roles: [],
    });
  });

  it("returns no roles for a malformed token", () => {
    expect(claimsOf(user({ access_token: "not-a-jwt" }))).toEqual({
      username: "profile-name",
      roles: [],
    });
  });
});
