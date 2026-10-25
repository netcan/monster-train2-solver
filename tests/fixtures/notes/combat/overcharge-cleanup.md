# Overcharged echoes after actual room combat

Hellhorned/Wurmkin seed770862752 Ring4 naturally reaches room2 with6 charged
echoes and maximum4 at the ninth EndTurn. The unchanged native game finishes
that room, removes its overcharge to4, then wins in the Pyre room with49HP.
The original model retained6, producing a room-stage, train-stage and final
33rd-input mismatch despite predicting the correct victory and Pyre health.

CombatManager.RemoveOverchargeCorruptionAtEndOfTurn clamps after the room's
post-combat modifiers and end-turn relic effects, even when no units remain.
RoomState.AdjustCorruption preserves permanent corruption and does not dispatch
addition callbacks on a decrease. The model now uses that existing adjustment
path at actual room completion. Individual exchanges and previews retain6;
early terminal combat still exits at its native boundary. Pure controls cover
below-maximum values and the permanent floor as well as overcharge.

The two33-input component observations preserve the original discrepancy and
an independently repaired terminal forecast, including complete state/RNG and
immutable roots. Profiles: `full-battle-units-spells-and-junk-20261025-091702-c096af66`
and `full-battle-units-spells-and-junk-20261025-092117-f577ab45`.
The development observer now captures terminal component boundaries separately
from rolling whole-battle acceptance. The FightClub identity binding is recorded
at native construction in both observations; battle values remain exact.
These components do not count as a complete accepted source/replay battle pair.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-overcharge-cleanup-calibration.mt2f -SkipBuild
```
