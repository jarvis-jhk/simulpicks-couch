# REA in this repo

[REA](https://github.com/morluto/rea) (npm `rea-agents`, MIT) is a reverse-engineering CLI.
For a .NET mod the useful part is its static managed provider (`rea-dotnet-static`): it reads
PE/CLI metadata and IL without loading the assembly, and reports types, members, signatures,
call edges and field accesses with evidence (artifact sha256, MVID, metadata tokens).
It does **not** decompile to C#.

Version used: rea-agents 6.4.0 (2026-10-12). Only the genuine package; lookalike repos exist.

## Usage

```sh
rea inspect-managed-members deps/witness/RoundsWithFriends-RoundsWithFriends.dll --format json > /tmp/rwf.json
tools/rea-scan.py /tmp/rwf.json '^playerActions$'     # which methods touch playerActions, and what else
```

`tools/rea-scan.py` joins member-ref names back into REA's call edges and field accesses (REA
leaves external field names null) and prints every matching method with the members it uses.
Write REA output outside the repo. Never commit decompiled or extracted game code; record
findings in our own words with the artifact and REA version as evidence.

## What it can and cannot do here

- Works on every witness in `deps/witness/` except `MMHOOK.dll` (2.6 MB, generated hooks): REA
  stops at a fixed 64 MiB metadata representation budget ("resource-limit"). MMHOOK is covered
  by `tools/refcheck` instead.
- Not a stub checker. REA names external types only by build-local coordinates (`class:101`),
  and leaves TypeRef parents unresolved, so a member signature cannot be matched across two
  assemblies. Tried on 2026-10-12: 119 of our 485 member refs had no witness ref with the same
  name and coordinate signature, all of them artefacts of that (e.g. `get_transform`). Stubs stay
  proven by `tools/refcheck`, and authoritatively by `tools/check.sh <Managed dir>`.
- Its real use: "which mod code touches member X, and what else does that code use?" across
  witness mods built against the real game — a map of how others solved the same problem.
- Assembly-CSharp itself is not here (no game in this container). With the game on a machine,
  the same command on `Rounds_Data/Managed/Assembly-CSharp.dll` answers questions about vanilla
  code directly (e.g. what `CardChoice.Update` reads). Run it locally only; nothing from it is
  committed beyond findings in our words.

## Findings

### 2026-10-12 — how local players read their own controller (M1 input)

Question from `docs/COUCH.md`: which `PlayerActions` members to use for per-player navigation
and confirm on the couch board.

Evidence, REA 6.4.0 on RoundsWithFriends 2.2.2 (`RoundsWithFriends.dll` from the zip pinned in
`fetch-deps.sh`), a mod built against the real game that already runs a multi-local-player
UI (character select):

- `RWF.Patches.CharacterSelectionInstance_Patch_Update.Prefix` reads per player
  `currentPlayer.data.playerActions` and uses its `Jump` (`WasPressed`) for ready/confirm and its
  `Move` (`get_X`, with `Abs` and `deltaTime` for a deadzone and repeat) for left/right; it also
  checks `data.input.inputType` and the action set's `Device` (`CommandWasPressed`).
- `<CreatePlayer>d__0.MoveNext` creates each local player's action set with
  `PlayerActions.CreateWithControllerBindings()` or `CreateWithKeyboardBindings()` and assigns
  `Device` from `inputDevice`, so every local player's `playerActions` is bound to only their own
  device (or keyboard).

Conclusion: the board design (Move X with repeat, Jump to confirm, from each player's own
`data.playerActions`) is the pattern a widely used mod already relies on for parallel local
input, so M1 does not need the vanilla picker's exact choice. Still open: whether vanilla
`CardChoice.Update` uses Jump or Fire — only matters if we want identical feel, needs the game.

Other witness hits for `pickrID`/`IsPicking`: NullManager, ModdingUtils
(`CardChoicePatchGetRanomCard.PickingPlayer`) and PickPhaseShenanigans
(`DisablePickingPlayer`) all read `CardChoice.instance.pickrID` to find "the" picking player.
Under couch mode several players pick at once while `pickrID` names one, so these mods are
compatibility risks for M2: they must see the right picker per spawned card.
