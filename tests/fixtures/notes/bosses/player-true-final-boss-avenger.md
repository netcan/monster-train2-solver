# Original Level9 OnHit ally player acceptance

`full-battle-player-true-final-boss-avenger-{search,replay}.mt2f` retain source
`full-battle-units-spells-and-junk-20261023-040607-f9f541a3` and fresh process
`full-battle-units-spells-and-junk-20261023-040931-4d847479`.

Both load the actual separate Advisor assembly. Native jump/unlock explicitly
selects `Level9TrueFinalBoss_V2`; deployment explicitly adds the original
Avenger. Enemy definitions, original HP, phases, waves, energy and Pyre80
are unchanged. No health, phase, upgrade or unit-definition edits occur.
The trace retains this route/deck control and `StartingDeckUnchanged=false`.

Real three-turn/3,000-ms tactical advice executes four plays and five EndTurns,
including the actual Avenger summon at 20/40. All nine inputs, roots,
complete published forecasts, successors and RNG match the fresh frozen
replay. Three captured linked attackers actually trigger its original
OnHit callback and receive one melee weakness; their complete unit turns
and shared contexts/RNG also match independent model execution.

Both lose, settle terminal effects, exit normally and leave protected files
unchanged, with capture/mismatch/unsupported/pending counters all zero.
Source pointer/pause/hide/refresh interactions pass, with 11 publications
and 141 pure captures; replay has zero publications/search nodes and 29
pure captures. Actual total response median/nearest-rank P95 is 5.18/5.57
seconds, distinct from the 3,000-ms search soft budget.

The gate requires the declared original added card, its actual native summon,
full five-body queued birth HP and casting/link state, generated Scourge
metadata, the three OnHit witnesses and existing signature/terminal/Valor
counterexamples. The ordinary Warrior and numeric-upgrade player profiles
retain their own strict checks. This controlled loss does not establish
natural victory or general strategy quality. No full regression or Mac build
is performed.

Focused entry: `scripts/Check-PlayerAdvisor.ps1 -Case TrueFinalBossAvenger -SkipBuild`.
See [native OnHit calibration](true-final-boss-avenger.md) for the mechanism.
