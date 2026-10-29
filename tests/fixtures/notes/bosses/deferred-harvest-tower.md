# Deferred multi-target spell and cross-floor Harvest

Pyreborne/LunaCoven, seed 1332512341, Covenant 0, ring 6. The checkpoint
follows four default-budget victories and a separately refined, independently
replayed ring 5 victory; it is not evidence of an entirely default-budget run.

Two independent original native processes replay 35 matching inputs and the
same turn 7 Greed cast. A dying Disciple of Ruin on room 2 triggers the
Entropic boss's Harvest, raising its room 0 Savage companion from 881/1250
to 931/1300 health. The historical model instead returned 881/1250. The
next hit's damage engine had retained a stale cross-floor snapshot while
draining the previous victim's death queue, overwriting the Harvest result.

The repaired engine merges the updated borrowed roster back into the active
damage engine. Two further independent native runs preserve the same inputs
and native states and record matching model predictions. The focused checker
compares all 36 full state/RNG transitions, historical mismatches, immutable
parents, and four concurrent boundary replays. This verifies the observed
interaction, not arbitrary remote effects or a complete battle victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-deferred-harvest-tower-original.mt2f
```
