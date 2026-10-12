# Couch mode: design (M1)

Status: design only. Nothing here has run in the game yet.

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

## Open questions (need the game to answer)

- Which `PlayerActions` member the vanilla picker uses to confirm (Jump vs. Fire) and how
  `CardChoice.Update` reads movement — read from the game on a machine that has it.
- Whether `ApplyCardStats.Pick` outside a native session needs `CardChoice` state (pickrID,
  `IsPicking`) set, and whether vanilla card animations (`CardVisuals`, `RPCA_ChangeSelected`)
  behave at a non-default scale.
- Camera: cards are world objects; the board must place them in the pick camera's view.
