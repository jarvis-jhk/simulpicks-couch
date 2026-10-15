# Evidence from public mod sources

Mod code only. A search hit for a mirror of decompiled game code was seen and not opened.
Collected 2026-10-15; conclusions are in docs/COUCH.md.

- Hand size, `children`: UnboundLib https://github.com/Rounds-Modding/UnboundLib/blob/ecc61b0/UnboundLib/Unbound.cs#L181-L194 ;
  PickNCards https://github.com/pdcook/PickNCards/blob/fba15c2/PickNCards/DrawNCards.cs#L219-L265 ;
  field types https://github.com/Pandapip1/SelectAnyNumberRounds/blob/857d04c/patches/LastCardContinue.cs#L14-L17
- Slot positions/scale via ScreenToWorldPoint, z -5: https://github.com/pdcook/PickNCards/blob/fba15c2/PickNCards/DrawNCards.cs#L155-L216 , #L345-L351
- Shared CardChoiceVisuals: same file #L270-L330
- `SpawnUniqueCard(Vector3, Quaternion)` and the picker read at spawn: https://github.com/Rounds-Modding/CardChoiceSpawnUniqueCardPatch/blob/4ba3296/CardChoiceSpawnUniqueCardPatch/CardChoiceSpawnUniqueCardPatch.cs#L42-L125
- ModdingUtils `PickingPlayer`: https://github.com/pdcook/ModdingUtils/blob/6cdc3f3/ModdingUtils/Patches/CardChoiceGetRandomCardPatch.cs#L101-L115
- `CardChoice.Pick` flow, `RPCA_DoEndPick`, `PublicInt`: https://github.com/Pandapip1/SelectAnyNumberRounds/blob/857d04c/patches/PickNoRefresh.cs#L9-L36 , RespawnCardChoice.cs#L9-L110
- Forced pick + `DoPick` signature: https://github.com/pdcook/CompetitiveRounds/blob/ced4130/CompetitiveRounds/PickTimerHandler.cs#L28-L46 , Patches.cs#L25-L40
- RWF `ApplyCardStats.Pick` transpiler: https://github.com/olavim/RoundsWithFriends/blob/1c45b62/RoundsWithFriends/Patches/ApplyCardStats.cs#L15-L37 ;
  local input https://github.com/olavim/RoundsWithFriends/blob/1c45b62/RoundsWithFriends/Patches/CharacterSelectionInstance.cs#L218-L253
- Card application without CardChoice: https://github.com/willuwontu/ModdingUtils/blob/b7f24af/ModdingUtils/Utils/Cards.cs#L72-L143
- PickPhaseShenanigans `pickrID && IsPicking`: https://github.com/pdcook/PickPhaseShenanigans/blob/9054a26/PickPhaseShenanigans/PickPhaseShenanigans.cs#L281
- No public mod patches `CardChoice.Update`; PickPhaseImprovements has no public repo found.
