
## Install

1. In r2modman (or Thunderstore Mod Manager) open your ROUNDS profile and **disable SimulPicks**
   (this replaces it; both use the same plugin id). UnboundLib and ModdingUtils stay.
2. Settings → Import local mod → choose `SimulPicksCouch-<version>.zip` from this release.
   By hand instead: put `EveryonePicks.dll` in `BepInEx/plugins/`.
3. Start the game through the mod manager. Steam Deck: the same r2modman profile under Proton.

## Test (needs 2–4 controllers; this build has not been in the game yet)

- Local match (offline, every player on this machine) with 2 players, then 3 or 4.
- At each pick: does every player get their own framed region with their name and cards?
- Each player moves with their own stick/d-pad and takes a card with jump. Do the others'
  cursors stay put while one player moves?
- Picked cards land on the right player (card bar) and the round starts once all have picked.
- Let one player run out the timer: the selected card is taken automatically.
- A card that grants an extra pick: that player gets a second hand in their region.
- Cards too small/large: Mod Options → EveryonePicks → Couch card size.

- With your usual mod pack (ClassesManagerReborn, WillsWackyManagers, curses, Pick N Cards …):
  play a few rounds. Card effects and class cards land on the right player? Nothing stuck?
  The log line "Couch board: other mods hooking the pick: …" names the suspects.

If a mod does not get along with couch mode, put its id from that log line into
`Couch.SequentialWith` in `BepInEx/config/com.redredrain.rounds.simulpicks.cfg`; local players
then take turns while it is loaded.

If anything goes wrong, open an issue with `BepInEx/LogOutput.log`. To go back to taking turns,
switch off "Players on this machine pick at once" in Mod Options.
