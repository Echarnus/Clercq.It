# Calculate Version Action

A reusable composite action that calculates semantic version based on GitVersion commit count.

## Description

This action takes GitVersion outputs and calculates a clean semantic version using the commit count as the patch number. It supports different versioning strategies for main branch vs feature branches.

## Inputs

| Input | Description | Required | Default |
|-------|-------------|----------|---------|
| `major` | Major version number from GitVersion | Yes | - |
| `minor` | Minor version number from GitVersion | Yes | - |
| `commits-since-version-source` | Number of commits since version source from GitVersion | Yes | - |
| `branch-name` | Current branch name | Yes | - |
| `pre-release-label` | Pre-release label from GitVersion | No | `''` |

## Outputs

| Output | Description |
|--------|-------------|
| `version` | Calculated semantic version |

## Version Format

### Main Branch
- Format: `{Major}.{Minor}.{CommitCount}`
- Example: `1.0.67`, `1.0.68`, `2.0.1`

### Feature/PR Branches
- Format: `{Major}.{Minor}.{CommitCount}-{label}`
- Examples: 
  - `1.0.67-copilot`
  - `1.0.68-feature`
  - `1.0.69-pr`

## Usage

```yaml
- name: Install GitVersion
  uses: gittools/actions/gitversion/setup@v1
  with:
    versionSpec: '6.0.x'

- name: Determine Version
  uses: gittools/actions/gitversion/execute@v1
  id: gitversion

- name: Calculate commit-based version
  id: version
  uses: ./.github/actions/calculate-version
  with:
    major: ${{ steps.gitversion.outputs.major }}
    minor: ${{ steps.gitversion.outputs.minor }}
    commits-since-version-source: ${{ steps.gitversion.outputs.commitsSinceVersionSource }}
    branch-name: ${{ github.ref_name }}
    pre-release-label: ${{ steps.gitversion.outputs.preReleaseLabel }}

- name: Use the version
  run: |
    echo "Version: ${{ steps.version.outputs.version }}"
```

## How It Works

1. For the `main` branch, it creates a clean semantic version using the commit count as the patch number
2. For feature branches, it appends a label based on:
   - GitVersion's `preReleaseLabel` if available
   - Branch name pattern matching (copilot/*, feature/*, pr/*, etc.)
3. Outputs the calculated version for use in subsequent workflow steps

## Version Control via Tags

The action works with GitVersion's tag-based versioning:
- Create tag `v1.1.0` → next commits become `1.1.1`, `1.1.2`, etc.
- Create tag `v2.0.0` → next commits become `2.0.1`, `2.0.2`, etc.
