# Notes

Running state and evidence. Newest first.

## 2026-10-15 — M1 slice 1: couch core + mod-source research

- `src/Couch/` (CouchLayout, CouchPlan, LaneCursor): pure logic, no Unity types, not wired into
  the pick flow yet. `tests/CouchTests` (net8 console, 655 checks) runs in `tools/check.sh`.
  Negative control: queue threshold 2→1 and dropping repeat steps made 7 checks fail; restored.
- check.sh locally: tests 655/0, refcheck Release 77/77, Debug 78/78, 0 unproven.
  CI run 38676841252 on fd474be green; log shows the same counts.
- Research from public mod sources (docs/research/mods.md, conclusions in docs/COUCH.md):
  hand size = `CardChoice.children` length; slots placed through `ScreenToWorldPoint` z -5;
  the spawn-time picker is `pickrID` + `pickerType` + `spawnedCards` (duplicate checks), so all
  three must be swapped per lane; RWF already makes `ApplyCardStats.Pick` per local player;
  `CardChoiceVisuals` is single, so lanes draw their own highlight. Jump vs Fire stays open
  (no public mod patches `CardChoice.Update`).
- Next slice: `CouchBoard` MonoBehaviour behind a `CouchMode` config (default off until a
  hardware test), using the core: spawn per lane with swapped CardChoice state, per-lane capture
  in `Patch_Pick_Capture`, `ReportSession` per lane. New game members need refcheck witnesses.
- No hardware run; nothing here has been in the game.

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
