# Native Covenant run startup

`StartingCovenantLevel` selects a native rank in the disposable RunSetup
screen using its original `HandleAscensionLevelChanged` callback. The game
then uses normal `SaveState`, `StartingConditions` and `StartRun` logic.
The probe neither activates Covenants after startup nor invokes the startup
deck effects. Selection records the previously unlocked maximum and makes
no claim that the disposable profile earned the requested level.

The original rank-one definition is the sole cumulative native owner.
Read-only hooks observe exactly one original `RelicEffectPurgeStarterDeck`
and one `RelicEffectAddCardSetStartOfRun`: fifteen cards become the single
champion, then twenty cards. Purging preserves all sixteen RNG streams;
the original card-set generation runs normally and its before/after RNG
is recorded. The rebuilt deck exactly equals the actual starting deck.
The production model consumes captured battle/deck state; it never replays
these campaign effects during search.

`full-battle-native-covenant-startup-01.mt2f` comes from
`full-battle-no-cards-20261023-105416-e7548b0f`. The native process exits
normally after 47.08 seconds, with protected files unchanged and all four
failure counters zero. It wins in seven EndTurns with Pyre10. All native
waves, complete turns, fourteen train stages, 57 room stages, state/RNG,
and sixteen independent immutable full chains match. Pure native stat
queries preserve their frames and all sixteen RNG streams; spoofed wave
origins remain rejected.

Run `ModelChecks --native-covenant-startup-only <archive>`. Higher cumulative
ranks, natural campaign completion and earned unlocks remain separate
acceptance targets.

The actual separate Advisor assembly also passes this native rank-one
startup, including full UI interactions and 31 executed inputs (24 card
plays and seven EndTurns). The source publishes 33 advice updates. A fresh
native process independently rebuilds the same twenty-card starting deck,
executes the same 31 frozen inputs without searches/publications, and wins
with the same Pyre56. Every complete forecast, root, successor, terminal
state and RNG matches. Both processes exit normally with protected files
unchanged, settled effects and all four failure counters zero. Source/replay
pure captures are 386/106, with no purity failures.

Player archives are `full-battle-player-native-covenant-startup-search.mt2f`
and `full-battle-player-native-covenant-startup-replay.mt2f`. Source profiles,
prefixed `full-battle-units-spells-and-junk-20261023-`, are
`105634-a8426ece` and `110107-4a1ce13e`, taking 258.52/127.50 seconds.
Advice uses three turns, a 3,000-ms soft budget and tactical candidates;
this result does not establish a hard response deadline or default-budget
winning quality. Run `scripts/Check-PlayerAdvisor.ps1 -Case NativeCovenantStartup -SkipBuild`.

The capture also records the original Covenant catalog and effect signatures,
so subsequent batches can distinguish battle effects from campaign-only
effects without inferring behavior from display names.

## Native cumulative rank two

The original `Covenant02_BattlesNewEnemies` has ID
`92c18b99-e4c0-4953-ba2c-4815f570a66e` and no combat-effect callbacks.
Original spawn selection already runs in the native game before capture.
Ordinary wave projections now accept the exact original 01+02 owners and
retain their order. Modified/extra 02 callbacks are rejected. This does not
broaden the calibrated original Level9 or Ring8 owner sets; their unproven
rank-two compositions remain guarded.

`full-battle-native-covenant-startup-02.mt2f` comes from
`full-battle-no-cards-20261023-110732-0d7ec801`. It uses normal native
rank-two starting conditions and both cumulative owners, performs the original
rank-one purge/add callbacks once each, and builds twenty starting cards.
The original first battle wins in seven EndTurns with Pyre8. All fourteen
train stages, 57 room stages, state/RNG and sixteen immutable full chains
match. Native runtime is 54.18 seconds, normal exit, protected files unchanged,
and capture/mismatch/unsupported/pending all zero. Run the same focused
`--native-covenant-startup-only` checker.

Actual rank-two advice and fresh native replay also pass: 31 inputs (24 card
plays and seven EndTurns), 33 source publications, full UI interactions,
and every complete forecast/root/successor/state/RNG. Both processes win
with Pyre52, exit normally with protected files unchanged and all failure
counters zero. Pure captures are 391/106 with no purity failures. Source
and replay suffixes are `110827-6b6810c5` / `111251-fbcee1e8`, prefixed
`full-battle-units-spells-and-junk-20261023-`, taking 262.50/113.51 seconds.
Player archive prefix is `full-battle-player-native-covenant-startup-02`,
with `-search.mt2f` / `-replay.mt2f`. Run
`scripts/Check-PlayerAdvisor.ps1 -Case NativeCovenantStartup02 -SkipBuild`.

## Native cumulative rank three

The original `Covenant03_MerchantCostsIncrease` has ID
`dcb31cd7-4293-4b07-b396-5456a0ce0b6e`. Its exact two effects are
`RelicEffectModifyMerchantRerollCosts` and
`RelicEffectModifyMerchantPurgeCosts`: sealed native `IMerchantRelicEffect`
implementations expose price data and no combat callbacks. Captured battle
gold/deck/upgrades remain authoritative. The battle model admits only the
original identity and complete two-effect signature; this does not claim
to simulate merchant purchases or broaden outer/final-boss combinations.

`full-battle-native-covenant-startup-03.mt2f` comes from
`full-battle-no-cards-20261023-111541-b0da8023`. All three cumulative
original owners and native starting conditions are retained, with one
original purge/add callback each and twenty cards. The original first
battle wins in seven EndTurns with Pyre8. Fourteen train stages, 57 room
stages, complete state/RNG and sixteen immutable full chains match. Native
runtime is 49.90 seconds, normal exit, protected files unchanged, and all
four failure counters zero.

Actual player advice and a fresh frozen replay also win with Pyre52 in seven
turns: each executes 31 inputs (24 card plays and seven EndTurns). All 33 source
publications, UI interactions, complete forecasts, roots, successors and RNG
match. Player captures are 383/106 with zero purity failures; native runtimes
are 235.30/113.94 seconds, both normal exits with protected files unchanged and
all four failure counters zero. Profile suffixes are `111631-90665d4e` /
`112028-f0fa9d35`, prefixed `full-battle-units-spells-and-junk-20261023-`.
Player archive prefix is `full-battle-player-native-covenant-startup-03`, with
`-search.mt2f` / `-replay.mt2f`. The focused checker is
`scripts/Check-PlayerAdvisor.ps1 -Case NativeCovenantStartup03 -SkipBuild`.
