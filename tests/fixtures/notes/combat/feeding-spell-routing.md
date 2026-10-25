# Feeding spell carrier routing

Local alternatives to the original Banished/Umbra Covenant0 Ring7 defeat
exposed `Missing dead unit spawner card routing.`. The diagnostic tool exports
a twenty-input legal prefix and the original feeding spell
`236215da-c1dd-40b7-8793-27ce327a7e78`, card17, on room2 at turn4.

Two independent native processes restore the unchanged six-win campaign
checkpoint. They execute only that prefix and candidate, without card, unit,
health, status, relic or encounter injection. The first process preserves the
old rejection and null predicted successor; the second uses the repaired model.
Both complete21 inputs, protect original files and exit normally in52.62/51.05
seconds. These are component observations, not terminal victories.

Native `CardManager.CheckMonsterRemoveFromStandByCondition` distinguishes food
from ordinary death and returns EatenPile. The model's feeding callback already
did this, but the spell's outer cleanup tried to route the same missing carrier
to Exhausted again. The fix accepts a carrier already present in Eaten; missing
or unknown destinations still reject.

The focused pair verifies every root/input/successor and complete state/RNG,
including equipment and statistics. Consumed cards32,36,39 occur once in Eaten
and in no other pile or living unit. Exhausted order is21,20,29,10,8; Standby
retains31,2. Food does not increment TimesExhausted. The repaired spell appears
in ordinary candidate enumeration, and all replay parents remain immutable.
Original five-Morsel feeding and the independent Endless spell-death control
also pass. No full regression or Mac build is needed for this scoped change.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --feeding-spell-routing-pair-only tests/fixtures/combat/units/player-feeding-spell-routing-calibration.mt2f tests/fixtures/combat/units/player-feeding-spell-routing-replay-calibration.mt2f
```
