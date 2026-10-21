# Feature flags in production (issue #253). Turn a flag on with a PR that sets
# it in the default below, e.g. { NewBlogEditor = true }; Deploy Infra applies
# it on merge. Unset flags are off. See docs/versioning.md#feature-flags.
variable "feature_flags" {
  description = "Feature flags for the website container. Each becomes the env var FeatureFlags__<Name>."
  type        = map(bool)
  default     = {}
}
