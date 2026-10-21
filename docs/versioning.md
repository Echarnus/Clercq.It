# Versioning & Branching Strategy

Clercq.It uses **trunk-based development with continuous deployment to production** (owner decision, 21 October 2026). There are no release, develop or hotfix branches: `main` is the only long-lived branch, every merge to `main` that passes the pipeline goes straight to production, and each production deploy is tagged and released automatically.

## Branching

- **`main`** is the trunk. It is always deployable and is the only long-lived branch.
- **Work branches** are short-lived (hours to days) and created from `main` per issue: `<type>/<issue>-<slug>`, where type is `feat`, `fix`, `chore`, `infra`, `content`, `docs` or `security`. Example: `fix/231-admin-login`.
- Work branches merge back through a pull request once the required checks pass (squash merge). See [Contributing](./contributing.md) for the full issue → PR → production loop.
- Unfinished work that must merge early is hidden behind a feature flag instead of living on a long branch.
- A fix for production is a normal work branch (`fix/...`) merged to `main`; there is no separate hotfix flow.
- Rollback = redeploy the image of an earlier release (`Deploy to Scaleway` → Run workflow → version). A failed post-deploy smoke test rolls back automatically ([devops.md](./devops.md#smoke-test-and-rollback)).

Not used, on purpose: `develop`, `release/*`, `hotfix/*`, `support/*`. If one appears, it is a mistake.

## Versions

Versions are calculated by [GitVersion](https://gitversion.net) (`GitVersion.yml`) and the composite action `.github/actions/calculate-version`.

| Branch | Version | Example |
| --- | --- | --- |
| `main` | `{Major}.{Minor}.{CommitsSinceVersionSource}` | `1.0.190` |
| Work branch | `{Major}.{Minor}.{Commits}-{label}` (label from GitVersion, or the branch type) | `1.0.190-fix` |

- The patch number is the number of commits on `main` since the last `v`-tag. With squash merges, each merged PR adds one.
- **Major and minor** are bumped by pushing a `v`-tag by hand, for example `v1.1.0` or `v2.0.0`. Only do this for a deliberate version jump.
- The build pipeline creates `v1.0.0` if no `v`-tag exists, because GitVersion needs a baseline.

## Production releases

After every successful automatic deploy, the `release` job in `.github/workflows/deploy.yml` creates:

- a tag `prod-<version>` on the deployed commit, for example `prod-1.0.190`;
- a GitHub Release titled `<version>` with notes generated from the merged PRs.

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
- **Version jumped back to a low patch number**: a new `v`-tag was created. Remove it if it was a mistake (`git push --delete origin vX.Y.Z`), or accept it as the new baseline.
- **Branch gets an odd label**: the branch name does not start with one of the types above; rename it.
