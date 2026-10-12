# Notes

Running state and evidence. Newest first.

## 2026-10-12 — REA

- REA (rea-agents 6.4.0, installed in JARVIS's toolchain) runs on all witness DLLs except
  MMHOOK (fixed 64 MiB metadata budget). Usage, limits and findings: docs/research/rea.md.
- Finding: RoundsWithFriends' local character select reads each player's own
  `data.playerActions.Jump`/`Move` and builds per-device action sets — the COUCH input question
  is no longer blocking M1. NullManager, ModdingUtils and PickPhaseShenanigans read
  `CardChoice.instance.pickrID` as the single picker: M2 compatibility risk.
- Not usable as a stub checker (build-local type coordinates, unresolved TypeRefs); refcheck stays.

## 2026-10-12 — fork set up, M0

- Fork of RedRedRain/EveryonePicks (full history, upstream 0.3.37 dev), remote `upstream`.
- M0: builds with no game installed. `refs/` stubs + `tools/refcheck`; locally Release
  77/77 and Debug 78/78 stubbed references witnessed, 0 attested, 0 unproven. Refcheck caught
  two wrong stubs before they shipped (Player.IsLocal field, LocalCleanPhotonView → bool);
  that is the negative control that the check bites.
- A Release DLL built against stubs has not been loaded into the game yet. Until a
  `tools/check.sh <Managed dir>` run on a real install, treat it as unverified.
- `diag/` (upstream's dev diagnostics plugin) is not built in CI.
- CI run 38292012929 on 74efeeb: green (Release+Debug build, refcheck 0 unproven, DLL artifact).
- ROUNDS is NOT on the laptop: no ~/.local/share/Steam there (gateway listing, approved by Jan,
  2026-10-12). Hardware tests and the authoritative real-DLL refcheck need the Deck (not a gateway
  target) or a Steam install; ask Jan when the prototype is ready.
- Weekly work session schedule e0884068 (Thu 03:30 UTC).
- Next: M1 per docs/COUCH.md. The open questions in it need the game's CardChoice code.
