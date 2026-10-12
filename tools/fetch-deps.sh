#!/usr/bin/env bash
# Downloads the mod-side dependencies (BepInEx, Harmony, UnboundLib) from Thunderstore into
# deps/, laid out like a mod-manager profile so the csproj can use it as $(Profile), and the
# witness assemblies tools/refcheck proves the stubs against into deps/witness/. Nothing here is
# ours to commit, so it is fetched, pinned by hash, and git-ignored.
#
#   tools/fetch-deps.sh [destination]     (default: deps/ next to this script's parent)
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
dest="${1:-$root/deps}"
cache="$dest/.zips"
mkdir -p "$cache"

# name  thunderstore-path  sha256
pkgs=(
  "BepInExPack_ROUNDS BepInEx/BepInExPack_ROUNDS/5.4.1901 789c9cb21801edd7cecb3768f8563216397bff175661fe80f6696ccf584eda7d"
  "UnboundLib willis81808/UnboundLib/3.2.14 0c1f87cfde10a67bfb2b802ea4ae222f9bb98adfc20fd5d689d847c4c45d383f"
  # Witnesses only: built against the real game, so their references prove members exist.
  "ModdingUtils Pykess/ModdingUtils/0.4.8 be3734e138c5a350fec55d5c64a1c39cbde9d36742c35c4969febd504b499e84"
  "RoundsWithFriends olavim/RoundsWithFriends/2.2.2 9714584b5d92a5f0943acd74e755a10ac2c16682a5f7fa902c0298d98fa26d83"
  "SimulPicks RedRedRain/SimulPicks/0.3.36 e31baaa3c43e406650edd5a1b86f4e8b690a032d428fb98e1913d646d1d6d010"
  "WillsWackyManagers willuwontu/WillsWackyManagers/1.5.17 7ac7d3352ed8969ce2e695aa5081670aa515fd9535499f4ab211351e87fdf3dc"
  "CardChoiceSpawnUniqueCardPatch Pykess/CardChoiceSpawnUniqueCardPatch/0.1.10 311392c9665de8748d1ff8e61d6dd5663ece94292e33184f6e44ce8b3f838820"
  "NullManager Root/NullManager/1.3.1 44a41854989ee23215ccfc6d6be88bdda25a2501f01eebac2b34c938a4729e68"
  "PickPhaseShenanigans Pykess/PickPhaseShenanigans/0.0.5 83c007d95e57864285eadd1c135b90afc6ed63552af42a0a9491d3e69d9dcc09"
)

unzip_to() { python3 -c 'import sys,zipfile; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])' "$1" "$2"; }

fetch() {  # url zip sha256
  if [ ! -f "$2" ] || ! echo "$3  $2" | sha256sum -c --status; then
    curl -fsSL --retry 3 -o "$2" "$1"
  fi
  echo "$3  $2" | sha256sum -c --quiet
}

for line in "${pkgs[@]}"; do
  case "$line" in \#*) continue ;; esac
  read -r name path sum <<<"$line"
  zip="$cache/$name.zip"
  fetch "https://thunderstore.io/package/download/$path/" "$zip" "$sum"
  rm -rf "$cache/$name" && mkdir -p "$cache/$name" && unzip_to "$zip" "$cache/$name"
done

mkdir -p "$dest/BepInEx/core" "$dest/BepInEx/plugins/willis81808-UnboundLib"
cp "$cache/BepInExPack_ROUNDS/BepInExPack_ROUNDS/BepInEx/core/"{BepInEx.dll,0Harmony.dll} "$dest/BepInEx/core/"
cp "$cache/UnboundLib/plugins/UnboundLib.dll" "$dest/BepInEx/plugins/willis81808-UnboundLib/"
# MMHOOK.Rounds (MIT, olavim): a hook delegate per Assembly-CSharp method, exact signatures.
fetch https://api.nuget.org/v3-flatcontainer/mmhook.rounds/1.0.0/mmhook.rounds.1.0.0.nupkg \
  "$cache/MMHOOK.Rounds.nupkg" 9d3d5dcc2814ee82cbf25751b664a8ff6aef1cc54b3eca0224df02ff90960f3e
rm -rf "$cache/MMHOOK" && mkdir -p "$cache/MMHOOK" && unzip_to "$cache/MMHOOK.Rounds.nupkg" "$cache/MMHOOK"

rm -rf "$dest/witness" && mkdir -p "$dest/witness"
cp "$cache/MMHOOK/lib/net472/MMHOOK.dll" "$dest/witness/"
cp "$cache/UnboundLib/plugins/UnboundLib.dll" "$dest/witness/"
for name in ModdingUtils RoundsWithFriends SimulPicks WillsWackyManagers CardChoiceSpawnUniqueCardPatch NullManager PickPhaseShenanigans; do
  # Thunderstore zips are not consistent about folders, or even about the path separator.
  find "$cache/$name" -iname '*.dll' | while read -r f; do cp "$f" "$dest/witness/$name-$(basename "${f//\\//}")"; done
done
echo "deps ready in $dest"
