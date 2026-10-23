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
to simulate merchant purchases. Later outer-boss combinations have separate
[native acceptance](native-covenant-outer-bosses.md); final bosses remain separate.

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

## Native cumulative rank four

The exact original `Covenant04_StrongerBosses` identity and complete ordered
25-effect signature are admitted for ordinary waves: eleven added triggers,
two spawn statuses, ten health and two attack adjustments. Trigger character
filters and spawn callbacks use the existing native-calibrated models; this
has separate [outer-boss acceptance](native-covenant-outer-bosses.md). Final-boss
composition scope remains separate. A mutated extra effect is
rejected by the focused checker.

`full-battle-native-covenant-startup-04.mt2f` comes from
`full-battle-no-cards-20261023-112448-972d8ce8`. Original run setup selects all
four owners, purges/adds once each and retains twenty cards. The complete
first battle wins in seven EndTurns with Pyre8: fourteen train stages,
57 room stages, four pure native stat queries, seven authored wave
definitions and sixteen immutable full chains match. Native runtime is
66.83 seconds, normal exit, protected files unchanged and all four failure
counters zero.

Actual player advice and fresh frozen replay both win with Pyre44: each
executes 31 inputs (24 plays and seven EndTurns). All 33 source publications,
UI interactions, complete forecasts, roots, successors and RNG match. Pure
player captures are 395/106, native runtimes 296.15/157.96 seconds; both exit
normally with protected files unchanged and all four failure counters zero.
Profile suffixes are `112620-aed191fc` / `113118-028effab`, prefixed
`full-battle-units-spells-and-junk-20261023-`. Player archive prefix is
`full-battle-player-native-covenant-startup-04`, with `-search.mt2f` /
`-replay.mt2f`; focused acceptance case is `NativeCovenantStartup04`.

## Native cumulative rank five

The exact original `Covenant05_VengefulShards` identity has two
`RelicEffectAddCardsStartOfRun` callbacks. Both execute through native startup:
the first-rank purge/add sequence changes 15 cards to one champion and then
twenty cards; the two additional callbacks each append one original
`BlightPyreDamageInHand` (`186180e9-94e9-47d0-b69c-865b9a6d27a4`), making
22 cards. Their native permanent upgrades are recorded and empty. These
start-of-run callbacks have no battle callback; the model retains actual
captured card state and rejects altered effect signatures.

`full-battle-native-covenant-startup-05.mt2f` comes from
`full-battle-no-cards-20261023-112844-2d35a16a`. All five original owners and
the complete startup callback chain are checked. Seven EndTurns win with
Pyre5; fourteen train stages, 57 room stages, four pure native stat queries,
seven authored wave definitions and sixteen immutable full chains match.
Native runtime is 59.44 seconds, normal exit, protected files unchanged and
all four failure counters zero.

Actual player advice and fresh replay both win with Pyre42: 33 inputs each
(26 card plays, seven EndTurns), 35 source publications and pure captures
426/116. UI interactions, all complete forecasts, roots, successors and RNG
match. Native runtimes are 359.69/173.48 seconds, normal exits, protected files
unchanged and all four failure counters zero. Profiles have suffixes
`113016-6733b758` / `113618-67811406`, prefixed
`full-battle-units-spells-and-junk-20261023-`; player archive prefix is
`full-battle-player-native-covenant-startup-05` with `-search.mt2f` /
`-replay.mt2f`.

## Native cumulative ranks six through ten

All original cumulative definitions now retain their exact identities and
complete ordered native effect signatures. Rank six only modifies reward
drafts (`IAddBonusCardToCardDrafts`); the resulting native deck remains
authoritative. Rank seven's sealed effect marks wave messages; rank-based
wave selection is captured through original `GetUnlockedWaves` and
`GetCharacters`, including nine authored definitions instead of seven.
Rank eight's fourteen original stat effects use the pure native birth query.
Rank nine executes one original `RelicEffectDamagePyreStartOfRun`: 30 percent
of 80 is 24, leaving 56 HP and the same 80 max HP, with all sixteen RNG
streams unchanged. Rank ten has no effect callbacks; its native conditional
wave definitions are retained. Altered effect signatures remain rejected.

All five complete first battles retain 22 starting cards and the original
purge/add/add/add callback chain. Each native run has normal exit, protected
files unchanged and all four failure counters zero; seven complete turns,
fourteen train stages and sixteen immutable full chains match. Every native
stat query preserves frame/RNG and rejects spoofed origins. Archives have
prefix `full-battle-native-covenant-startup-` and two-digit rank suffix.
Profiles below have prefix `full-battle-no-cards-20261023-`.

| Rank | Profile suffix | Native seconds | Room stages | No-card outcome / Pyre |
| --- | --- | ---: | ---: | --- |
| 6 | `113412-6c6ed098` | 60.29 | 57 | win / 5 |
| 7 | `113520-81327c5b` | 60.96 | 63 | loss / 0 |
| 8 | `113630-13d6e234` | 62.39 | 63 | loss / 0 |
| 9 | `113740-ada1ea9b` | 61.13 | 60 | loss / 0 |
| 10 | `113849-299e686c` | 55.23 | 60 | loss / 0 |

Use `scripts/Check-NativeCovenantStartup.ps1 -Rank 8 -SkipBuild` for one
baseline, or add `-Player` for an accepted source/replay pair (currently
ranks 1–8 and 10). The default baseline ranks are 1–10; this runs only the selected
startup acceptance, never the full fixture suite. Higher-rank actual player
advice, flying-boss compositions, earned unlocks and full campaigns remain
separate acceptance targets.

Highest cumulative rank ten now also passes actual player advice and fresh
frozen replay: both win with Pyre4 after 33 inputs (26 card plays and seven
EndTurns). All 35 source publications, UI interactions, complete forecasts,
roots, successors and RNG match; player captures are 435/114 with zero purity
failures. Native runtimes are 334.50/163.42 seconds, both normal exits with
protected files unchanged and all four failure counters zero. Profile
suffixes are `114040-65f9c3da` / `114617-b144a789`, prefixed
`full-battle-units-spells-and-junk-20261023-`; player archive prefix is
`full-battle-player-native-covenant-startup-10` with `-search.mt2f` /
`-replay.mt2f`. Run `Check-NativeCovenantStartup.ps1 -Rank 10 -Player
-SkipBuild`. This proves all ten original cumulative owners coexist in the
actual first-battle player path; rank nine's individual player pair, flying
bosses, full campaigns and earned unlocks remain separate.

Rank six's actual player advice and independent frozen native replay also
win with Pyre42: thirty-three inputs each (twenty-six plays, seven EndTurns),
thirty-five source publications, UI interaction and every complete forecast,
root, successor and RNG match. Both have sixty-seven room and fourteen train
stages, normal native exits, protected files unchanged and all failure/purity
counters zero. Captures are 428/116; native runtimes are 341.28/159.64 seconds.
Profile suffixes are `122735-f87b0e95` / `123318-12d495f0` (same player prefix
above); archive prefix is `full-battle-player-native-covenant-startup-06`.
Run `Check-NativeCovenantStartup.ps1 -Rank 6 -Player -SkipBuild`.

Ranks seven/eight now also pass individual player source/replay pairs.
Each performs thirty-three inputs (twenty-six plays, seven EndTurns), with
thirty-five source publications and fourteen train phases. UI interaction,
every full forecast, root, successor and RNG matches; native exits are normal,
protected files unchanged and all four native/purity failure counters zero.
Both source/replay games win. Profile prefix is the same player prefix above.

| Rank | Source suffix | Replay suffix | Native seconds source/replay | Captures | Room stages | Pyre |
| --- | --- | --- | --- | --- | ---: | ---: |
| 7 | `124150-b6d8bfbe` | `124737-3fa94f13` | 344.81/176.53 | 418/114 | 65 | 32 |
| 8 | `123621-241decd9` | `124200-1973f9fd` | 338.66/175.26 | 424/114 | 66 | 28 |

Archives use `full-battle-player-native-covenant-startup-07` / `-08`, each
with search/replay suffixes; select their rank with the same focused script.
