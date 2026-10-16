# Roadmap

## M0 — builds in CI without the game ✅ (2026-10-12)

- Reference stubs for the game assemblies (`refs/`), dependencies from Thunderstore/NuGet pinned
  by hash (`tools/fetch-deps.sh`), `tools/refcheck` proving every member against witnesses.
- GitHub Actions: Release + Debug build, refcheck, DLL artifact.
- Open: one authoritative `tools/check.sh <Managed dir>` run against a real ROUNDS install.

## M1 — 2×2 couch prototype — built, prerelease v0.4.0 (2026-10-16), awaiting Jan's game test

Design in `docs/COUCH.md`. A local pick board replaces the sequential `RunnerLoop` when two or
more local players owe a pick at the same time:

- Screen split: 2 players → two rows, 3–4 → 2×2, 5+ → 3×3. Each region tinted and labelled
  with its player's colour and name, cards scaled to fit.
- Cards spawned through the game's `CardChoice.SpawnUniqueCard` with that player as picker, so
  rarity, class blacklists and `CardChoiceSpawnUniqueCardPatch` behave as in a normal pick.
- Per-region input from that player's own `playerActions` (move left/right, confirm).
- Apply through SimulPicks' existing host-authoritative path; barrier and waiting display reused.
- Done when: on Jan's machine with 2–4 controllers, an offline local match of 4 players picks
  in parallel for a whole game without desync or stuck phases.

## M2 — robust

- Extra picks (devil cards, Pick N Cards, MorePicksPatch) add another row in that player's region.
- ✅ v0.5.0 (CI only): fallback for card mods that open their own `CardChoice` (board waits /
  takes turns, logged), `Couch.SequentialWith` opt-out, pick hooks logged, throwing mod hooks
  end one lane's pick instead of the round, picker swapped for pick hooks. Game test by Jan.
- Common mod packs: ClassesManagerReborn, WillsWackyManagers, NullManager, RoundsWithFriends
  with online + couch mixes.
- Fault relay: unhandled exceptions in the couch board reported anonymously (no names/IDs).
- Keyboard player + controllers mixed; a controller disconnecting mid-pick.

## M3 — release

- GitHub release with the DLL and a Thunderstore-ready zip; own package name so it does not
  collide with SimulPicks on Thunderstore. README with install steps for Windows and Deck.
- Then the upstream issue "Couch mode: local players pick in parallel", and a PR if wanted.
