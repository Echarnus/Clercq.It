import { z } from "zod";
import { authFetch } from "./api/adminApi";

// Feature flags (issue #253). The API owns them: `GET /api/features` returns the
// `FeatureFlags` configuration section, to signed-in admins only (issue #454), so this
// is admin-only code. See docs/versioning.md#feature-flags.
const FeatureFlagsSchema = z.record(z.string(), z.boolean());

export type FeatureFlags = z.infer<typeof FeatureFlagsSchema>;

/**
 * All configured flags, read with the admin's token. Fails closed: when nobody is
 * signed in, the API is down or answers with something unexpected, every flag reads as off.
 */
export async function fetchFeatureFlags(): Promise<FeatureFlags> {
  try {
    const response = await authFetch("/api/features");
    if (!response.ok) return {};
    const result = FeatureFlagsSchema.safeParse(await response.json());
    return result.success ? result.data : {};
  } catch {
    return {};
  }
}

/** Whether a flag is on. Names are case-insensitive, as in the API. */
export async function isEnabled(name: string): Promise<boolean> {
  const flags = await fetchFeatureFlags();
  const wanted = name.toLowerCase();
  return Object.entries(flags).some(([flag, on]) => on && flag.toLowerCase() === wanted);
}
