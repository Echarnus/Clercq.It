import type { NextRequest } from "next/server";
import { adminGate } from "@/lib/adminSession";

// Only signed-in admins get the dashboard, not even its page shell (issue #454,
// docs/application-security.md#admin-pages). The sign-in and callback pages under
// /admin stay public: they are how an admin gets a session.
export function proxy(request: NextRequest) {
  return adminGate(request);
}

export const config = {
  matcher: ["/admin/dashboard", "/admin/dashboard/:path*"],
};
