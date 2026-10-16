#!/usr/bin/env python3
"""thunderstore.toml must say what package/manifest.json says (name, version, description,
website, dependencies), and the package must meet Thunderstore's upload rules, so a tag never
publishes a GitHub release that Thunderstore would refuse or describe differently."""
import json, re, struct, sys, tomllib

root = sys.argv[1] if len(sys.argv) > 1 else "."
m = json.load(open(f"{root}/package/manifest.json"))
t = tomllib.load(open(f"{root}/thunderstore.toml", "rb"))
p = t["package"]
errors = []

def same(what, a, b):
    if a != b:
        errors.append(f"{what}: manifest {a!r} vs thunderstore.toml {b!r}")

same("name", m["name"], p["name"])
same("version", m["version_number"], p["versionNumber"])
same("description", m["description"], p["description"])
same("website", m["website_url"], p["websiteUrl"])
same("dependencies", m["dependencies"], [f"{k}-{v}" for k, v in p["dependencies"].items()])

if not re.fullmatch(r"[A-Za-z0-9_]+", m["name"]): errors.append("name: only A-Z a-z 0-9 _")
if not re.fullmatch(r"\d+\.\d+\.\d+", m["version_number"]): errors.append("version: Major.Minor.Patch")
if len(m["description"]) > 250: errors.append(f"description: {len(m['description'])} > 250 chars")
for d in m["dependencies"]:
    if not re.fullmatch(r"[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+", d): errors.append(f"dependency {d!r}")
png = open(f"{root}/{t['build']['icon']}", "rb").read(24)
if png[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack(">II", png[16:24]) != (256, 256):
    errors.append("icon: must be a 256x256 PNG")
if "rounds" not in t["publish"]["communities"]: errors.append("community: rounds missing")

for e in errors: print("ts-check:", e)
print("ts-check:", "FAIL" if errors else "ok", m["name"], m["version_number"])
sys.exit(1 if errors else 0)
