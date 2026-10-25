import { z } from "zod";
import { authFetch } from "./adminApi";

// The identity provider setup for the admin Settings page (issue #462, docs/ciam.md).
// Only URLs, the realm, client ids and role names: the API never sends secrets here.

export const IdentityProviderSchema = z.object({
  provider: z.string(),
  baseUrl: z.url(),
  realm: z.string(),
  authority: z.url(),
  adminConsoleUrl: z.url(),
  accountConsoleUrl: z.url(),
  providerConsoleUrl: z.url().nullable(),
  webClientId: z.string(),
  mcpClientId: z.string(),
  roles: z.array(z.object({ name: z.string(), description: z.string() })),
});

export type IdentityProvider = z.infer<typeof IdentityProviderSchema>;

/** How Cloud IAM is set up (needs the Admin.View role). */
export async function fetchIdentityProvider(): Promise<IdentityProvider> {
  const response = await authFetch("/api/auth/identity-provider");
  if (!response.ok) {
    throw new Error(`Identity provider request failed (${response.status})`);
  }
  return IdentityProviderSchema.parse(await response.json());
}
