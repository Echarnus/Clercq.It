import fc from "fast-check";
import { describe, expect, it } from "vitest";
import { safeReturnTo } from "./auth";

// Property-based tests (issue #402): the return address after sign-in comes from
// the OIDC state, so it is untrusted. Whatever it holds, the redirect must stay on
// this site and inside /admin.

const ORIGIN = "https://www.clercq.it";

// Strings that look like the tricks used against redirect checks.
const tricky = fc.oneof(
  fc.string(),
  fc.webUrl(),
  fc
    .tuple(
      fc.constantFrom("/admin", "//", "/\\", "\\\\", "/admin/../", "https:", "javascript:", ""),
      fc.string(),
    )
    .map(([prefix, rest]) => prefix + rest),
);

describe("safeReturnTo (property)", () => {
  it("always returns a same-origin path under /admin", () => {
    fc.assert(
      fc.property(tricky, (value) => {
        const result = safeReturnTo(value);
        const url = new URL(result, ORIGIN);

        expect(url.origin).toBe(ORIGIN);
        expect(result.startsWith("/admin")).toBe(true);
        expect(result.startsWith("//")).toBe(false);
      }),
    );
  });

  it("falls back to the dashboard for anything that is not a string", () => {
    fc.assert(
      fc.property(
        fc.anything().filter((value) => typeof value !== "string"),
        (value) => {
          expect(safeReturnTo(value)).toBe("/admin/dashboard");
        },
      ),
    );
  });

  it("keeps a valid admin path unchanged", () => {
    fc.assert(
      fc.property(fc.webPath(), (path) => {
        const value = `/admin${path}`;
        expect(safeReturnTo(value)).toBe(value);
      }),
    );
  });
});
