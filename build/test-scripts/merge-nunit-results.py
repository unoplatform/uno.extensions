#!/usr/bin/env python3
"""Merge NUnit runtime-test documents: merge-nunit-results.py <output> <input>...

The mobile scripts launch the app once per filter (TEST_FILTERS), each launch writing its own
document. The first one is kept as the base: the test-suites of the others are appended under its
test-run, whose counters are recomputed from the merged test cases.
"""
import sys
import xml.etree.ElementTree as ET

output, *inputs = sys.argv[1:]
if not inputs:
    raise SystemExit("usage: merge-nunit-results.py <output> <input>...")

tree = ET.parse(inputs[0])
run = tree.getroot()
for path in inputs[1:]:
    run.extend(ET.parse(path).getroot())

results = [case.get("result") for case in run.iter("test-case")]
counts = {
    "passed": results.count("Passed"),
    "failed": results.count("Failed"),
    "skipped": results.count("Skipped"),
}
run.set("total", str(len(results)))
run.set("testcasecount", str(len(results)))
for name, count in counts.items():
    run.set(name, str(count))
run.set("result", "Failed" if counts["failed"] else "Passed")

tree.write(output, encoding="utf-8", xml_declaration=True)
print(f"Merged {len(inputs)} result document(s), {len(results)} test cases, into {output}.")
