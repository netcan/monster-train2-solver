# Unit purge with relics without purge callbacks

Random28 ring 7 exposed the blanket rejection of relics during a unit-card
purge. Native dispatch calls only effects implementing IOnPurgeRelicEffect.
Version 435 permits the five observed original effect types that do not
implement that interface; all other effects remain guarded.

Two original and two repaired independent native observations replay 33 inputs
and compare complete states and RNG, including the purged unit card and its
exhausted equipment. A real purge callback is rejected at the purge entry point;
four concurrent branches preserve their parent. This component observation is
not a battle or campaign victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-random28-ring7-unit-purge-original.mt2f
```
