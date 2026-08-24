#!/usr/bin/env python3
"""Validate an NUnit runtime-test result file.

Shared by the android / ios / wasm runtime-test scripts so all three fail the same way. Exits
non-zero when the file is unparseable, contains no test cases, or contains any case whose result is
neither Passed nor Skipped.

A run that produces *zero* cases is treated as a failure: an app that starts, writes an empty
document and exits looks identical to a green run to the publish task, and that is precisely how a
silently-broken filter or a crashed test host slips through.
"""
import os
import sys
import xml.etree.ElementTree as ET

results_path = os.environ.get("RUNTIME_TEST_RESULTS")
if not results_path:
    raise SystemExit("RUNTIME_TEST_RESULTS environment variable is required.")

try:
    tree = ET.parse(results_path)
except (ET.ParseError, OSError) as exc:
    raise SystemExit(f"Unable to read runtime test results from {results_path}: {exc}")

cases = tree.findall(".//test-case")
if not cases:
    raise SystemExit(f"Runtime tests produced no test cases in {results_path}.")

failed = [
    (case.get("name"), case.get("result"))
    for case in cases
    if case.get("result") and case.get("result") not in ("Passed", "Skipped")
]

print(f"Validated {len(cases)} runtime test cases from {results_path}.")

if failed:
    print(f"Failed/inconclusive tests: {len(failed)}")
    for name, result in failed:
        print(f" - {name}: {result}")
    sys.exit(1)
