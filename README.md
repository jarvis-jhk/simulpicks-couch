# EveryonePicks — couch fork

> This fork adds **couch mode**: several players on one PC or Steam Deck pick their cards at the
> same time, each on their own part of the screen with their own controller. Prereleases are on
> the [releases page](../../releases) with install and test steps; see [ROADMAP.md](ROADMAP.md).
> The original mod is [RedRedRain/EveryonePicks](https://github.com/RedRedRain/EveryonePicks).

## Couch mode

When two or more players on this machine owe a pick at the same time, a couch board replaces the
one-at-a-time pick: two players get a row each, three or four a quarter of the screen, framed in
their colour with their name and a countdown. Each chooses with their own controller (stick or
left/right, jump to take the card). Cards are rolled for that player, so class and rarity rules
apply as in a normal pick, and results go through SimulPicks' usual sync, so online lobbies mixing
couch and single players work the same way. One local player keeps the normal pick unchanged.
Config `Couch.CouchMode` (Mod Options: "Players on this machine pick at once") switches it off;
`CouchCardScale` sizes the cards. Not yet tested in the game: see [NOTES.md](NOTES.md).

A ROUNDS mod. Everyone picks their card at the same time instead of taking turns.

[Thunderstore](https://thunderstore.io/c/rounds/p/RedRedRain/SimulPicks/)

- Cards show up as each player locks one in
- Bottom left shows who's picked and who you're waiting on
- Hover a player's cards to spread them out
- Extra picks from other mods work
- Nulled cards reach everyone, not just the player who picked one
- Someone quitting won't kill the match

## Bugs

[Open an issue.](../../issues/new/choose) Attach `BepInEx\LogOutput.log` if you have it.

## Known issues

- Reroll and other card-removal effects don't sync. The card sync only adds cards, so anything
  that removes or replaces them only happens on the picker's screen.
- Cards that use `*=` in `SetupCard` can apply different stats to the picker than to everyone
  else.
- otDan-PickTimer isn't compatible.

## Building

```
cd src
dotnet build -c Release -p:EpDebug=false
```

Without ROUNDS installed (CI, Linux), `tools/check.sh` fetches the dependencies, builds against
the reference stubs in `refs/` and checks every game member the DLL uses; see
[refs/README.md](refs/README.md).

Drop `EpDebug=false` for a dev build with F10 bound to a fake reveal, handy for working on the
visuals without a lobby.

Defaults to the usual Steam and Thunderstore paths. If yours differ, make `src/Local.props`:

```xml
<Project>
  <PropertyGroup>
    <Profile>D:\path\to\your\profile</Profile>
  </PropertyGroup>
</Project>
```

Targets `netstandard2.0` on purpose. The game runs an old Mono and anything newer lets the
compiler bind methods that don't exist at runtime.

## Licence

MIT.
