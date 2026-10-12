# Reference stubs

ROUNDS' own assemblies (Assembly-CSharp, Photon, TextMeshPro, uGUI, InControl) may not be
committed or downloaded in CI. These projects stand in for them at compile time only: each
declares just the types and members this mod compiles against, with `throw null` bodies. They
are our own source and contain no game code. At runtime the game's real assemblies are loaded
instead, matched by assembly name, so every declaration must have exactly the real shape:
field vs property, static vs instance, return and parameter types.

A wrong shape compiles fine and fails in the game with `MissingFieldException` /
`MissingMethodException`. `tools/refcheck` exists to stop that. It reads the built mod and
checks every reference into a stubbed assembly against **witnesses**, assemblies that were
compiled against the real game:

- `MMHOOK.Rounds` (MIT, NuGet): one hook delegate per Assembly-CSharp method, with exact
  signatures. Proves methods and property accessors.
- UnboundLib, ModdingUtils, RoundsWithFriends, upstream's own released SimulPicks DLL and a few
  card-framework mods from Thunderstore: their member references prove fields and Photon members.

`tools/fetch-deps.sh` downloads all of them pinned by sha256 into `deps/` (git-ignored).

## Adding to a stub

1. Write the declaration you need. Never more than the mod uses.
2. `tools/check.sh`. If refcheck prints `UNPROVEN`, find a witness that uses the member (another
   Thunderstore mod, pinned in `fetch-deps.sh`), or check it against the real game.
3. Only if neither is possible, list it in `attested.txt` with the reason you believe it.

The authoritative check is `tools/check.sh /path/to/ROUNDS/Rounds_Data/Managed` on a machine
with the game, which resolves every reference against the real assemblies.

Two mistakes refcheck caught on day one, both plausible from memory of PUN 2:
`Photon.Realtime.Player.IsLocal` is a readonly field, not a property, and
`PhotonNetwork.LocalCleanPhotonView` returns `bool`.

Unity's engine modules come from the `UnityEngine.Modules` 2018.4.34 package on
nuget.bepinex.dev (reference assemblies Unity publishes). An older version is chosen on purpose:
compiling against an API the game's Unity lacks would fail at runtime the same way.
