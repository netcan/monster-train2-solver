# Natural Fragile player advice and independent replay

`full-battle-player-boss-fragile-bounded-{search,replay}.mt2f` retains source
`full-battle-units-spells-and-junk-20261022-164718-b83d5103` (999.00 seconds)
and fresh process `full-battle-units-spells-and-junk-20261022-170429-01e46adb`
(325.77 seconds). Both load the real Advisor assembly and start from the
unchanged natural Ring8 checkpoint with seven preceding wins, Pyre62 and seed
424244. Restore verifies all thirteen checkpoint members. No encounter, boss,
deck or stat authoring is used.

Both processes finish with a native loss after 44 card actions/nine EndTurns.
All 53 inputs, deployment roots, native successors, complete published forecasts
and RNG match. Replay consumes the original RollingDecisions directly with
zero search nodes. Both have normal exit zero, settled terminal effects,
protected originals unchanged and zero capture/mismatch/unsupported/pending
counters. Ninety-five live boss snapshots cover Casting and Relentless, queued
birth, the original three-stack enemy aura and three native ability activations.
The source records 55 publications and 591 pure captures and passes panel,
pointer, drag/clamp, pause/resume, hide/cancel/show and refresh checks.

The scene uses a three-turn, three-second soft search budget with the optional
tactical seed. Six publications use explicitly shorter complete windows: two
one-turn windows and four two-turn windows. The other forecasts cover the
requested horizon or a verified terminal loss. All six shorter windows keep
`CompletedHorizon=false`, separate requested/completed counts and the display
label `预算内缩短`; replay preserves their metadata and complete forecast states.
Actual response median/P95 is 7.03/9.78 seconds including capture and model
verification. Atomic model expansions can exceed the search soft limit.

The first diagnostic failed immediately after champion birth: player capture
registered the prepended relic OnEquipmentAdded trigger before the four earlier
ability triggers. Advisor now installs both trigger-creation observation hooks.
The champion keeps creation IDs 0..3 for its ability callbacks and ID4 for the
prepended relic callback. The first 17-input three-second diagnostic then
exposed an incomplete requested horizon. That original failure is preserved
separately in [budget-window controls](../planning/player-budget-windows.md). An unfinished
ten-second diagnostic was stopped when the bounded-window fix became available;
neither diagnostic is curated as a full battle.

The player gate admits all six calibrated Level8 definitions, the ordinary
AllyBuffer/Fragile/Corrupter and their Trial counterparts, with existing
action/controller payload validation. Level9 and uncalibrated Covenant birth
effects retain their guards.
The bounded pair checker requires the actual loss and never grants winning
acceptance; the existing winning-pair checker still requires native victories.
Native player availability is accepted for this bounded Fragile route. A
separate twelve-turn/30-second pair now proves a natural victory; see
[winning Fragile evidence](player-fragile-winning.md). This bounded pair still
requires losses and does not inherit that result. General strategy quality
remains open. No full fixture regression or Mac build is performed.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case FragileBounded -SkipBuild
```
