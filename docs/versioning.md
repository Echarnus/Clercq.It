# Versioning & Branching Strategy

This document describes the versioning strategy and branching workflow used in the Clercq.It project.

## Overview

The project uses **GitVersion** for automatic semantic versioning based on Git history and branch naming conventions. We follow **GitHub Flow** with **Continuous Deployment** mode.

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

The GitVersion configuration is stored in `GitVersion.yml` at the repository root. The global mode is set to `ContinuousDelivery` to ensure each commit gets a unique version number with build metadata.

### Branch Configuration

#### Main Branch

```yaml
main:
  regex: ^master$|^main$
  mode: ContinuousDelivery
  label: ''
  increment: Patch
  track-merge-target: false
```

- **Mode**: ContinuousDelivery (includes commit count in version)
- **Increment**: Patch (1.0.0 → 1.0.1-n where n is commits since tag)
- **Label**: None (clean version when tagged)
- **Purpose**: Production releases with unique version for each commit

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
First commit: 1.0.1-1
Second commit: 1.0.1-2
Third commit: 1.0.1-3
...
After creating v1.0.1 tag: 1.0.2
Next commit: 1.0.2-1
```

**Note:** Each commit to the `main` branch gets a unique version with build metadata. This happens because:
- The `main` branch is configured with `mode: ContinuousDelivery` and `increment: Patch` in GitVersion
- GitVersion uses the latest tag as a baseline and adds the commit count since that tag
- The version format is `{major}.{minor}.{patch}-{commits-since-tag}`
- To get a clean version number (e.g., 1.0.2), create a new git tag for that version

### Feature Branch Examples

```
feature/user-auth → 1.0.1-feature.user-auth.1
feature/api-improvements → 1.0.1-feature.api-improvements.1
```

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

1. **Incorrect version calculation**
   - Check branch naming convention
   - Verify GitVersion.yml syntax
   - Ensure proper merge history

2. **Missing version tags**
   - The build pipeline automatically creates the initial `v1.0.0` tag if none exists
   - To manually create a tag: `git tag v1.0.0`
   - To push tags: `git push --tags`
   - Verify tags exist: `git tag -l`

3. **Configuration not applying**
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
