# Original Level9 ordinary ally player acceptance

`full-battle-player-true-final-boss-ordinary-{search,replay}.mt2f` retain source
`full-battle-units-spells-and-junk-20261023-005911-ae5e05f9` and fresh process
`full-battle-units-spells-and-junk-20261023-010440-34e56d8e`.

Both load the actual separate Advisor assembly. The native jump explicitly
selects/unlocks `Level9TrueFinalBoss_V2`; deployment explicitly adds one original
HornedWarrior. The trace marks `StartingDeckUnchanged=false` and retains the
complete added-card control. Enemy definitions, original HP, phases, waves,
energy and Pyre80 are unchanged. No HP, phase or unit-definition edits occur.

Real three-turn/3,000-ms tactical player advice executes 15 card plays and five
EndTurns, including the added Warrior summon. All 20 inputs, roots, complete
published forecasts, successors and RNG match the independent frozen replay.
Both finish with a loss, settled terminal effects, normal exits and zero
capture/mismatch/unsupported/pending counters. This is controlled route and
deck acceptance; natural true-final-boss victory remains open.

Native pointer routing, pause/resume, hide/cancel and refresh interactions pass.
The source records 22 publications/290 pure captures; replay records zero
publications/search nodes and 49 pure captures. Actual input response median
is 4.55 seconds and nearest-rank P95 is 6.09 seconds. These are measured total
responses, distinct from the 3,000-ms search budget.

The checker additionally requires original five-body birth HP, casting/link
state, generated Scourge metadata and the actual Warrior with its ordinary
summon/attack signature. The existing original-deck 17-input player pair also
passes. No Mac package or full regression was built/run.

Focused entry: `./scripts/Check-PlayerAdvisor.ps1 -Case TrueFinalBossOrdinary -SkipBuild`.
