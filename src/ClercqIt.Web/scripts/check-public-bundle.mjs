#!/usr/bin/env node
// Checks what an anonymous visitor can download from a production build (issue #454,
// docs/application-security.md#admin-pages). Run after `pnpm build`:
//
//   pnpm check:public-bundle
//
// It starts `next start`, fetches every public page as a visitor without a session,
// downloads each JavaScript chunk those pages load, and fails when:
//   - a public page's HTML or JS contains admin code (MyZen, the admin API client,
//     the sign-in library, admin endpoints);
//   - the sign-in pages under /admin contain dashboard code;
//   - /admin/dashboard serves anything but a bare redirect to /admin;
//   - the build contains browser source maps (.map files).
import { spawn } from "node:child_process";
import { readdirSync } from "node:fs";
import { join } from "node:path";

const PORT = Number(process.env.CHECK_PORT ?? 3399);
const BASE = `http://127.0.0.1:${PORT}`;
const ROOT = new URL("..", import.meta.url).pathname;

// Strings that only admin code contains. Case-sensitive, except MyZen in any spelling.
const DASHBOARD_CODE = [
  /myzen/i, // MyZen panel and API client
  "/api/stats?days", // statistics panel
  "Statistics request failed",
  "/api/features", // feature flags are admin-only
  "/api/images", // image upload
  "Not authenticated", // authFetch, the admin API client
  "Contributor", // role names
  "Admin.View",
];
const SIGN_IN_CODE = [
  "signinRedirect", // oidc-client-ts
  "/api/auth/config",
  "/admin/session",
  "admin/dashboard",
  "admin/callback",
];

const PUBLIC_PAGES = [
  "/",
  "/home",
  "/blogs",
  "/portfolio",
  "/certifications",
  "/this-page-does-not-exist",
];
// Public by necessity (they are how an admin signs in), but without dashboard code.
const SIGN_IN_PAGES = ["/admin", "/admin/callback"];
const GATED_PAGES = ["/admin/dashboard", "/admin/dashboard/blogs"];

const failures = [];
const chunkCache = new Map();

function findSourceMaps(dir) {
  // eslint-disable-next-line security/detect-non-literal-fs-filename -- a fixed path in this app's own build output
  return readdirSync(dir, { withFileTypes: true, recursive: true })
    .filter((entry) => entry.isFile() && entry.name.endsWith(".map"))
    .map((entry) => join(entry.parentPath, entry.name));
}

async function waitForServer(server) {
  for (let i = 0; i < 60; i++) {
    if (server.exitCode !== null) throw new Error("next start exited early");
    try {
      await fetch(`${BASE}/robots.txt`);
      return;
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 500));
    }
  }
  throw new Error("next start did not answer within 30s");
}

async function chunk(path) {
  if (!chunkCache.has(path)) {
    const response = await fetch(`${BASE}${path}`);
    chunkCache.set(path, response.ok ? await response.text() : "");
  }
  return chunkCache.get(path);
}

async function scan(page, forbidden) {
  const response = await fetch(`${BASE}${page}`, { redirect: "manual" });
  const html = await response.text();
  const scripts = [...new Set(html.match(/\/_next\/static\/[\w./-]+?\.js/g) ?? [])];
  const sources = [{ name: "HTML", text: html }];
  for (const script of scripts) sources.push({ name: script, text: await chunk(script) });

  for (const { name, text } of sources) {
    for (const needle of forbidden) {
      if (typeof needle === "string" ? text.includes(needle) : needle.test(text)) {
        failures.push(`${page}: ${name} contains ${String(needle)}`);
      }
    }
  }
  console.log(`${page}: ${response.status}, ${scripts.length} scripts scanned`);
}

async function checkGated(page) {
  const response = await fetch(`${BASE}${page}`, { redirect: "manual" });
  const body = await response.text();
  const header = response.headers.get("location");
  const location = header ? new URL(header, BASE).pathname : null;
  // Next.js puts the target URL in the body; anything that looks like a page is a leak.
  if (response.status !== 307 || location !== "/admin" || /<|_next/.test(body)) {
    failures.push(
      `${page}: expected a bare 307 to /admin for a visitor, got ${response.status} to ${location} (${body.length} bytes)`,
    );
  } else {
    console.log(`${page}: 307 to /admin, nothing served`);
  }
}

const maps = findSourceMaps(join(ROOT, ".next", "static"));
for (const map of maps) failures.push(`source map in the build: ${map}`);

// SSR calls fail fast against a closed port; the pages render their empty states.
const server = spawn("pnpm", ["exec", "next", "start", "-p", String(PORT), "-H", "127.0.0.1"], {
  cwd: ROOT,
  env: { ...process.env, API_INTERNAL_URL: "http://127.0.0.1:9" },
  stdio: ["ignore", "ignore", "inherit"],
});

try {
  await waitForServer(server);
  for (const page of PUBLIC_PAGES) await scan(page, [...DASHBOARD_CODE, ...SIGN_IN_CODE]);
  for (const page of SIGN_IN_PAGES) await scan(page, DASHBOARD_CODE);
  for (const page of GATED_PAGES) await checkGated(page);
} catch (error) {
  failures.push(String(error));
} finally {
  server.kill();
}

if (failures.length > 0) {
  console.error("\nAdmin code or details reach anonymous visitors:");
  for (const failure of failures) console.error(`  - ${failure}`);
  process.exit(1);
}
console.log("\nPublic bundle check passed: no admin code reaches anonymous visitors.");
