import { fileURLToPath } from "node:url";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

// Unit and component tests (issue #246). Playwright e2e lives in e2e/ and is
// not run by Vitest. Coverage thresholds ratchet up: raise them when coverage
// rises, never lower them to get green (docs/testing.md).
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { "@": fileURLToPath(new URL("./", import.meta.url)) },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./test/setup.ts"],
    include: ["**/*.test.{ts,tsx}"],
    exclude: ["node_modules/**", ".next/**", "e2e/**"],
    restoreMocks: true,
    coverage: {
      provider: "v8",
      reporter: ["text-summary", "lcov", "json-summary"],
      include: ["lib/**/*.{ts,tsx}", "hooks/**/*.{ts,tsx}", "components/**/*.{ts,tsx}"],
      // shadcn/ui primitives are generated code; they are covered through the components that use them.
      exclude: ["components/ui/**", "**/*.test.{ts,tsx}"],
      thresholds: {
        // The API client is fully covered; keep it that way.
        "lib/api/**": { lines: 100, branches: 95, functions: 100, statements: 99 },
        lines: 81,
        branches: 74,
        functions: 76,
        statements: 81,
      },
    },
  },
});
