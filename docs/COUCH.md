# Couch mode: design (M1)

Status: implemented (`src/CouchBoard.cs` on the pure core below), released as prerelease v0.4.0.
Nothing here has run in the game yet; the hardware test is in NOTES.md.

## Where SimulPicks serialises local players

`State.SeedPickers` enqueues every picker for which `IsLocalPlayer` is true; offline that is
every player. `RunnerLoop` then drains `localQueue` one entry at a time, each through
`RunLocalNativePick`: `CardChoiceVisuals.Show`, `CardChoice.StartPick(1, playerID)`, wait while
`IsPicking`, scrub, `ReportSession`. One `CardChoice.instance` exists, so the native flow can only
serve one picker. Everything downstream — results, barrier, apply-set, reveal — is already
per-player and does not care how a session ran.

## The local board

When the runner would start and **two or more distinct local players** are queued, and couch
mode is enabled (config, default on), run `CouchBoard` instead of the native session for those
players. A single queued local player keeps the native flow unchanged, so online play with one
player per machine is untouched.

Per player the board holds a **lane**: region rect, spawned cards, selected index, picks owed,
picks produced. The board as a whole sets `localPickActive` (so the existing spawn/RPC
redirection keeps cards local), and the per-session globals become per-lane:

- `currentSessionPlayer` → the capture patch (`Patch_Pick_Capture`) looks up the lane by
  `pickerID` instead of comparing with one global.
- `currentSessionPicks` → per lane; each lane calls `ReportSession` itself when it finishes, so a
  fast player's results are broadcast while others still pick.

### Layout

2 lanes → two full-width rows; 3–4 → 2×2; 5–9 → 3×3. Lane order = player order, so a player's
region stays in the same place across rounds. Region gets a translucent tint in
`PlayerManager.GetColorFromPlayer(id).winText`'s colour and the player's name. Cards are laid
out in one row centred in the region and scaled to fit (about 50–60% in 2×2).

### Spawning

For each card: set `CardChoice.instance.pickrID` (and `pickerType`) to the lane's player, then
call the game's `SpawnUniqueCard(pos, rot)` so the vanilla rarity roll, duplicate rules and
`CardChoiceSpawnUniqueCardPatch`'s class/blacklist logic all see the right picker. Restore the
fields afterwards. Card count per lane = the game's normal hand size (read from `CardChoice`,
which mods like PickPhaseImprovements change).

### Input

Each lane reads only `player.data.playerActions` (InControl): move X with a repeat delay for
left/right, Jump/confirm to pick. That is the device the player fights with, so no new binding
is needed. A keyboard player works the same way through their keyboard `PlayerActions`.

### Picking

On confirm: mark selected visuals, then apply the card for that player the way the vanilla
pick does (`ApplyCardStats.Pick(pickerID, forcePick, PickerType.Player)` on the spawned card),
which `Patch_Pick_Capture` records into the lane. Destroy the lane's cards. If `picks` grew (a
card granted an extra pick → a new `DoPick` → `EnqueueLocalPick`), the board picks that entry up
and gives the lane a fresh row; otherwise the lane is done: `ReportSession`, show "picked".

Timeouts reuse `PickSeconds` per pick: on expiry the lane's selected card is picked.

### Fallback

Some card mods open their own `CardChoice` session mid-pick (assuming one picker). If
`CardChoice.instance.IsPicking` becomes true while the board runs, or a queued entry carries a
non-`Player` `PickerType`, the board finishes the lanes it has and hands the rest back to the
sequential runner. Logged as a fallback, not an error.

### Online

Nothing changes on the wire. Each machine's board covers only its own local players; status,
results and session-done broadcasts are per player exactly as today.

## Runtime as built (v0.4.0)

Where the implementation settled what the design above left open:

- `State.RunnerLoop` asks `TakeCouchBoard` before each native session: `CouchPlan.Split` over the
  queue entries; the board's players' first entries leave the queue, the rest keeps its order.
  `CouchBoard.Ready()` (reflection targets, `CardChoice`, `MainCam`) failing keeps it native.
- **One lane session per queue entry**, as the native runner does: each pick is its own
  `ReportSession`. An extra pick (a new `DoPick` → `EnqueueLocalPick`) is taken off the queue by
  the board while it runs and becomes a fresh hand in that player's region; one arriving after the
  board ended is picked up by the runner as usual.
- Spawning swaps `pickrID`, `pickerType` and `spawnedCards` (a list per lane) around each
  `SpawnUniqueCard` call (reflection), positions through `MainCam.cam.ScreenToWorldPoint`, z = -5,
  and multiplies the card's own scale by the layout scale × `CouchCardScale`.
- Selection: `CardVisuals.ChangeSelected(bool)` per card (WillsWackyManagers calls it directly)
  plus a colour bar under the selected card drawn in `OnGUI`, with the region frame, name and
  countdown. No fill over the region, so cards keep their colours.
- Confirm: `ApplyCardStats.Pick(playerID, false, Player)` on the chosen card (reflection), the
  call vanilla `CardChoice.Pick` makes; `Patch_Pick_Capture` files it into the lane via
  `CouchBoard.CaptureFor`. The other cards go at once, the picked one after 0.35 s, then report.
  `CardChoice.IsPicking` stays false the whole time, so vanilla `Update` never reads input.
- Input ignores the first 0.4 s of a hand (jump held over from play). Timeout = `PickSeconds` per
  pick, then the selected card is taken.
- Barrier cut: `FlushRunner` → `CouchBoard.Flush` reports each open lane's picks so far; hard
  reset → `Abort`.
- Game members it uses that refcheck proves: `PlayerActions.Jump/Move`, InControl
  `OneAxisInputControl.WasPressed`, `TwoAxisInputControl.X`, `MainCam.instance/cam`,
  `CardVisuals.ChangeSelected`. Reflection only (MMHOOK proves they exist, not that they are
  public): `SpawnUniqueCard`, `ApplyCardStats.Pick`, `children`.

### Mod packs (v0.5.0)

- `Ready()` also declines when `CardChoice.IsPicking` is already true (a mod's own pick is open)
  or a loaded plugin GUID / Harmony owner is listed in `Couch.SequentialWith` (`ModGate`, exact,
  case-insensitive). Declining means that round's local picks run sequentially as in SimulPicks.
- While the board runs, `IsPicking` turning true means a mod called `StartPick` itself; the lanes
  freeze (cards inactive, deadlines and settle pushed, input grace renewed) until it is false.
- Every lane step runs guarded: an exception (typically another mod's spawn or pick hook) ends
  that lane's pick through `Finish`, reporting its picks so far; a board-level throw reports all
  open lanes. Setup gives every taken player a lane even if name/colour lookup fails.
- `Confirm` swaps `pickrID`/`pickerType` around `ApplyCardStats.Pick`, so pick hooks that read the
  picker from `CardChoice` (PickPhaseShenanigans, ModdingUtils `PickingPlayer`) see the lane.
- First board of a session logs the other Harmony owners patching `CardChoice`/`ApplyCardStats`.

## Pure core (implemented, unit-tested)

`src/Couch/` holds the parts with no Unity or game types, tested by `tests/CouchTests` (run by
`tools/check.sh` and CI). Used by `src/CouchBoard.cs`.

- `CouchLayout`: grid per lane count, lane regions in normalised screen space (row-major, player
  order), one centred card row per region with a name strip, and the scale relative to a
  full-screen hand (2×2, hand of five ≈ 0.5). Normalised coordinates convert to world space the
  way PickNCards places its slots (`ScreenToWorldPoint`, z = -5), see research below.
- `CouchPlan.Split`: which queued entries the board takes (first entry of each distinct
  `PickerType.Player` player, up to nine) and which stay sequential; fewer than two → native.
- `LaneCursor`: per-lane selection from Move X: deadzone 0.5, move on push, repeat after
  0.35 s every 0.12 s, wraps; a long frame delivers every owed repeat.

## Findings from public mod sources (2026-10-15)

Mod code only, never game code; links pinned to commits in `docs/research/mods.md`.

- **Hand size** is the length of the private `Transform[] children` on `CardChoice`, the slot
  transforms under the CardChoice GameObject (UnboundLib fills it in `Start`; PickNCards
  resizes it in a `StartPick` prefix by cloning `children[0]`). The board reads that length.
- **Positions** are those slot transforms in world space; PickNCards computes them in normalised
  screen units through the main camera's `ScreenToWorldPoint` at z = -5 and sets scale in a
  `Spawn` postfix (vanilla does not sync card scale). That answers the camera question: lay out
  in normalised space, convert through `MainCam`.
- **The picker is global state at spawn time.** CardChoiceSpawnUniqueCardPatch reads
  `pickerType` and `pickrID` (and indexes `PlayerManager.players` by `pickrID`) and checks
  duplicates against `spawnedCards`; ModdingUtils' `PickingPlayer` helper does the same. So per
  card spawn the board must swap in the lane's `pickrID`, `pickerType` **and `spawnedCards`**
  list, then restore — not just `pickrID` as planned above.
- **Applying a pick**: vanilla-like `CardChoice.Pick(GameObject, bool)` calls
  `ApplyCardStats.Pick(pickrID, false, pickerType)` and then `RPCA_DoEndPick(int[], int, int, int)`;
  every spawned card carries a `PublicInt.theInt` slot index. RoundsWithFriends transpiles
  `ApplyCardStats.Pick` to key on a per-player id instead of the Photon actor, so several local
  players on one actor already work there; the board must call `Pick` per card, not
  bypass it. ModdingUtils' `AddCardToPlayer` applies cards with no `CardChoice` state at all
  (private `ApplyStats` + `SilentAddToCardBar`), a fallback if `Pick` needs session state.
- **`CardChoiceVisuals`** (the hands) is one shared instance following one selected card; the
  board cannot use it per lane and draws its own selection highlight instead.

## Open questions (need the game to answer)

- Which `PlayerActions` member the vanilla picker uses to confirm (Jump vs. Fire). No public mod
  patches `CardChoice.Update`, so mod sources cannot answer it. Not blocking: the board uses
  Jump/Move like RoundsWithFriends' local character select (docs/research/rea.md).
- Mods that read `CardChoice.instance.pickrID` as "the" picker outside a spawn
  (PickPhaseShenanigans' `pickrID && IsPicking` check) while several players pick at once — M2.
- Whether `ApplyCardStats.Pick` outside a native session needs `IsPicking` set, and whether card
  animations (`CardVisuals`, `RPCA_ChangeSelected`) behave at ~0.5 scale. Hardware test.
- Member names may differ on current game builds (1.1.2 renamed some); the real-DLL
  `tools/check.sh <Managed dir>` run settles each one the board uses.
