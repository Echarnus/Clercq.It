# Versioning & Branching Strategy

This document describes the versioning strategy and branching workflow used in the Clercq.It project.

## Overview

The project uses **GitVersion** combined with **commit-count-based versioning** for automatic semantic versioning. We follow **GitHub Flow** with **Continuous Deployment** mode.

### Version Format

- **Main branch**: `{Major}.{Minor}.{CommitCount}` (e.g., 1.0.67, 1.0.68)
  - The patch number is the number of commits since the last major/minor tag
  - Each commit to main gets a unique, incrementing version
- **Feature/PR branches**: `{Major}.{Minor}.{CommitCount}-{branch-label}` (e.g., 1.0.67-copilot, 1.0.68-feature)
- **Major/Minor versions**: Controlled via git tags (e.g., `v2.0.0`, `v1.1.0`)

## Branching Strategy (GitHub Flow)

### Branch Types

- **`main`** - Production branch, triggers deployment
- **`develop`** - Development branch for feature integration
- **`feature/*`** - Feature branches merged via Pull Requests
- **`hotfix/*`** - Hotfix branches for urgent production fixes

### Workflow

1. Create feature branches from `main` or `develop`
2. Develop and test features locally
3. Create Pull Request to `main` or `develop`
4. Automated tests run on PR
5. After approval and merge, automated deployment occurs (if merging to `main`)

## GitVersion Configuration

### Configuration File

The GitVersion configuration is stored in `GitVersion.yml` at the repository root. GitVersion tracks commits and provides the commit count, which is then used by the build pipeline to construct the version number.

### Version Calculation

The build pipeline (`build.yml`) calculates versions using this logic:

```bash
# For main branch
VERSION="${MAJOR}.${MINOR}.${CommitsSinceVersionSource}"

# For feature/PR branches  
VERSION="${MAJOR}.${MINOR}.${CommitsSinceVersionSource}-${branch-label}"
```

This ensures:
- **Unique versions**: Each commit gets a unique version number
- **Clean versions on main**: No pre-release tags (1.0.67, not 1.0.0-67)
- **Branch identification**: Feature branches include the branch type in the version

### Branch Configuration

#### Main Branch

```yaml
main:
  regex: ^master$|^main$
  mode: ContinuousDeployment
  label: ''
  increment: Patch
  prevent-increment:
    of-merged-branch: false
  track-merge-target: false
```

- **Mode**: ContinuousDeployment (provides commit tracking)
- **Increment**: Patch (for major/minor control via tags)
- **Label**: None (clean version numbers)
- **Prevent Increment**: `of-merged-branch: false` allows tracking of all commits
- **Actual Version**: Calculated as `1.{Minor}.{CommitCount}` by the build pipeline
- **Purpose**: Production releases with auto-incrementing version based on commit count

#### Develop Branch

```yaml
develop:
  regex: ^dev(elop)?(ment)?$
  mode: ContinuousDeployment
  label: 'alpha'
  increment: Minor
  source-branches: ['main']
```

- **Increment**: Minor (1.0.0 → 1.1.0-alpha.1)
- **Label**: alpha
- **Purpose**: Development integration

#### Feature Branches

```yaml
feature:
  regex: ^features?[/-]
  mode: ContinuousDeployment
  label: 'feature'
  increment: Inherit
  source-branches: ['develop', 'main', 'release', 'feature', 'support', 'hotfix']
```

- **Increment**: Inherit from parent branch
- **Label**: feature (e.g., 1.0.1-feature.my-feature.1)
- **Naming**: `feature/my-feature` or `features/my-feature`

#### Copilot Branches

```yaml
copilot:
  regex: ^copilot[/-]
  mode: ContinuousDeployment
  label: 'copilot'
  increment: Inherit
  source-branches: ['develop', 'main', 'release', 'feature', 'support', 'hotfix']
```

- **Increment**: Inherit from parent branch
- **Label**: copilot (e.g., 1.0.1-copilot.fix-abc.1)
- **Purpose**: GitHub Copilot automated branches
- **Naming**: `copilot/fix-*` or `copilot/feature-*`

#### Hotfix Branches

```yaml
hotfix:
  regex: ^hotfix(es)?[/-]
  mode: ContinuousDeployment
  label: 'beta'
  increment: Patch
  source-branches: ['develop', 'main', 'support']
```

- **Increment**: Patch (1.0.0 → 1.0.1-beta.1)
- **Label**: beta
- **Naming**: `hotfix/urgent-fix` or `hotfixes/urgent-fix`

#### Pull Request Branches

```yaml
pull-request:
  regex: ^(pull|pull\-requests|pr)[/-]
  mode: ContinuousDeployment
  label: 'pr'
  increment: Inherit
  source-branches: ['develop', 'main', 'release', 'feature', 'support', 'hotfix']
```

- **Increment**: Inherit from target branch
- **Label**: pr (e.g., 1.0.1-pr.123.1)
- **Purpose**: Pull request validation

#### Release Branches

```yaml
release:
  regex: ^releases?[/-]
  mode: ContinuousDeployment
  label: 'beta'
  increment: None
  source-branches: ['develop']
  is-release-branch: true
```

- **Increment**: None (maintains version)
- **Label**: beta
- **Naming**: `release/v1.0.0` or `releases/v1.0.0`

## Version Examples

### Main Branch Progression

```
Initial tag: v1.0.0 (created automatically on first build)
First commit after tag: 1.0.1
Second commit: 1.0.2
Third commit: 1.0.3
...
After 22 commits: 1.0.22
After 67 commits: 1.0.67
After 100 commits: 1.0.100
```

**How it works:**
- GitVersion tracks commits since the last version tag (v1.0.0)
- The build pipeline uses `GitVersion_CommitsSinceVersionSource` as the patch number
- Result: Each commit gets version `1.0.{CommitCount}`
- This creates clean, incrementing versions for production deployments

### Feature Branch Examples

```
feature/user-auth → 1.0.22-feature
feature/api-improvements → 1.0.22-feature
copilot/fix-123 → 1.0.67-copilot
```

**Note:** Feature branches include the branch type as a label, helping identify the source of the build.

### Develop Branch Examples

```
develop → 1.1.0-alpha.1
develop (after feature merge) → 1.1.0-alpha.2
```

### Hotfix Branch Examples

```
hotfix/security-patch → 1.0.1-beta.1
hotfix/critical-bug → 1.0.2-beta.1
```

## Usage in CI/CD

### In GitHub Actions

```yaml
- name: Install GitVersion
  uses: gittools/actions/gitversion/setup@v1
  with:
    versionSpec: '6.x'

- name: Determine Version
  uses: gittools/actions/gitversion/execute@v1
  id: gitversion

- name: Use version
  run: |
    echo "Version: ${{ steps.gitversion.outputs.semVer }}"
    echo "Full version: ${{ steps.gitversion.outputs.fullSemVer }}"
```

### Available Outputs

- `semVer`: Semantic version (e.g., 1.0.1)
- `fullSemVer`: Full semantic version with metadata
- `majorMinorPatch`: Just major.minor.patch
- `shortSha`: Short commit SHA
- `sha`: Full commit SHA
- `branchName`: Current branch name

## Version Display in Application

### Version JSON File

During the Docker build process, a `version.json` file is automatically generated in the frontend's public directory. This file contains:

```json
{
  "version": "1.0.1",
  "gitSha": "abc1234",
  "buildDate": "2024-10-05"
}
```

The file is created using GitVersion outputs:
- `version`: The semantic version from GitVersion (`semVer`)
- `gitSha`: The short commit SHA (`shortSha`)
- `buildDate`: The commit date (`commitDate`)

### Frontend Integration

The Next.js frontend fetches this file at runtime and displays the version in the footer component. This provides visibility into which version is currently deployed.

The version is displayed as: `Version 1.0.1 (abc1234)`

### Build Arguments

The Dockerfile accepts the following build arguments to generate the version file:
- `VERSION`: Semantic version number
- `GIT_SHA`: Short Git commit SHA
- `BUILD_DATE`: Build/commit date

These are automatically passed by the GitHub Actions build workflow from GitVersion outputs.

## Local Development

### Install GitVersion CLI

```bash
# Via dotnet tool
dotnet tool install --global GitVersion.Tool

# Via chocolatey (Windows)
choco install gitversion.portable

# Via brew (macOS)
brew install gitversion
```

### Check Version Locally

```bash
# Show current version
dotnet gitversion

# Show configuration
dotnet gitversion /showConfig

# What would the next version be?
dotnet gitversion /output json
```

### Example Output

```json
{
  "Major": 1,
  "Minor": 0,
  "Patch": 1,
  "PreReleaseTag": "12",
  "PreReleaseLabelWithDash": "-12",
  "SemVer": "1.0.1-12",
  "FullSemVer": "1.0.1-12",
  "InformationalVersion": "1.0.1-12+Branch.main.Sha.abc123",
  "BranchName": "main",
  "ShortSha": "abc1234",
  "CommitDate": "2024-01-15",
  "CommitsSinceVersionSource": 12
}
```

**Note**: The version includes the commit count (e.g., `1.0.1-12`) which represents 12 commits since the v1.0.0 tag. When you create a new tag (e.g., v1.0.1), subsequent commits will be `1.0.2-1`, `1.0.2-2`, etc.

## Best Practices

### Branch Naming

Follow consistent naming conventions:
- `feature/description`
- `hotfix/issue-description`
- `release/v1.0.0`

### Tagging

Use semantic version tags for releases:
- `v1.0.0`, `v1.0.1`, `v2.0.0`

**Initial Tag Requirement:**
- GitVersion requires at least one version tag to calculate incremental versions
- The build pipeline automatically creates `v1.0.0` if no tags exist
- After the initial tag, each commit to `main` increments the patch version automatically

### Merge Strategy

Use merge commits to maintain history:
- Avoid squash merges for version calculation
- Use `--no-ff` for important merges

### Documentation

Keep version history clear:
- Tag releases with meaningful messages
- Maintain CHANGELOG.md for major releases

## Troubleshooting

### Common Issues

1. **Version not incrementing**
   - **On tag commit**: If you create a tag (e.g., `v1.0.0`) on the current commit, GitVersion will show `1.0.0` for that commit. The patch increment to `1.0.1` only happens on the NEXT commit after the tag.
   - **Branch name mismatch**: Ensure your branch name matches one of the configured patterns in GitVersion.yml (e.g., `main`, `develop`, `feature/*`, `copilot/*`, `hotfix/*`)
   - **No base tag**: GitVersion needs at least one version tag as a baseline. The build pipeline creates `v1.0.0` automatically if none exists.
   - **First run after setup**: The first commit after setting up GitVersion will use the initial tag. Subsequent commits will increment.
   - **Prevent increment on merge**: If `prevent-increment.of-merged-branch: true` is set for the main branch, GitVersion will not increment the version when branches are merged. This should be set to `false` to ensure each merge to main increments the version.

2. **Incorrect version calculation**
   - Check branch naming convention matches GitVersion.yml patterns
   - Verify GitVersion.yml syntax
   - Ensure proper merge history

3. **Missing version tags**
   - The build pipeline automatically creates the initial `v1.0.0` tag if none exists
   - To manually create a tag: `git tag v1.0.0`
   - To push tags: `git push --tags`
   - Verify tags exist: `git tag -l`

4. **Configuration not applying**
   - Verify GitVersion.yml is in repository root
   - Check YAML syntax and indentation
   - Clear GitVersion cache: `dotnet gitversion /nocache`

### Debug Commands

```bash
# Show configuration being used
dotnet gitversion /showConfig

# Show verbose logging
dotnet gitversion /l Debug

# Show what variables would be set
dotnet gitversion /showvariable FullSemVer

# Clear cache and recalculate
dotnet gitversion /nocache
```

## Migration Guide

### From Manual Versioning

1. Install GitVersion in your CI/CD pipeline
2. Create `GitVersion.yml` configuration
3. Tag your current version: `git tag v1.0.0`
4. Update build scripts to use GitVersion outputs
5. Test with feature branch to verify behavior

### Configuration Changes

When updating GitVersion configuration:

1. Test changes in feature branch first
2. Verify version calculation with `dotnet gitversion /showConfig`
3. Update documentation
4. Communicate changes to development team

## Resources

- [GitVersion Documentation](https://gitversion.net/docs/)
- [GitHub Flow Examples](https://gitversion.net/docs/learn/branching-strategies/githubflow/examples)
- [Semantic Versioning](https://semver.org/)
- [Git Flow vs GitHub Flow](https://lucamezzalira.com/2014/03/10/git-flow-vs-github-flow/)
