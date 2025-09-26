# GitVersion Configuration

This document explains the GitVersion configuration used in the Clercq.It project.

## Overview

GitVersion automatically calculates semantic version numbers based on Git history and branch naming conventions. The project uses **GitHub Flow** with **Continuous Deployment** mode.

## Configuration File

The GitVersion configuration is stored in `GitVersion.yml` at the repository root.

## Branch Configuration

### Main Branch

```yaml
main:
  regex: ^master$|^main$
  mode: ContinuousDeployment
  tag: ''
  increment: Patch
  prevent-increment-of-merged-branch-version: true
  track-merge-target: false
  source-branches: ['develop', 'feature', 'support', 'hotfix']
  is-mainline: true
```

- **Increment**: Patch (1.0.0 → 1.0.1)
- **Tag**: None (clean version)
- **Purpose**: Production releases

### Develop Branch

```yaml
develop:
  regex: ^dev(elop)?(ment)?$
  mode: ContinuousDeployment
  tag: 'alpha'
  increment: Minor
  source-branches: ['main']
  is-mainline: false
```

- **Increment**: Minor (1.0.0 → 1.1.0-alpha.1)
- **Tag**: alpha
- **Purpose**: Development integration

### Feature Branches

```yaml
feature:
  regex: ^features?[/-]
  mode: ContinuousDeployment
  tag: 'feature'
  increment: Inherit
  source-branches: ['develop', 'main', 'release', 'feature', 'support', 'hotfix']
```

- **Increment**: Inherit from parent branch
- **Tag**: feature (e.g., 1.0.1-feature.my-feature.1)
- **Naming**: `feature/my-feature` or `features/my-feature`

### Hotfix Branches

```yaml
hotfix:
  regex: ^hotfix(es)?[/-]
  mode: ContinuousDeployment
  tag: 'beta'
  increment: Patch
  source-branches: ['develop', 'main', 'support']
```

- **Increment**: Patch (1.0.0 → 1.0.1-beta.1)
- **Tag**: beta
- **Naming**: `hotfix/urgent-fix` or `hotfixes/urgent-fix`

### Pull Request Branches

```yaml
pull-request:
  regex: ^(pull|pull\-requests|pr)[/-]
  mode: ContinuousDeployment
  tag: 'pr'
  increment: Inherit
  source-branches: ['develop', 'main', 'release', 'feature', 'support', 'hotfix']
```

- **Increment**: Inherit from target branch
- **Tag**: pr (e.g., 1.0.1-pr.123.1)
- **Purpose**: Pull request validation

### Release Branches

```yaml
release:
  regex: ^releases?[/-]
  mode: ContinuousDeployment
  tag: 'beta'
  increment: None
  prevent-increment-of-merged-branch-version: true
  is-release-branch: true
```

- **Increment**: None (maintains version)
- **Tag**: beta
- **Naming**: `release/v1.0.0` or `releases/v1.0.0`

## Version Examples

### Main Branch Progression

```
Initial: 1.0.0
After fix: 1.0.1
After another fix: 1.0.2
```

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
  "PreReleaseTag": "",
  "PreReleaseTagWithDash": "",
  "PreReleaseLabel": "",
  "PreReleaseLabelWithDash": "",
  "PreReleaseNumber": null,
  "WeightedPreReleaseNumber": 60000,
  "BuildMetaData": "",
  "BuildMetaDataPadded": "",
  "FullBuildMetaData": "1.Branch.main.Sha.abc123",
  "MajorMinorPatch": "1.0.1",
  "SemVer": "1.0.1",
  "LegacySemVer": "1.0.1",
  "LegacySemVerPadded": "1.0.1",
  "AssemblySemVer": "1.0.0.0",
  "AssemblySemFileVer": "1.0.1.0",
  "FullSemVer": "1.0.1",
  "InformationalVersion": "1.0.1+1.Branch.main.Sha.abc123",
  "BranchName": "main",
  "EscapedBranchName": "main",
  "Sha": "abc123456789...",
  "ShortSha": "abc1234",
  "NuGetVersionV2": "1.0.1",
  "NuGetVersion": "1.0.1",
  "NuGetPreReleaseTagV2": "",
  "NuGetPreReleaseTag": "",
  "VersionSourceSha": "abc123456789...",
  "CommitsSinceVersionSource": 0,
  "CommitsSinceVersionSourcePadded": "0000",
  "UncommittedChanges": 0,
  "CommitDate": "2024-01-15"
}
```

## Troubleshooting

### Common Issues

1. **Incorrect version calculation**
   - Check branch naming convention
   - Verify GitVersion.yml syntax
   - Ensure proper merge history

2. **Missing version tags**
   - Create initial version tag: `git tag v1.0.0`
   - Push tags: `git push --tags`

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

## Best Practices

1. **Branch Naming**: Follow consistent naming conventions
   - `feature/description`
   - `hotfix/issue-description`
   - `release/v1.0.0`

2. **Tagging**: Use semantic version tags for releases
   - `v1.0.0`, `v1.0.1`, `v2.0.0`

3. **Merge Strategy**: Use merge commits to maintain history
   - Avoid squash merges for version calculation
   - Use `--no-ff` for important merges

4. **Documentation**: Keep version history clear
   - Tag releases with meaningful messages
   - Maintain CHANGELOG.md for major releases

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