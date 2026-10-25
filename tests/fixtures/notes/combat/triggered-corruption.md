# Champion Strike corruption queues Inspire

Random campaign09,Wurmkin/Hellhorned,seed771950956,Covenant0,Ring4,
checkpoint `campaign-ring4-20261025-213144-ce1dfcd9`. After the merchant
Juice/Corrupt Fracture calibration,actual0.2.78 advice advances through
multiple turns but stops before terminal combat. Its final zero-turn search
lists the room2 CorruptionAdded preview gap among its reasons. The run is
incomplete,not an accepted default victory or complete policy loss.

A separate pure deployment route extracts30 inputs and an EndTurn boundary
at turn7. Two independent native observations repeat all31 inputs with full
state/RNG and immutable parents. The original model refuses the next turn's
room2 preview. Champion MonsterWurmChampion1 has OnAttacking AdjustCorruption
Room,+1; allied ShardsoulCarver5 has CorruptionAdded,+3attack/+3armor.

Native RoomState.AdjustCorruption queues each callback. CombatManager refuses
to recursively drain an already running trigger queue,so the room corruption
commits first,then the queued Inspire effects run. The passive development
probe records all eight next-turn preview callbacks: corruption2..9,
Carver attack20->44 and armor0->24. Every callback completes in preview at
turn8,room2. After cancellation,actual Carver remains20attack/30HP/no armor
and actual room corruption remains1; the full successor checks that restored
state and all retained preview writes as well. Native runtime81.87s/80.61s.
This is preview evidence,not eight actual combat buffs applied to the live unit.

Version0.2.80 permits the calibrated Champion OnAttacking,+1 Room signature
to queue unit callbacks before committing the changed room capacities. Unknown
owners and amounts remain rejected by focused negative controls. A direct
queue check requires the deferred Inspire callback and committed corruption2.
Tower adjustments and card Slay keep their existing guards. The prior12-input
Slay/corruption/Carver native pair and shared pure checks still pass. Read-only
callback instrumentation is DEVELOPMENT_PROBE-only and only active for this
exact unsupported-branch request; it is absent from the player plugin.

The now-complete tactical route exports38 inputs at Pyre90 and passes an
independent full model replay. It still requires native battle acceptance;
actual-default search is assessed separately.

```powershell
./scripts/Check-Models.ps1 -Fixture player-triggered-corruption-calibration.mt2f -SkipBuild
```
