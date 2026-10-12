# simulpicks-couch: brief

JARVIS's fork of SimulPicks (RedRedRain/EveryonePicks, MIT), a ROUNDS mod in which everyone
picks their card at the same time. SimulPicks only parallelises across machines: on one PC or
Steam Deck every local player is still queued and picks one after another through the game's
single `CardChoice.instance` (`State.RunnerLoop`). **Goal: couch mode — several local players
on one machine pick at the same time**, each on their own part of the screen with their own
controller. Online mixes keep working: each machine runs its local board, SimulPicks syncs
between machines as it does today.

Requested by Jan (2026-10-12). JARVIS owns the repo: README, issues, PRs, releases. Jan is a
user, and real tests need his hardware.

## Rules

- **No game code in the repo, ever.** ROUNDS' Assembly-CSharp and other game DLLs are not
  committed, uploaded, or downloaded by CI. CI builds against the reference stubs in `refs/`
  (read `refs/README.md` before touching them) and `tools/refcheck` proves every member used.
- **Do not claim hardware results you do not have.** A CI build is not a tested mod. Anything
  that only ran in CI says so. Hardware tests need ROUNDS + 2–4 controllers on Jan's laptop or
  Deck; write exactly what Jan must do.
- **Upstream etiquette.** No issues or PRs at RedRedRain/EveryonePicks before a working,
  hardware-tested prototype. Then propose an issue "Couch mode: local players pick in parallel"
  and ask whether a PR is welcome. Keep the diff upstreamable: couch mode is additive and off
  the code path when there is at most one local picker.
- **Releases only** reach Jan (lane notify=releases). Progress, decisions and problems go to
  NOTES.md, issues or PRs.
- Target `netstandard2.0` (the game's old Mono). No personal data in any fault report.
- The report-API URL is secret: never in this public repo.

## Layout

- `src/` the mod (upstream code plus couch mode). `State.cs` is the pick orchestration,
  `Patches.cs` the CardChoice interceptions.
- `refs/` reference stubs, `tools/` fetch-deps, check, refcheck. `diag/` upstream's dev
  diagnostics plugin (not built in CI).
- `ROADMAP.md` the plan, `NOTES.md` running state and evidence. Read NOTES before working.

## Reverse engineering

REA (`rea inspect-managed-members`, static .NET metadata + IL, no decompiler) maps which mod code
touches a game member; `tools/rea-scan.py` queries its output. Read `docs/research/rea.md` first:
what it can and cannot do, and findings so far. Output stays outside the repo; only findings in
our own words get committed. It is not a stub checker — that stays `tools/refcheck`.

## Build

`tools/check.sh` (needs a .NET 8 SDK; no game). On a machine with ROUNDS the csproj uses the
real game automatically (see README); `tools/check.sh <Managed dir>` checks against it.
