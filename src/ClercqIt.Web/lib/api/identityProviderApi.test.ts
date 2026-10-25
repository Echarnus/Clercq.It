// @vitest-environment node
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { API } from "@/test/msw/fixtures";
import { server } from "@/test/msw/server";
import { fetchIdentityProvider } from "./identityProviderApi";

vi.mock("../auth", () => ({ getAccessToken: vi.fn(() => Promise.resolve("token")) }));

const setup = {
  provider: "Cloud IAM (hosted Keycloak)",
  baseUrl: "https://lemur-16.cloud-iam.com/auth",
  realm: "clercqit",
  authority: "https://lemur-16.cloud-iam.com/auth/realms/clercqit",
  adminConsoleUrl: "https://lemur-16.cloud-iam.com/auth/admin/clercqit/console/",
  accountConsoleUrl: "https://lemur-16.cloud-iam.com/auth/realms/clercqit/account/",
  providerConsoleUrl: "https://www.cloud-iam.com",
  webClientId: "clercqit-web",
  mcpClientId: "clercqit-mcp",
  roles: [{ name: "Admin.View", description: "Open the admin dashboard" }],
};

describe("identity provider API client", () => {
  it("reads the setup with the access token", async () => {
    let auth: string | null = null;
    server.use(
      http.get(`${API}/api/auth/identity-provider`, ({ request }) => {
        auth = request.headers.get("Authorization");
        return HttpResponse.json(setup);
      }),
    );

    await expect(fetchIdentityProvider()).resolves.toEqual(setup);
    expect(auth).toBe("Bearer token");
  });

  it("accepts a local Keycloak without a Cloud IAM link", async () => {
    server.use(
      http.get(`${API}/api/auth/identity-provider`, () =>
        HttpResponse.json({ ...setup, providerConsoleUrl: null }),
      ),
    );

    await expect(fetchIdentityProvider()).resolves.toMatchObject({ providerConsoleUrl: null });
  });

  it("throws on an error status", async () => {
    server.use(
      http.get(`${API}/api/auth/identity-provider`, () => new HttpResponse(null, { status: 403 })),
    );

    await expect(fetchIdentityProvider()).rejects.toThrow("Identity provider request failed (403)");
  });
});
