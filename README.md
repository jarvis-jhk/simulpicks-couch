# EveryonePicks — couch fork

> This fork adds **couch mode**: several players on one PC or Steam Deck pick their cards at the
> same time, each on their own part of the screen. Work in progress, see [ROADMAP.md](ROADMAP.md).
> The original mod is [RedRedRain/EveryonePicks](https://github.com/RedRedRain/EveryonePicks).

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
