# Room masks and original spell moon upgrades

`room-moon-spell-upgrade-calibration.mt2f` retains component profile
`full-battle-no-cards-20261022-135526-81fbd623` (36.78 seconds, normal exit,
protected originals unchanged). The five unchanged spells are Celestial Storm,
Starstruck, Heavenfall, Gravity and Silver Strike. An original numeric upgrade
on Celestial Storm supplies a real five-times magic-power rescaling witness.

Fifteen settled native boundaries cover installing/removing original Eclipse,
three resource refreshes, four global phase updates and six floor selections,
including repeated selection and an unmasked control. Complete trains, hand
cards, private cached upgrades and gameplay RNG match eight immutable model
branches per boundary. There are 105 trait snapshots and forty active-state
changes. Full global phase under Eclipse supplies ten Exclusive and twenty-five
Additive witnesses: Gravity's Exclusive traits use the room's New override,
while Additive traits accept both global Full and room New.

The model now evaluates each original trait's modifier mode, validates every
captured room mask and refreshes spell hand upgrades when a moon attachment is
installed or removed, including a room with no character callback actors.
Unknown room payloads and unsupported mask values still fail focused guards.
Living-unit moon traits, other relic combinations and room preview callbacks
remain guarded. This component does not establish successful spell plays;
the complete paid composition is recorded separately below.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellUpgrades -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-upgrade-calibration.mt2f
```

## Complete paid spell chain and fresh native replay

`full-battle-original-moon-card-upgrades-room-mask{,-replay}.mt2f` retains
source `full-battle-units-spells-and-junk-20261022-135939-c671830e` (80.73 seconds)
and fresh replay `full-battle-units-spells-and-junk-20261022-140157-335020ae`
(73.76 seconds). Both win at Pyre80 after sixteen plays/five EndTurns, settle,
exit normally, preserve protected originals and have four zero failure counters.
Twenty-one independently frozen inputs, complete native states/gameplay RNG
and sixteen immutable full model chains match. This uses authored durable
Stewards/enemies and a free energy/phase control; the five spell definitions
and paid Eclipse remain original.

Both global phases exercise all five spells, original upgraded OnCast effects,
private upgrade reset on played/unplayed discard, redraw and positive existing
magic-power upgrades. Under global Full, Gravity on the Eclipse floor retains
the Exclusive New descend effect; Heavenfall's Additive New and Full upgrades
are both active. Physical positions, Dazed, damage, private caches and every
later combat/birth/draw boundary match. The independent replay searches no nodes.
This establishes a full controlled battle, not natural-deck or player acceptance.

```powershell
scripts/Run-FullBattleProbe.ps1 -OriginalMoonCardUpgradesRoomMask -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-room-mask.mt2f
```
