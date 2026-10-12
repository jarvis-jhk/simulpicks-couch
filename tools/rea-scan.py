#!/usr/bin/env python3
"""Query REA's managed-member evidence for methods that touch given members.

REA (github.com/morluto/rea) leaves member-ref names out of its field accesses, so this joins
them back through member_refs before matching. Input is the JSON of

    rea inspect-managed-members <dll> --format json > x.json

  tools/rea-scan.py x.json '<regex over member or method names>'

Prints each matching method with the members it calls or reads, in IL order.
See docs/research/rea.md for what REA can and cannot tell us here.
"""
import collections
import json
import re
import sys

if len(sys.argv) != 3:
    sys.exit(__doc__)
result = json.load(open(sys.argv[1]))["normalized_result"]
pattern = re.compile(sys.argv[2])
refs = {m["token"]: m["name"] for m in result["member_refs"]}
used = collections.defaultdict(list)
for e in result["call_edges"]:
    used[e["caller"]].append(e["target_name"] or refs.get(e["target_token"]))
for a in result["field_accesses"]:
    used[a["method"]].append(a["field_name"] or refs.get(a["field_token"]))
for method, names in used.items():
    if pattern.search(method) or any(n and pattern.search(n) for n in names):
        print(method, "|", " ".join(dict.fromkeys(n for n in names if n and not n.startswith("<"))))
