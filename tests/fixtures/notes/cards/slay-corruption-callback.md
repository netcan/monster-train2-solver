# Card Slay corruption drains unit callbacks

Hellhorned/Wurmkin seed770862752, Ring6: an offline alternative uses the
original two-hit spell `ae114dc4-d08f-4909-a2ad-d99215661463` against enemy6
in room1. The spell has merchant Corrupt, a one-corruption eligibility cost
and authored OnKill gain2 corruption. Shardsoul Carver is already in that room.

Card Slay called the room corruption primitive without its unit callback
queue. 0.2.56 routes that payload through the retained damage engine, draining
each native CorruptionAdded unit callback before committing room corruption
and before removing the lethal victim. Existing nested queue and corruption
relic guards remain in place; arbitrary nested combinations are not claimed.

Two independent original/repaired native observations each execute12 inputs
from unchanged deployment. The original boundary preserves its null forecast;
the repaired forecast matches native. Carver attack29->38 and armor9->18,
room corruption3->5 and victim6 removal match along with every full successor,
forecast and RNG state. Parents remain immutable; original files are unchanged.

Controls pass: four-input Corrupt-before-summon native pair,62-input
merchant/returned Corrupt pair and original card OnKill energy/refund battle.
A separate60-second local refinement over decisions6..8 completes11 of12
alternatives with zero unsupported transitions and finds no win. It is not an
impossibility proof or a native Ring6 victory.

```powershell
./scripts/Check-Models.ps1 -SkipBuild -Fixture player-slay-corruption-callback-calibration.mt2f
```
