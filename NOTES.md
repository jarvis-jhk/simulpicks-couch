# Notes

Running state and evidence. Newest first.

## 2026-10-12 — fork set up, M0

- Fork of RedRedRain/EveryonePicks (full history, upstream 0.3.37 dev), remote `upstream`.
- M0: builds with no game installed. `refs/` stubs + `tools/refcheck`; locally Release
  77/77 and Debug 78/78 stubbed references witnessed, 0 attested, 0 unproven. Refcheck caught
  two wrong stubs before they shipped (Player.IsLocal field, LocalCleanPhotonView → bool);
  that is the negative control that the check bites.
- A Release DLL built against stubs has not been loaded into the game yet. Until a
  `tools/check.sh <Managed dir>` run on a real install, treat it as unverified.
- `diag/` (upstream's dev diagnostics plugin) is not built in CI.
- Asked the laptop (gateway) whether ROUNDS is installed there; answer pending.
- Next: M1 per docs/COUCH.md. The open questions in it need the game's CardChoice code.
