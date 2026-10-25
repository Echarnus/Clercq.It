import type { NextRequest } from "next/server";
import { endAdminSession, startAdminSession } from "@/lib/adminSession";

// The admin session cookie that proxy.ts checks (issue #454, lib/adminSession.ts).
export function POST(request: NextRequest) {
  return startAdminSession(request);
}

export function DELETE() {
  return endAdminSession();
}
