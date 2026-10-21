#!/usr/bin/env python3
"""Weekly audit of the trunk-based rules (issue #245).

Compares what GitHub enforces with what the repository says it enforces and
reports drift:

- the live rulesets differ from .github/rulesets/*.json, or one is missing
- repository settings (squash only, auto-merge, delete head branches)
- release/* or develop branches
- branches older than MAX_BRANCH_AGE_DAYS that are not finished (no merged or
  closed PR)
- a production deploy from main in the last DEPLOY_WINDOW_DAYS without a
  prod-<version> tag
- docs that link to or name repository files that no longer exist

On drift it opens (or comments on) a type:bug priority:p1 issue.
See docs/contributing.md#enforced-rules.

Usage:
  audit_repo.py [--dry-run]        GH_TOKEN and GITHUB_REPOSITORY must be set
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path
from urllib.parse import quote

MAX_BRANCH_AGE_DAYS = 7
DEPLOYS_TO_CHECK = 20
# Only recent deploys: older ones predate the release job that tags them.
DEPLOY_WINDOW_DAYS = 14
FORBIDDEN_BRANCH = re.compile(r"^(release/.*|develop|development|master)$")
ISSUE_TITLE = "Repository audit: drift from the trunk-based rules"
ISSUE_LABELS = ["type:bug", "priority:p1", "status:ready", "area:ci"]
DEPLOY_JOB = "Deploy to Scaleway"

# Repository settings the loop depends on (docs/contributing.md).
EXPECTED_SETTINGS = {
    "allow_squash_merge": True,
    "allow_merge_commit": False,
    "allow_rebase_merge": False,
    "allow_auto_merge": True,
    "delete_branch_on_merge": True,
}

DOC_FILES = ["CLAUDE.md", "README.md"]
DOC_DIRS = ["docs"]
# Backticked paths under these roots are checked; markdown links always are.
PATH_ROOTS = ("src/", "infra/", "tests/", ".github/", "docs/", ".claude/", ".semgrep/")
PLACEHOLDER = re.compile(r"[<>*{}$]|\.\.\.")
LINK = re.compile(r"\[[^\]]*\]\(([^)\s]+)\)")
CODE_PATH = re.compile(r"`([^`\s]+)`")


class Api:
    """Thin wrapper over `gh api`; tests replace it with a fake."""

    def __init__(self, repo: str):
        self.repo = repo

    def get(self, path: str):
        path = path.replace("{repo}", self.repo)
        out = subprocess.run(["gh", "api", path], check=True, capture_output=True, text=True).stdout
        return json.loads(out) if out.strip() else None

    def get_all(self, path: str) -> list:
        items, page = [], 1
        sep = "&" if "?" in path else "?"
        while True:
            batch = self.get(f"{path}{sep}per_page=100&page={page}")
            items.extend(batch)
            if len(batch) < 100:
                return items
            page += 1

    def post(self, path: str, body: dict):
        path = path.replace("{repo}", self.repo)
        subprocess.run(["gh", "api", "-X", "POST", path, "--input", "-"],
                       input=json.dumps(body), check=True, capture_output=True, text=True)


# ---------------------------------------------------------------- rulesets

def normalise_rules(rules: list) -> dict:
    """Map rule type -> parameters, with required checks reduced to their contexts."""
    result = {}
    for rule in rules:
        params = dict(rule.get("parameters") or {})
        if rule["type"] == "required_status_checks":
            params["required_status_checks"] = sorted(c["context"] for c in params.get("required_status_checks", []))
        result[rule["type"]] = params
    return result


def check_rulesets(api, expected: dict) -> list:
    """expected: ruleset name -> ruleset JSON from .github/rulesets/."""
    findings = []
    live = {r["name"]: r for r in api.get("repos/{repo}/rulesets?includes_parents=false")}
    for name, want in sorted(expected.items()):
        if name not in live:
            findings.append(f"Ruleset `{name}` is missing (defined in `.github/rulesets/{name}.json`).")
            continue
        have = api.get(f"repos/{{repo}}/rulesets/{live[name]['id']}")
        if have.get("enforcement") != want["enforcement"]:
            findings.append(f"Ruleset `{name}` enforcement is `{have.get('enforcement')}`, expected `{want['enforcement']}`.")
        if have.get("target") != want["target"]:
            findings.append(f"Ruleset `{name}` targets `{have.get('target')}`, expected `{want['target']}`.")
        if (have.get("conditions") or {}).get("ref_name") != want["conditions"]["ref_name"]:
            findings.append(f"Ruleset `{name}` applies to different refs than `.github/rulesets/{name}.json`.")
        want_rules, have_rules = normalise_rules(want["rules"]), normalise_rules(have.get("rules", []))
        for rule_type, params in want_rules.items():
            if rule_type not in have_rules:
                findings.append(f"Ruleset `{name}` is missing the `{rule_type}` rule.")
                continue
            for key, value in params.items():
                if have_rules[rule_type].get(key) != value:
                    findings.append(
                        f"Ruleset `{name}` rule `{rule_type}`: `{key}` is "
                        f"`{json.dumps(have_rules[rule_type].get(key))}`, expected `{json.dumps(value)}`.")
        for rule_type in sorted(set(have_rules) - set(want_rules)):
            findings.append(f"Ruleset `{name}` has a `{rule_type}` rule that is not in `.github/rulesets/{name}.json`.")
        want_bypass = sorted((b["actor_type"], b.get("actor_id")) for b in want.get("bypass_actors", []))
        have_bypass = sorted((b["actor_type"], b.get("actor_id")) for b in have.get("bypass_actors") or [])
        if want_bypass != have_bypass:
            findings.append(f"Ruleset `{name}` bypass list differs from `.github/rulesets/{name}.json`.")
    return findings


def check_settings(api) -> list:
    repo = api.get("repos/{repo}")
    findings = []
    for key, want in EXPECTED_SETTINGS.items():
        # Some settings are only visible to admins; a token that cannot see one skips it.
        if key in repo and repo[key] != want:
            findings.append(f"Repository setting `{key}` is `{json.dumps(repo[key])}`, expected `{json.dumps(want)}`.")
    return findings


# ---------------------------------------------------------------- branches

def check_branches(api, now: datetime) -> list:
    findings = []
    owner = api.repo.split("/")[0]
    for branch in api.get_all("repos/{repo}/branches"):
        name = branch["name"]
        if name == "main":
            continue
        if FORBIDDEN_BRANCH.match(name):
            findings.append(f"Branch `{name}` exists; trunk-based development has no release or develop branches.")
            continue
        commit = api.get(f"repos/{{repo}}/commits/{branch['commit']['sha']}")
        committed = parse_time(commit["commit"]["committer"]["date"])
        age = now - committed
        if age <= timedelta(days=MAX_BRANCH_AGE_DAYS):
            continue
        pulls = api.get(f"repos/{{repo}}/pulls?state=all&head={quote(owner + ':' + name, safe='')}")
        if any(p["state"] == "closed" for p in pulls) and not any(p["state"] == "open" for p in pulls):
            continue  # finished work; deleting the branch is cleanup, not drift
        status = "open PR" if pulls else "no PR"
        findings.append(f"Branch `{name}` is {age.days} days old with {status} (limit {MAX_BRANCH_AGE_DAYS} days).")
    return findings


# ---------------------------------------------------------------- deploys

def check_deploy_tags(api, now: datetime) -> list:
    tags = api.get("repos/{repo}/git/matching-refs/tags/prod-")
    tagged = {}
    for ref in tags:
        sha = ref["object"]["sha"]
        if ref["object"]["type"] == "tag":
            sha = api.get(f"repos/{{repo}}/git/tags/{sha}")["object"]["sha"]
        tagged[sha] = ref["ref"].removeprefix("refs/tags/")
    runs = api.get(f"repos/{{repo}}/actions/workflows/deploy.yml/runs?branch=main&event=workflow_run&status=success&per_page={DEPLOYS_TO_CHECK}")
    findings = []
    for run in runs.get("workflow_runs", []):
        if now - parse_time(run["created_at"]) > timedelta(days=DEPLOY_WINDOW_DAYS):
            continue
        jobs = api.get(f"repos/{{repo}}/actions/runs/{run['id']}/jobs")["jobs"]
        deployed = any(j["name"] == DEPLOY_JOB and j["conclusion"] == "success" for j in jobs)
        if deployed and run["head_sha"] not in tagged:
            findings.append(
                f"Production deploy of `{run['head_sha'][:7]}` (run {run['html_url']}) has no `prod-<version>` tag.")
    return findings


# ---------------------------------------------------------------- docs

def doc_paths(root: Path) -> list[Path]:
    files = [root / f for f in DOC_FILES if (root / f).exists()]
    for d in DOC_DIRS:
        files.extend(sorted((root / d).rglob("*.md")))
    return files


def check_docs(root: Path) -> list:
    findings = []
    for doc in doc_paths(root):
        text = doc.read_text(encoding="utf-8")
        rel_doc = doc.relative_to(root).as_posix()
        for target in LINK.findall(text):
            target = target.split("#", 1)[0]
            if not target or re.match(r"^[a-z]+:", target) or PLACEHOLDER.search(target):
                continue
            base = root if target.startswith("/") else doc.parent
            if not (base / target.lstrip("/")).exists():
                findings.append(f"`{rel_doc}` links to `{target}`, which does not exist.")
        for target in CODE_PATH.findall(text):
            if not target.startswith(PATH_ROOTS) or PLACEHOLDER.search(target):
                continue
            target = target.split("#", 1)[0].split(":", 1)[0].rstrip(".,")
            if not (root / target).exists():
                findings.append(f"`{rel_doc}` names `{target}`, which does not exist.")
    return sorted(set(findings))


# ---------------------------------------------------------------- main

def parse_time(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def load_expected_rulesets(root: Path) -> dict:
    return {p.stem: json.loads(p.read_text(encoding="utf-8")) for p in sorted((root / ".github/rulesets").glob("*.json"))}


def run_audit(api, root: Path, now: datetime) -> list:
    return (check_rulesets(api, load_expected_rulesets(root))
            + check_settings(api)
            + check_branches(api, now)
            + check_deploy_tags(api, now)
            + check_docs(root))


def report(api, findings: list, run_url: str) -> None:
    body = "The weekly repository audit found drift from the rules in `docs/contributing.md#enforced-rules`:\n\n"
    body += "\n".join(f"- {f}" for f in findings)
    body += f"\n\nAudit run: {run_url}\n\nFix the drift (or update the rule and its docs in a PR) and close this issue."
    open_issues = api.get_all("repos/{repo}/issues?state=open&labels=type:bug")
    existing = next((i for i in open_issues if i["title"] == ISSUE_TITLE), None)
    if existing:
        api.post(f"repos/{{repo}}/issues/{existing['number']}/comments", {"body": body})
        print(f"Commented on existing issue #{existing['number']}")
    else:
        api.post("repos/{repo}/issues", {"title": ISSUE_TITLE, "body": body, "labels": ISSUE_LABELS})
        print("Opened a drift issue")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true", help="print findings, do not open an issue")
    args = parser.parse_args()

    repo = os.environ["GITHUB_REPOSITORY"]
    root = Path(__file__).resolve().parents[2]
    api = Api(repo)
    findings = run_audit(api, root, datetime.now(timezone.utc))

    if not findings:
        print("No drift found.")
        return 0
    print("Drift found:")
    for f in findings:
        print(f"- {f}")
    if not args.dry_run:
        server = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
        run_url = f"{server}/{repo}/actions/runs/{os.environ.get('GITHUB_RUN_ID', '')}"
        report(api, findings, run_url)
    return 1


if __name__ == "__main__":
    sys.exit(main())
