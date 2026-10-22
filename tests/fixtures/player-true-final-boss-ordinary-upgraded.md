# Original Level9 numeric ally player acceptance

`full-battle-player-true-final-boss-ordinary-upgraded-{search,replay}.mt2f`
retain source `full-battle-units-spells-and-junk-20261023-012312-8e86edea`
and fresh process `full-battle-units-spells-and-junk-20261023-012749-1b736f72`.

Both use the actual separate Advisor assembly, original Level9 five-body
encounter and three-turn/3,000-ms tactical configuration. Native route selection
and unlocks are explicit controls. Deployment explicitly adds one original
HornedWarrior and applies original `BlessingBonusHealth` (+20 HP), then
`EnhanceAttackOnConsume` (+10 attack). Its paid summon produces 25/28 with
Multistrike 1. Source definitions, enemy HP/phases/waves, energy and Pyre80
remain unchanged; `StartingDeckUnchanged=false` records the deck control.

The actual player advice executes 15 card plays and five EndTurns, including
the upgraded Warrior. Every input, root, complete published forecast, successor
and RNG matches the independent frozen replay. Both exit normally after a
settled loss with all four native failure counters zero. This controlled deck
acceptance does not establish natural true-final-boss victory.

Source overlay interactions pass: pointer routing, pause/resume, hide/cancel,
show and refresh. Its 22 publications/283 pure captures and replay's zero
publications/search nodes/49 pure captures have zero purity failures. Actual
total response median is 4.54 seconds; nearest-rank P95 is 6.35 seconds.
These measurements include work outside the 3,000-ms soft search budget.

The checker requires the exact declared upgrade controls, original queued
birth HP, casting/link state, generated Scourge metadata and the actual
upgraded Warrior with complete ordinary source provenance. The unmodified
Warrior player pair also passes after extending the shared checker.

Focused entry:
`./scripts/Check-PlayerAdvisor.ps1 -Case TrueFinalBossOrdinaryUpgraded -SkipBuild`.
No full regression or Mac build was performed.
