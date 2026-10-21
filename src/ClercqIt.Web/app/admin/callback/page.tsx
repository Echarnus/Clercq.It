"use client";

import { useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { completeSignIn } from "@/lib/auth";

// Keycloak redirects here after sign-in with the authorization code; exchange it
// (with the PKCE verifier) for tokens and continue to the admin page asked for.
export default function AdminCallbackPage() {
  const router = useRouter();
  // The code can be exchanged once; React's development double effect must not repeat it.
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    completeSignIn()
      .then((returnTo) => router.replace(returnTo))
      .catch((error: unknown) => {
        console.error("Sign-in callback failed:", error);
        router.replace(`/admin?error=${encodeURIComponent("Sign-in failed. Please try again.")}`);
      });
  }, [router]);

  return (
    <div className="flex min-h-screen items-center justify-center">
      <p className="text-sm text-muted-foreground">Signing you in...</p>
    </div>
  );
}
