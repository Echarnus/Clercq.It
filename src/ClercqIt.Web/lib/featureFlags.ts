import { z } from "zod";
import { getApiUrl } from "./api/baseApi";

// Feature flags (issue #253). The API owns them: `GET /api/features` returns the
// `FeatureFlags` configuration section. See docs/versioning.md#feature-flags.
const FeatureFlagsSchema = z.record(z.string(), z.boolean());

export type FeatureFlags = z.infer<typeof FeatureFlagsSchema>;

/**
 * All configured flags. Fails closed: when the API is down or answers with
 * something unexpected, every flag reads as off. Cached for a minute, so a
 * flagged page does not add an API call per render.
 */
export async function fetchFeatureFlags(): Promise<FeatureFlags> {
  try {
    const response = await fetch(`${getApiUrl()}/api/features`, { next: { revalidate: 60 } });
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
