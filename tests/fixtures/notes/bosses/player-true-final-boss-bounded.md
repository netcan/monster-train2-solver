# Original Level9 player advice and independent replay

`full-battle-player-true-final-boss-bounded-{search,replay}.mt2f` retain source
`full-battle-units-spells-and-junk-20261022-234012-62cfad2c` (226.92 seconds)
and fresh process `full-battle-units-spells-and-junk-20261022-234439-c08a7224`
(92.94 seconds). Both load the actual separate Advisor assembly. The explicit
native jump selects `Level9TrueFinalBoss_V2` and overrides its unlock; this is
a controlled route selection, not natural campaign progression. Original
encounter/character assets, starting deck and Pyre80 remain unchanged. There
are no health, phase or wave controls.

Both finish with a loss after 12 card plays and five EndTurns. Every one of
the 17 inputs, deployment roots, native successors, complete published plans,
state and RNG matches. Replay consumes the source decisions with zero search
nodes and publications. Both exit normally with settled terminal effects,
protected files unchanged and zero capture/mismatch/unsupported/pending counters.

The source records 19 real player publications and 247 pure captures; replay
records 43 pure captures. Pointer routing, panel rendering, pause/resume,
hide/cancel/show, refresh and generation changes pass. Three-turn/3,000-ms
tactical search has actual median/P95 response 4.62/6.63 seconds including
capture and verification; the soft search budget is not total response time.

The player gate now admits the original five Level9 definitions with no
Covenant. It captures queued birth, the global Top controller, all five linked
bodies, three HP owners, physical anchors, Hellforged Casting flags and native
generated Scourges. Existing model payload checks continue to guard extra
allies, upgrades, equipment, active relic effects and uncalibrated terminal
signatures. Diagnostic writers are disabled in the player build; pure capture
does not depend on the development route environment flag.

This pair establishes bounded player advice and complete losing replay. It
does not establish a natural win, higher Covenant support or all late-game
deck/relic combinations. The separate controlled component fixtures establish
specific lethal and Relentless boundaries. No Mac package or full regression
is built/run.

Focused acceptance:

```powershell
./scripts/Check-PlayerAdvisor.ps1 -Case TrueFinalBossBounded -SkipBuild
```

Native source uses `-OriginalTrueFinalBoss -BattleDistance 8 -PlayerAdvisor
-RollingAdvice -PlannerHorizon 3 -PlannerTimeLimitMs 3000 -PlannerTacticalSeed`;
fresh replay adds `-RollingInput <source/full-battle.mt2f>`.
