# Versioning & Branching Strategy

Clercq.It uses **trunk-based development with continuous deployment to production** (owner decision, 21 October 2026). There are no release, develop or hotfix branches: `main` is the only long-lived branch, every merge to `main` that passes the pipeline goes straight to production, and each production deploy is tagged and released automatically.

## Branching

- **`main`** is the trunk. It is always deployable and is the only long-lived branch.
- **Work branches** are short-lived (hours to days) and created from `main` per issue: `<type>/<issue>-<slug>`, where type is `feat`, `fix`, `chore`, `infra`, `content`, `docs` or `security`. Example: `fix/231-admin-login`.
- Work branches merge back through a pull request once the required checks pass (squash merge). See [Contributing](./contributing.md) for the full issue → PR → production loop.
- Unfinished work that must merge early is hidden behind a [feature flag](#feature-flags) instead of living on a long branch.
- A fix for production is a normal work branch (`fix/...`) merged to `main`; there is no separate hotfix flow.
- Rollback = redeploy the image of an earlier release (`Deploy to Scaleway` → Run workflow → version). A failed post-deploy smoke test rolls back automatically ([devops.md](./devops.md#smoke-test-and-rollback)).

Not used, on purpose: `develop`, `release/*`, `hotfix/*`, `support/*`. If one appears, it is a mistake.

## Feature flags

A feature flag lets unfinished work merge to `main` and ship to production switched off (issue #253). Flags are plain configuration: the `FeatureFlags` section of the API configuration, one boolean per flag. Every flag is off unless it is set to `true`.

| Where | How a flag is turned on |
| --- | --- |
| Production | the `feature_flags` default in `infra/terraform/feature_flags.tf`, e.g. `{ NewBlogEditor = true }`. Terraform sets the container env var `FeatureFlags__NewBlogEditor=true`; `Deploy Infra` applies it on merge. |
| Local (Aspire or `dotnet run`) | `src/ClercqIt.Api/appsettings.Development.json` (`"FeatureFlags": { "NewBlogEditor": true }`) or the env var `FeatureFlags__NewBlogEditor=true` |
| API integration tests | `factory.WithWebHostBuilder(b => b.UseSetting("FeatureFlags:NewBlogEditor", "true"))` |
| Frontend unit tests | override the MSW handler for `/api/features` with `server.use(...)` to return `{ NewBlogEditor: true }` (the default handler returns no flags) |

**Add and use a flag**

- API endpoint or group: `.RequireFeature("NewBlogEditor")`. While the flag is off it answers 404, as if the endpoint did not exist. In a handler or service, inject `IFeatureFlags` and call `IsEnabled("NewBlogEditor")`.
- MCP tool or authorization policy: `.AddRequirements(new FeatureFlagRequirement("NewBlogEditor"))` on the policy (`src/ClercqIt.Api/FeatureFlags/FeatureFlagRequirement.cs`). The policy fails while the flag is off, so the MCP server hides the tool.
- Frontend: `isEnabled()` from `src/ClercqIt.Web/lib/featureFlags.ts` reads `GET /api/features` with the signed-in admin's token, so it only works in the admin UI (off when nobody is signed in or the API is unreachable). Hide a section or link of the admin UI with `isEnabled("NewBlogEditor")`. A public page cannot read flags: if one ever needs to, add a separate public endpoint for just that flag.
- Name flags in PascalCase after the feature, not after a ticket. Names are case-insensitive.

`GET /api/features` needs an admin role (issue #454): anonymous visitors get 401, so flag names do not tell them what is being built. Flags are still not access control: admin features need their own authorization policy.

**Remove a flag** once the feature is live: delete the `RequireFeature` and `isEnabled` calls, the entry in `infra/terraform/feature_flags.tf` and any local settings, in one PR. A flag that stays on for more than a few weeks is dead code.

## Versions

Versions are calculated by [GitVersion](https://gitversion.net) (`GitVersion.yml`) and the composite action `.github/actions/calculate-version`.

| Branch | Version | Example |
| --- | --- | --- |
| `main` | `{Major}.{Minor}.{CommitsSinceVersionSource}` | `1.0.190` |
| Work branch | `{Major}.{Minor}.{Commits}-{label}` (label from GitVersion, or the branch type) | `1.0.190-fix` |

- The patch number is the number of commits on `main` since the last `v`-tag. With squash merges, each merged PR adds one.
- **Major and minor** are bumped with a new `v`-tag, for example `v1.1.0` or `v2.0.0`. Only do this for a deliberate version jump. The `prod-tags` ruleset ([Enforced rules](./contributing.md#enforced-rules)) lets only GitHub Actions create, move or delete `v*` tags, so a tag pushed by hand is rejected. An admin has to disable that ruleset for the push and enable it again.
- The build pipeline creates `v1.0.0` if no `v`-tag exists, because GitVersion needs a baseline.

## Production releases

After every successful automatic deploy, the `release` job in `.github/workflows/deploy.yml` creates:

- a tag `prod-<version>` on the deployed commit, for example `prod-1.0.190`;
- a GitHub Release titled `<version>` with the image digest and notes generated from the merged PRs;
- two release assets with the image's signed SLSA build provenance (#403): `clercq-it-<version>.intoto.jsonl` (the in-toto statements as DSSE envelopes) and `clercq-it-<version>.sigstore.json` (the full Sigstore bundle).

The provenance is the attestation `build.yml` creates with `actions/attest-build-provenance`; the release job fetches it from the repository's attestations API by image digest. If none is found, the release is still created, without assets, and the run shows a warning. To check a release against the image:

```bash
gh attestation verify oci://docker.io/echarnus/clercq-it@<digest> \
  --repo Echarnus/Clercq.It --bundle clercq-it-<version>.sigstore.json
```

OpenSSF Scorecard's Signed-Releases check reads these assets on the last five releases.

The `prod-` prefix is deliberate: GitVersion treats `v`-tags as version sources, so tagging each deploy with `v1.0.190` would reset the commit count and make the next version `1.0.1`. Tags without the `v` prefix that do not parse as a version are ignored by GitVersion.

Manual deploys (`workflow_dispatch`, used for rollbacks) do not create a new release.

## Version display in the application

During the Docker build a `version.json` file is written to the frontend's public directory:

```json
{ "version": "1.0.190", "gitSha": "abc1234", "buildDate": "2026-10-21" }
```

It is served at `https://www.clercq.it/version.json` and shown in the footer as `Version 1.0.190 (abc1234)`. The Dockerfile takes `VERSION`, `GIT_SHA` and `BUILD_DATE` build arguments, passed by `build.yml` from GitVersion.

## Docker image tags

`build.yml` pushes `echarnus/clercq-it:<version>`, `:<shortSha>` and `:latest` for every build of `main`.

## Checking the version locally

```bash
dotnet tool install --global GitVersion.Tool
dotnet gitversion                     # full output
dotnet gitversion /showvariable FullSemVer
dotnet gitversion /showConfig
```

## Troubleshooting

- **Version did not increment**: a commit that carries a `v`-tag shows that tag's version; the next commit increments. Check that no one pushed a `v`-tag by accident (`git tag -l 'v*'`).
- **Version jumped back to a low patch number**: a new `v`-tag was created. Accept it as the new baseline, or have an admin remove it (`git push --delete origin vX.Y.Z` is blocked by the `prod-tags` ruleset until it is disabled).
- **Branch gets an odd label**: the branch name does not start with one of the types above; rename it.
