import { InMemoryWebStorage, UserManager, WebStorageStateStore, type User } from "oidc-client-ts";
import { z } from "zod";
import { getApiUrl } from "./api/baseApi";

// Admin sign-in with the authorization code flow + PKCE against Keycloak
// (Cloud IAM in production), issue #257. Keycloak shows its own login page, so
// MFA, brute-force protection and required actions are handled there. Tokens
// live in memory only; after a reload the next sign-in is silent while the
// Keycloak session lasts. See docs/ciam.md.
//
// The server only serves the dashboard to a live admin session (issue #454): after
// sign-in and each token renewal the access token goes to POST /admin/session, which
// keeps it in an httpOnly cookie for /admin/dashboard (lib/adminSession.ts).

const AuthConfigSchema = z.object({ authority: z.string().url(), clientId: z.string().min(1) });

export type AuthConfig = z.infer<typeof AuthConfigSchema>;

const DEFAULT_RETURN_TO = "/admin/dashboard";

let manager: Promise<UserManager> | null = null;
// The admin session sync for the current access token (one entry at most).
const sessions = new Map<string, Promise<void>>();

/** Thrown when the signed-in account has none of the admin roles. */
export class AdminAccessError extends Error {}

async function postAdminSession(token: string): Promise<void> {
  const response = await fetch("/admin/session", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (response.status === 403) {
    throw new AdminAccessError("Your account has no access to the admin.");
  }
  if (!response.ok) {
    throw new Error(`The admin session could not start (${response.status})`);
  }
}

/** Hands a new access token to the server once, however often it is reported. */
function syncAdminSession(token: string): Promise<void> {
  let sync = sessions.get(token);
  if (!sync) {
    sync = postAdminSession(token);
    sessions.clear();
    sessions.set(token, sync);
  }
  return sync;
}

/** The issuer and public client id, from `GET /api/auth/config`. */
export async function fetchAuthConfig(): Promise<AuthConfig> {
  const response = await fetch(`${getApiUrl()}/api/auth/config`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`Sign-in is not available (${response.status})`);
  }
  return AuthConfigSchema.parse(await response.json());
}

function getManager(): Promise<UserManager> {
  manager ??= fetchAuthConfig().then(
    (config) => {
      const userManager = new UserManager({
        authority: config.authority,
        client_id: config.clientId,
        redirect_uri: `${window.location.origin}/admin/callback`,
        post_logout_redirect_uri: `${window.location.origin}/admin`,
        response_type: "code",
        // Keycloak adds the client's default scopes (roles, profile, email) on its own.
        scope: "openid",
        // Tokens never touch localStorage. Only the PKCE verifier and state have
        // to survive the redirect to Keycloak, so they go to sessionStorage.
        userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
        stateStore: new WebStorageStateStore({ store: window.sessionStorage }),
        automaticSilentRenew: true,
      });
      // A renewed token renews the server's admin session too.
      userManager.events.addUserLoaded((user) => {
        syncAdminSession(user.access_token).catch((error: unknown) => {
          console.error("Admin session renewal failed:", error);
        });
      });
      return userManager;
    },
    (error: unknown) => {
      manager = null;
      throw error;
    },
  );
  return manager;
}

/** Only same-site admin paths are allowed as a return address. */
export function safeReturnTo(value: unknown): string {
  return typeof value === "string" && value.startsWith("/admin") && !value.startsWith("//")
    ? value
    : DEFAULT_RETURN_TO;
}

/** Redirects to the Keycloak login page; comes back on /admin/callback. */
export async function signIn(returnTo: string = DEFAULT_RETURN_TO): Promise<void> {
  await (await getManager()).signinRedirect({ state: safeReturnTo(returnTo) });
}

/** Finishes the sign-in on /admin/callback and returns where to go next. */
export async function completeSignIn(): Promise<string> {
  const user = await (await getManager()).signinRedirectCallback();
  await syncAdminSession(user.access_token);
  return safeReturnTo(user.state);
}

/** The signed-in user, or null when there is none or the token has expired. */
export async function getUser(): Promise<User | null> {
  const user = await (await getManager()).getUser();
  return user && !user.expired ? user : null;
}

/** The access token for API calls, or null when not signed in. */
export async function getAccessToken(): Promise<string | null> {
  return (await getUser())?.access_token ?? null;
}

/** Forgets the tokens and the admin session, ends the Keycloak session; comes back on /admin. */
export async function signOut(): Promise<void> {
  sessions.clear();
  await fetch("/admin/session", { method: "DELETE" }).catch(() => undefined);
  const userManager = await getManager();
  const user = await userManager.getUser();
  await userManager.removeUser();
  await userManager.signoutRedirect({ id_token_hint: user?.id_token });
}

const TokenClaimsSchema = z.object({
  preferred_username: z.string().optional(),
  roles: z.array(z.string()).optional(),
});

/** Username and realm roles from the access token (the API checks the same `roles` claim). */
export function claimsOf(user: User): { username: string; roles: string[] } {
  try {
    const payload = user.access_token.split(".")[1] ?? "";
    const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
    const claims = TokenClaimsSchema.parse(JSON.parse(json));
    return {
      username: claims.preferred_username ?? user.profile.preferred_username ?? "",
      roles: claims.roles ?? [],
    };
  } catch {
    return { username: user.profile.preferred_username ?? "", roles: [] };
  }
}

/** Test hook: forget the cached UserManager. */
export function resetAuth(): void {
  manager = null;
  sessions.clear();
}
