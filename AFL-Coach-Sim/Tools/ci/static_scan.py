#!/usr/bin/env python3
import os, re, argparse, csv
from pathlib import Path

PATTERNS = {
    "expensive_call_in_update": re.compile(r"\b(GetComponent|FindObjectOfType|FindObjectsOfType|GameObject\.Find|Resources\.Load|Instantiate|Destroy)\b"),
    "linq_in_update": re.compile(r"\b(Select|Where|OrderBy|OrderByDescending|ToList|GroupBy|Distinct|Join)\s*\("),
    "debug_in_update": re.compile(r"\bDebug\.(Log|LogWarning|LogError)\s*\("),
    "empty_catch": re.compile(r"catch\s*\([^\)]*\)\s*\{(\s*//[^\n]*\n|\s*)\}"),
    "using_unityeditor": re.compile(r"^\s*using\s+UnityEditor\s*;", re.MULTILINE),
    "start_coroutine": re.compile(r"\bStartCoroutine\s*\("),
    "subscribe_event": re.compile(r"\+\="),
    "unsubscribe_event": re.compile(r"\-="),
    "async_void": re.compile(r"\basync\s+void\s+\w+\s*\("),
    "fixedupdate_physics_calls": re.compile(r"\b(AddForce|MovePosition|MoveRotation|velocity)\b"),
}

METHOD_START_RE = re.compile(r"\b(?:public|private|protected|internal)?\s*(?:async\s+)?(?:override\s+)?(?:static\s+)?void\s+(Update|FixedUpdate|LateUpdate)\s*\(", re.MULTILINE)

def find_method_ranges(text):
    lines = text.splitlines()
    text_len = len(text)
    ranges = {"Update": [], "FixedUpdate": [], "LateUpdate": []}
    for m in METHOD_START_RE.finditer(text):
        name = m.group(1)
        brace_pos = text.find("{", m.end())
        if brace_pos == -1:
            continue
        depth = 0
        i = brace_pos
        end_pos = None
        while i < text_len:
            ch = text[i]
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    end_pos = i
                    break
            i += 1
        if end_pos is None:
            continue
        start_line = text.count("\n", 0, brace_pos) + 1
        end_line = text.count("\n", 0, end_pos) + 1
        ranges[name].append((start_line, end_line))
    return ranges

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=str, default=".")
    parser.add_argument("--out", type=str, default="static_scan_report.csv")
    args = parser.parse_args()

    ROOT = Path(args.repo_root)
    cs_files = [p for p in ROOT.rglob("*.cs") if "Library" not in str(p) and "Temp" not in str(p)]

    findings = []
    for p in cs_files:
        text = p.read_text(errors="ignore")
        lines = text.splitlines()
        method_ranges = find_method_ranges(text)

        m = PATTERNS["using_unityeditor"].search(text)
        if m:
            findings.append({
                "file": str(p.relative_to(ROOT)),
                "line": text.count("\\n", 0, m.start()) + 1,
                "severity": "warn",
                "rule": "using-UnityEditor-in-runtime",
                "detail": "File imports UnityEditor; wrap editor-only code or move to Editor/."
            })
        for m in PATTERNS["empty_catch"].finditer(text):
            findings.append({
                "file": str(p.relative_to(ROOT)),
                "line": text.count("\\n", 0, m.start()) + 1,
                "severity": "warn",
                "rule": "empty-catch-block",
                "detail": "Empty catch block swallows exceptions."
            })
        for m in PATTERNS["async_void"].finditer(text):
            findings.append({
                "file": str(p.relative_to(ROOT)),
                "line": text.count("\\n", 0, m.start()) + 1,
                "severity": "warn",
                "rule": "async-void",
                "detail": "Avoid async void (except event handlers); prefer async Task."
            })
        if PATTERNS["subscribe_event"].search(text) and not PATTERNS["unsubscribe_event"].search(text):
            findings.append({
                "file": str(p.relative_to(ROOT)),
                "line": 1,
                "severity": "info",
                "rule": "event-unsubscribe-missing",
                "detail": "Subscriptions detected but no obvious unsubscription; ensure to unsubscribe in OnDisable/OnDestroy."
            })
        if PATTERNS["start_coroutine"].search(text):
            if "OnDisable" not in text and "OnDestroy" not in text and "StopCoroutine" not in text:
                findings.append({
                    "file": str(p.relative_to(ROOT)),
                    "line": 1,
                    "severity": "info",
                    "rule": "coroutine-stop-missing",
                    "detail": "StartCoroutine used without a stop path."
                })

        for method_name, ranges in method_ranges.items():
            for (s, e) in ranges:
                body = "\\n".join(lines[s-1:e])
                if PATTERNS["expensive_call_in_update"].search(body):
                    findings.append({
                        "file": str(p.relative_to(ROOT)),
                        "line": s,
                        "severity": "warn",
                        "rule": f"{method_name.lower()}-expensive-call",
                        "detail": f"Potentially expensive call inside {method_name}()."
                    })
                if PATTERNS["linq_in_update"].search(body):
                    findings.append({
                        "file": str(p.relative_to(ROOT)),
                        "line": s,
                        "severity": "info",
                        "rule": f"{method_name.lower()}-linq-allocation",
                        "detail": f"LINQ usage inside {method_name}() can allocate per-frame."
                    })
                if PATTERNS["debug_in_update"].search(body):
                    findings.append({
                        "file": str(p.relative_to(ROOT)),
                        "line": s,
                        "severity": "info",
                        "rule": f"{method_name.lower()}-debuglog",
                        "detail": f"Debug logging inside {method_name}() can be noisy."
                    })
                if method_name == "FixedUpdate" and PATTERNS["fixedupdate_physics_calls"].search(body):
                    findings.append({
                        "file": str(p.relative_to(ROOT)),
                        "line": s,
                        "severity": "ok",
                        "rule": "fixedupdate-physics",
                        "detail": "Physics calls in FixedUpdate (generally correct)."
                    })

    with open(args.out, "w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=["file","line","severity","rule","detail"])
        writer.writeheader()
        for row in findings:
            writer.writerow(row)

    print(f"Wrote {{len(findings)}} findings to {{args.out}}")

if __name__ == "__main__":
    main()
