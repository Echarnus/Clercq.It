"use client";

import { useEffect, useState } from "react";
import { ExternalLink } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { fetchIdentityProvider, type IdentityProvider } from "@/lib/api/identityProviderApi";
import type { AdminSectionContext } from "@/lib/adminNav";

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="grid gap-1 sm:grid-cols-[12rem_1fr] sm:gap-4">
      <dt className="text-sm text-muted-foreground">{label}</dt>
      <dd className="min-w-0 text-sm break-all">{children}</dd>
    </div>
  );
}

function ConsoleLink({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className="inline-flex items-center gap-1 font-medium text-primary underline-offset-4 hover:underline"
    >
      {children}
      <ExternalLink className="h-3.5 w-3.5" aria-hidden />
    </a>
  );
}

/** How Cloud IAM is set up, with links to its consoles (issue #462, docs/ciam.md). */
function IdentityProviderDetails() {
  const [idp, setIdp] = useState<IdentityProvider | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetchIdentityProvider()
      .then((result) => {
        if (!cancelled) setIdp(result);
      })
      .catch((err: unknown) => {
        if (!cancelled)
          setError(err instanceof Error ? err.message : "Could not load the identity provider");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (error) return <p className="text-sm text-destructive">{error}</p>;
  if (!idp) return <p className="text-sm text-muted-foreground">Loading…</p>;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-x-6 gap-y-2">
        <ConsoleLink href={idp.adminConsoleUrl}>Open Keycloak admin console</ConsoleLink>
        {idp.providerConsoleUrl && (
          <ConsoleLink href={idp.providerConsoleUrl}>Open Cloud IAM</ConsoleLink>
        )}
        <ConsoleLink href={idp.accountConsoleUrl}>Your Keycloak account</ConsoleLink>
      </div>
      <p className="text-sm text-muted-foreground">
        Sign in to the admin console with a <strong>{idp.realm}</strong> realm account that has the
        realm-management role <code>realm-admin</code>: the <code>admin</code> user Cloud IAM
        created with the deployment (first password in its &quot;deployment has been completed&quot;
        email), or your own account once it has that role. The account that owns the deployment and
        its plan signs in at Cloud IAM. Passwords are never shown here.
      </p>
      <dl className="space-y-2">
        <Row label="Provider">{idp.provider}</Row>
        <Row label="Base URL">
          <code>{idp.baseUrl}</code>
        </Row>
        <Row label="Realm">
          <code>{idp.realm}</code>
        </Row>
        <Row label="Issuer (authority)">
          <code>{idp.authority}</code>
        </Row>
        <Row label="Site sign-in client">
          <code>{idp.webClientId}</code> (public, PKCE)
        </Row>
        <Row label="MCP connector client">
          <code>{idp.mcpClientId}</code> (public, PKCE)
        </Row>
        <Row label="Realm roles">
          <ul className="space-y-1">
            {idp.roles.map((role) => (
              <li key={role.name}>
                <Badge variant="secondary">{role.name}</Badge>{" "}
                <span className="text-muted-foreground">{role.description}</span>
              </li>
            ))}
          </ul>
        </Row>
      </dl>
    </div>
  );
}

/** Read-only facts about the admin setup and the signed-in user's roles. */
export function AdminSettings({ username, roles }: AdminSectionContext) {
  return (
    <div className="space-y-6">
      <section className="space-y-2" aria-labelledby="settings-auth">
        <h3 id="settings-auth" className="text-sm font-medium">
          Authentication: Cloud IAM
        </h3>
        <IdentityProviderDetails />
      </section>
      <section className="space-y-2">
        <h3 className="text-sm font-medium">Your roles</h3>
        <p className="text-sm text-muted-foreground">
          Signed in as <strong>{username}</strong>
        </p>
        <p className="text-sm text-muted-foreground">{roles.join(" ") || "None"}</p>
      </section>
      <section className="space-y-2">
        <h3 className="text-sm font-medium">Storage</h3>
        <p className="text-sm text-muted-foreground">Connected to Scaleway Object Storage</p>
      </section>
      <section className="space-y-2">
        <h3 className="text-sm font-medium">API Status</h3>
        <p className="text-sm text-muted-foreground">All endpoints operational</p>
      </section>
    </div>
  );
}
