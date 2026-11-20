"""Check the specification or run a fresh, CA-tagged acceptance suite."""
import argparse
from collections import Counter, defaultdict
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
ID = r"(?:RF|RNF|CA|RH|AR|ADR|RN)-N?\d+[a-z]?"
CA = r"CA-N?\d+[a-z]+"
DEFINITIONS = (
    rf"^\*\*({ID}) ", rf"^\| ({ID}) \|", rf"^### ({ID}) ",
    rf"\*\*({CA})\*\*",
)


def read_contract(language, errors):
    folder = ROOT / "docs" / language
    requirements = (folder / "requirements.md").read_text()
    architecture = (folder / "architecture.md").read_text()
    sources = [folder / "requirements.md", folder / "architecture.md", folder / "README.md",
               *sorted((folder / "design/diagrams").glob("*.md"))]
    for source in sources:
        for target in re.findall(r"\]\(([^()\s]+)\)", source.read_text()):
            path = target.split("#", 1)[0]
            if path and "://" not in path and not (source.parent / path).exists():
                errors.append(f"{language}: broken link {target} in {source.name}")
    combined = requirements + "\n" + architecture
    identifiers = Counter(item for regex in DEFINITIONS
                          for item in re.findall(regex, combined, re.M))
    for item, count in identifiers.items():
        if count != 1:
            errors.append(f"{language}: {item} has {count} definitions")
    for item in sorted(set(re.findall(rf"\b{ID}\b", combined)) - identifiers.keys()):
        errors.append(f"{language}: unresolved reference {item}")
    versions = re.findall(r"^\| (?:Version|Versión) \| ([\d.]+) \|", combined, re.M)
    versions += re.findall(r"\]\(requirements\.md\) v([\d.]+)", architecture)
    if len(versions) != 3 or len(set(versions)) != 1:
        errors.append(f"{language}: inconsistent document versions {versions}")
    catalog = {}
    for row in requirements.splitlines():
        if row.startswith("| `"):
            fields = row.split("|")
            operation = fields[1].strip().strip("`")
            criteria = re.findall(CA, fields[-2])
            if not criteria or operation in catalog:
                errors.append(f"{language}: invalid operation mapping {operation}")
            offline = fields[-2].strip().startswith("offline ")
            catalog[operation] = (offline, re.findall(r"`([^`]+)`", "|".join(fields[2:-2])), criteria)
    live = {}
    for criterion, body in re.findall(rf"\*\*({CA})\*\* \[LIVE\]([^\n]*)", requirements):
        declaration = re.match(r" \[evidence: ([a-z0-9_, ]+)\]", body)
        keys = tuple(key.strip() for key in declaration[1].split(",")) if declaration else ()
        if (not keys or len(keys) != len(set(keys))
                or any(not re.fullmatch(r"[a-z][a-z0-9_]*", key) for key in keys)):
            errors.append(f"{language}: {criterion} requires unique evidence keys")
        live[criterion] = keys
    return set(identifiers), versions, catalog, live


def contract(scope):
    errors = []
    en = read_contract("en", errors)
    es = read_contract("es", errors)
    if en != es:
        errors.append("English and Spanish definitions, versions, catalog or live criteria differ")
    ids, versions, catalog, live = en
    if not versions or scope["spec"] != f"docs/en/requirements.md {versions[0]}":
        errors.append("scope.spec does not match requirements")
    selected = scope["committed"] + scope["support"]
    if len(selected) != len(set(selected)):
        errors.append("duplicate or overlapping scope entries")
    for item in selected:
        if not re.fullmatch(CA, item) or item not in ids:
            errors.append(f"undefined scope criterion {item}")
    if not catalog or not live or not live.keys() <= set(scope["committed"]):
        errors.append("expected catalog and committed live criteria")
    for operation, (offline, _, criteria) in catalog.items():
        associated = set(criteria)
        if associated - set(scope["committed"]) or not associated - live.keys():
            errors.append(f"{operation}: expected committed local criteria")
        if bool(associated & live.keys()) == offline:
            errors.append(f"{operation}: live criteria must match the operation evidence profile")
    return errors, live


def tagged_methods():
    methods = set()
    pattern = re.compile(
        rf'\[Ac\("({CA})"\)\]\s*(?:\[[^\]]+\]\s*)*'
        r'(?:public|internal)\s+(?:[\w.<>]+\s+)+(CA_\w+)\s*\('
    )
    for source in (ROOT / "tests/Acceptance").rglob("*.cs"):
        methods.update(pattern.findall(source.read_text()))
    return methods


def evaluate(trx, required, methods, live=False):
    root = ET.parse(trx).getroot()
    outcomes = defaultdict(list)
    errors = []
    blocked = []
    for result in root.findall(".//{*}UnitTestResult"):
        name = result.get("testName", "")
        match = re.search(r"\b(CA_(N?\d+[a-z]+)_\w+)", name)
        outcome = result.get("outcome")
        if not match:
            errors.append("test name missing CA identifier")
            continue
        criterion = "CA-" + match[2]
        if (criterion, match[1]) not in methods:
            errors.append(f"{criterion}: method requires matching Ac attribute")
            continue
        outcomes[criterion].append(outcome)
        message = result.findtext(".//{*}ErrorInfo/{*}Message", "")
        if live and outcome in ("NotExecuted", "Inconclusive") and message.strip() == "provider_unavailable":
            blocked.append(criterion)
        elif outcome != "Passed":
            errors.append(f"{criterion}: expected Passed, observed {outcome}")
    covered = required.intersection(outcomes)
    passed = sorted(item for item in covered if all(x == "Passed" for x in outcomes[item]))
    errors.extend(f"{item}: missing result" for item in sorted(required - covered))
    return {"covered": len(covered), "passed": len(passed), "passed_ids": passed,
            "blocked_ids": sorted(set(blocked)), "errors": errors}


def live_evidence(path, references):
    """Read only the independent oracle's allowlisted, redacted observations."""
    try:
        evidence = json.loads(path.read_text())
    except (OSError, ValueError):
        return None
    if not isinstance(evidence, dict):
        return None
    if any(not isinstance(evidence.get(key), str) or not re.fullmatch(r"[0-9a-f]{64}", evidence[key])
           for key in references):
        return None
    return {key: evidence[key] for key in references}


def suite(name, required, live_keys):
    report = {"suite": name, "required": len(required), "covered": 0, "passed": 0,
              "status": "blocked"}
    projects = list((ROOT / "tests/Acceptance").glob("*.csproj"))
    if len(projects) != 1:
        return report | {"reason": "expected one tests/Acceptance project", "found": len(projects)}
    if not shutil.which("dotnet"):
        return report | {"reason": "dotnet runner unavailable"}
    if name == "live" and os.getenv("RUN_LIVE_ACCEPTANCE") != "1":
        return report | {"reason": "dedicated real accounts and RUN_LIVE_ACCEPTANCE=1 required"}
    with tempfile.TemporaryDirectory() as folder:
        command = ["dotnet", "test", str(projects[0]), "--filter", f"TestCategory={name}",
                   "--results-directory", folder, "--logger", "trx;LogFileName=results.trx"]
        evidence_dir = Path(folder) / "evidence"
        environment = dict(os.environ)
        if name == "live":
            evidence_dir.mkdir()
            environment["LIVE_ACCEPTANCE_EVIDENCE"] = str(evidence_dir)
        start = time.monotonic()
        try:
            run = subprocess.run(command, cwd=ROOT, env=environment, stdout=subprocess.DEVNULL,
                                 stderr=subprocess.DEVNULL, timeout=600 if name == "local" else 900)
        except subprocess.TimeoutExpired:
            return report | {"status": "failed", "reason": "suite deadline exceeded"}
        report["seconds"] = round(time.monotonic() - start, 3)
        trx = Path(folder) / "results.trx"
        if not trx.exists():
            return report | {"status": "failed", "reason": "runner produced no TRX"}
        try:
            report.update(evaluate(trx, required, tagged_methods(), live=name == "live"))
        except ET.ParseError:
            return report | {"status": "failed", "reason": "invalid TRX"}
        if name == "live":
            report["provider_references"] = {}
            for criterion in list(report["passed_ids"]):
                references = live_evidence(evidence_dir / f"{criterion}.json", live_keys[criterion])
                if references is None:
                    report["errors"].append(f"{criterion}: missing redacted provider references")
                    report["passed_ids"].remove(criterion)
                    report["passed"] -= 1
                else:
                    report["provider_references"][criterion] = references
        if report["errors"] or run.returncode:
            report["status"] = "failed"
        elif report["blocked_ids"]:
            report["status"] = "blocked"
        else:
            report["status"] = "passed"
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("gate", choices=["contract", "local", "live", "release"])
    gate = parser.parse_args().gate
    scope = json.loads((ROOT / "harness/scope.json").read_text())
    errors, live = contract(scope)
    git = ["git", "-C", str(ROOT)]
    revision = subprocess.check_output(git + ["rev-parse", "HEAD"], text=True).strip()
    dirty = bool(subprocess.check_output(git + ["status", "--porcelain"]))
    report = {"spec": scope["spec"], "base": revision, "dirty": dirty,
              "contract_errors": errors, "suites": []}
    if not errors:
        for name, required in [("local", set(scope["committed"]) - live.keys()), ("live", set(live))]:
            if gate in (name, "release"):
                report["suites"].append(suite(name, required, live))
    statuses = [row["status"] for row in report["suites"]]
    status = "failed" if errors or "failed" in statuses else (
        "blocked" if "blocked" in statuses else "passed")
    print(json.dumps(report | {"status": status}, indent=2))
    return {"passed": 0, "failed": 1, "blocked": 2}[status]


if __name__ == "__main__":
    raise SystemExit(main())
