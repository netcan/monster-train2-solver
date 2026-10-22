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

## Retained spell after enemy focus changes the selected floor

`player-moon-room-turn-calibration.mt2f` extracts a complete native EndTurn and
one queued hand-resource callback from failed actual player profile
`full-battle-units-spells-and-junk-20261022-140415-1e378fbe`. That source stops
after thirteen decisions with a full-state mismatch and exits normally; it is
explicitly not accepted as a winning battle. The component retains this failure
provenance instead of relabeling the source as successful.

The native deployment starts on Eclipse floor two at New. EndTurn advances to
Full, initial enemies focus floor zero, then the native trigger queue refreshes
the retained hand after drawing/turn-begin statuses. Gravity must switch from
Exclusive New descend to unmasked Full ascend. The modeled final selected floor
was already correct, but its retained upgrade still reflected the old floor.
BattleTurn now refreshes the hand at this post-queue boundary. Complete successor
state and every RNG match sixteen immutable branches; the independent native
callback matches its complete before/after contexts as well.

The original unmasked five-spell baseline is also refreshed with captured unit
masks, retaining source `full-battle-units-spells-and-junk-20261022-140415-8aa600d6`
(84.13 seconds) and fresh `full-battle-units-spells-and-junk-20261022-140601-c5143a8c`
(74.08 seconds). Both win at Pyre80 after fifteen plays/five EndTurns, settle,
exit normally, preserve protected originals and retain four zero failure counters.
Twenty frozen inputs and sixteen complete immutable chains match. The old archive
omitted masks required by strict movement validation; it is available in Git
history. The model guard remains intact. No full regression or Mac package is built.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-moon-room-turn-only tests/fixtures/player-moon-room-turn-calibration.mt2f
```

## Pending native effects after floor selection

The subsequent diagnostic player profile
`full-battle-units-spells-and-junk-20261022-141016-c8eba506` passes that EndTurn,
then exposes a separate driver/observer race on decision sixteen. Moving focus
from Eclipse floor two to unmasked floor zero enqueues its original support
spell. The old readiness check only inspected running triggers; it could issue
Gravity before RunCombat consumed the pending effects queue. Gravity therefore
still descended, while the settled action model correctly expected Full ascend.
That failed whole battle remains diagnostic evidence only.

The player observer and native driver now share a read-only settlement check:
pending effects must be empty, the current card effect must finish and native
external input must be enabled. The game processes its own queue normally; the
check neither executes effects nor rewrites card upgrades. Captured wait counts
make this boundary independently observable in subsequent player acceptance.

## Actual player victory and independent replay

`full-battle-player-moon-room-spells-{search,replay}.mt2f` retains source
`full-battle-units-spells-and-junk-20261022-141652-c2651ff8` (293.02 seconds)
and fresh replay `full-battle-units-spells-and-junk-20261022-142200-b805a421`
(127.11 seconds). Both actual player entry-point battles win at Pyre80 after
37 plays and six EndTurns, settle, exit normally, preserve protected originals
and have four zero failure counters. All 43 inputs, full states, forecasts and
gameplay RNG match; the fresh process searches no nodes.

The focused checker independently verifies 1,014 settled hand-trait snapshots,
72 Exclusive and 180 Additive Full/Eclipse witnesses, all five original spell
identities and 32 casts into masked floors across the pair. Nine native birth
boundaries per process and sixteen immutable complete model branches match.
The native driver observes 10,238 pending-effect waits across the pair; it reads
the original queue rather than manually draining or changing it.

The real player assembly publishes 45 times. Initial search takes 3.47 seconds
and the first used response takes 6.75 seconds; the next 42 decisions retain
the verified winning continuation (median/P95 response 0.97/1.46 seconds).
This uses the controlled original-spell scene with authored durable units,
energy/phase controls and a six-turn/ten-second tactical configuration. Natural
late-game decks, default-budget performance and other trait/relic combinations
remain separate acceptance tasks. No Mac package or full regression is built.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case MoonRoomSpells -SkipBuild
```

## Original Lunacy with room refresh and paid phase advances

`room-moon-spell-advance-calibration.mt2f` adds unchanged original Lunacy to
the five-spell component. Source `full-battle-no-cards-20261022-150949-47c8e3a5`
exits normally in 29.85 seconds and preserves protected originals. All fifteen
refresh/global-phase/selection/Eclipse install/remove boundaries preserve the
complete Lunacy instance and gameplay RNG, while the five spell upgrades retain
the existing 105 trait snapshots and forty active changes. Eight immutable
branches match each boundary; altered Lunacy scaling parameters remain rejected.

The old room-refresh guard rejected Lunacy merely because its trait name
contains `MoonPhase`. Its original `CardTraitScalingAdvanceMoonPhase` inherits
the empty resource callback; only the separately calibrated paid-cost advance
callback changes the global phase. The guard now accepts that captured original
signature and continues rejecting other parameters and resource traits.

`full-battle-original-moon-card-upgrades-lunacy{,-replay}.mt2f` retains source
`full-battle-units-spells-and-junk-20261022-151750-f4b2e802` (101.31 seconds)
and fresh process `full-battle-units-spells-and-junk-20261022-152015-0acb7aa2`
(95.75 seconds). Both win at Pyre80 after twenty plays/five EndTurns, settle,
exit normally, preserve protected originals and have four zero failure counters.
All twenty-five frozen inputs, complete states and RNG match the zero-node
fresh replay and sixteen immutable model branches.

Three unchanged Lunacy instances pay 2, 3 and 0 energy on Eclipse floor zero,
advance the global phase by even/odd/zero counts, grant exactly 2/3/0 Conduit
and enter discard. Twenty complete native hand-resource callbacks per process
also expose a missing immediate notification: managing a moon upgrade reapplies
magic power with native managers, which dispatches `CardUpdated` to Conduit.
The model now rebuilds that tracked upgrade while preserving its native dictionary
position; unchanged phase upgrades dispatch no extra notification. The retained
Celestial Storm switches Conduit damage 10 to 2 and back as its stronger-magic
trait changes. Whole-battle refresh had previously hidden this intermediate gap.
The individual callback comparisons include private contributor order, multiplied
card sets, every card upgrade and RNG; no comparison fields are omitted.

These are controlled durable-unit/energy scenes, not natural-deck or actual
player acceptance for this composition. Old five-spell room and original Lunacy
baselines pass focused checks. No full regression or Mac build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellAdvance -SkipBuild
scripts/Run-FullBattleProbe.ps1 -OriginalMoonCardUpgradesLunacy -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-advance-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-lunacy.mt2f
```

## Actual player Lunacy/Conduit/Eclipse composition

`full-battle-player-moon-room-lunacy-{search,replay}.mt2f` retains actual player
source `full-battle-units-spells-and-junk-20261022-152609-d3fc9e2a` (596.44 seconds)
and independent process `full-battle-units-spells-and-junk-20261022-153642-d0c04c1b`
(179.99 seconds). Both win at Pyre80 after forty-one plays/seven EndTurns,
settle, exit normally, preserve protected originals and have four zero failure
counters. Every one of the forty-eight inputs, roots, successors, forecasts
and gameplay RNG matches; the replay searches no nodes. Each process casts
eight original Lunacy cards for actual paid costs 4/3/20/3/3/3/3/3, with exact
phase parity and Conduit increments, alongside all five original moon spells
and Eclipse. The player variant uses its own choices rather than the fixed
2/3/0 paid-chain script above.

The real player assembly publishes fifty times, takes 568 pure captures, renders
the panel, and passes pause/resume, hide/cancel/show, refresh, pointer routing
and drag controls. The first ten deployment forecasts are legal bounded plans
without a terminal victory; the first full winning search occurs at turn one
(4.48 seconds search, 9.34 seconds including capture and verification).
Thirty-seven later inputs retain the complete winning continuation, with median/
P95 response of 1.73/2.54 seconds. All settled hand-trait snapshots and sixteen
immutable complete chains are checked by the same focused pair gate as the
earlier five-spell player scene, adding paid Lunacy witnesses.

This is the controlled durable-unit/energy scene with a six-turn/ten-second
tactical configuration. The deployment forecast boundary and the initial
search/verification time remain explicit; this does not establish default
three-second performance or natural late-game acceptance. No Mac build or full
regression is performed.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case MoonRoomLunacy -SkipBuild
```

## Unchanged Blight/Junk cost relic with Lunacy and Eclipse

`room-moon-spell-cost-relic-calibration.mt2f` adds the original
`CheaperBlightScourge` before battle, without modifying its two unconditional
`RelicEffectModifyCardTypeCost` definitions. Original SelfPurgeBlight and
ScourgeT1 retain their definitions and the native -99 cost/+99 X-cost offsets.
Source `full-battle-no-cards-20261022-154309-449c5152` exits normally in 30.07
seconds, preserves protected originals and matches all fifteen component
boundaries, complete state/RNG and eight immutable branches per boundary.
The six spells, including unchanged Lunacy, keep 105 trait snapshots, forty
upgrade changes and both Eclipse modes. An altered conditional-cost payload
is still rejected before refresh.

The prior guard rejected every active relic. It now admits only the existing
validated unconditional Blight/Junk cost signatures, whose filters do not
match spells and whose effects have no moon-resource callback. Other effect
types and unknown/conditional cost parameters remain guarded.

`full-battle-original-moon-card-upgrades-cost-relic{,-replay}.mt2f` retains
source `full-battle-units-spells-and-junk-20261022-154501-733cf86e` (111.99
seconds) and fresh process `full-battle-units-spells-and-junk-20261022-154726-785f5ad6`
(99.54 seconds). Both win at Pyre80 after twenty-two plays/five EndTurns,
settle, exit normally, preserve protected originals and have four zero failure
counters. All twenty-seven frozen inputs, full states and RNG match the
zero-node independent replay and sixteen immutable model chains. The two
unchanged cost-control cards are played and purged at zero energy, alongside
Lunacy's paid 2/3/0 advances, all five original moon spells and Eclipse.
Twenty complete native hand-resource callbacks also match per process.

The old five-spell component and original card-type-cost battle pass focused
checks. These remain controlled durable-unit/energy scenes; actual player
acceptance of this relic composition and natural late-game decks are separate.
No full regression or Mac build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellCostRelic -SkipBuild
scripts/Run-FullBattleProbe.ps1 -OriginalMoonCardUpgradesCostRelic -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-cost-relic-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-cost-relic.mt2f
```
