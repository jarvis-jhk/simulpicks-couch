#!/usr/bin/env bash
# The CI gate, runnable anywhere with a .NET 8 SDK and no game installed: fetch the pinned
# dependencies, build the release DLL against the reference stubs, and prove every game member
# it uses exists (tools/refcheck). Pass a ROUNDS Rounds_Data/Managed directory to check against
# the real game instead:   tools/check.sh [/path/to/ROUNDS/Rounds_Data/Managed]
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

tools/fetch-deps.sh
dotnet build src -c Release -p:EpDebug=false -p:UseRefStubs=true --nologo -v:q
dotnet build src -c Debug -p:UseRefStubs=true -o build/debug --nologo -v:q
dotnet build tools/refcheck -c Release -o build/refcheck --nologo -v:q

for dll in src/bin/Release/EveryonePicks.dll build/debug/EveryonePicks.dll; do
  echo "refcheck $dll"
  if [ $# -gt 0 ]; then
    dotnet build/refcheck/refcheck.dll "$dll" --real "$1"
  else
    dotnet build/refcheck/refcheck.dll "$dll" --witness deps/witness/*.dll --attested refs/attested.txt
  fi
done
