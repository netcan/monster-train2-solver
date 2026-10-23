# Ordinary Covenant enemy birth stats

Ordinary Covenant 1 waves were deliberately blocked because their enemy stats
were not projected at birth. Pure native stat capture now separates authored
attack/health/Horde bases from the Covenant adjustment, just as the calibrated
true-final-boss path does. The player gate permits original 01 and 00+01;
unknown definitions and additional uncalibrated Covenants retain their guards.
The production helper only reads native preview stats; it never activates a
Covenant or repeats startup deck effects.

| Archive | Original definitions | Native source (prefix `full-battle-no-cards-`) |
| --- | --- | --- |
| `full-battle-covenant-enemy-stats-01.mt2f` | 01 | `20261023-092021-5f7f321f` |
| `full-battle-covenant-enemy-stats-00-01.mt2f` | 00+01 | `20261023-092021-5a36989e` |

Each isolated fresh first battle activates the unchanged original definitions
after startup. The evidence discloses the full owners/effects, activation order,
unchanged starting deck and all sixteen RNG streams. Startup effects are not
reapplied; these controls do not claim a naturally initialized Covenant campaign.

Four unique pure native previews cover six authored wave definitions. The heavy
receives +15 HP; the original first-battle boss receives +1 attack and +180 HP.
Each preview preserves the frame and all sixteen RNG streams. Missing/spoofed
Covenant origins remain rejected by the immutable model.

Both complete native no-cards victories with Pyre10: seven EndTurns, fourteen
train stages, 57 room stages, complete context/state/RNG and sixteen independent
immutable full chains per archive match. Native processes exit normally with
protected files unchanged and capture/mismatch/unsupported/pending all zero.
The retained all-four true-final-boss full-battle control also passes the shared
wave checker after this extension.

Run `ModelChecks --covenant-enemy-stats-only <archive>` for a focused complete
battle check.

The actual separate Advisor assembly also passes both original contexts. Each
source executes 28 inputs (21 card plays and seven EndTurns), publishes thirty
advice updates and passes UI rendering, pointer routing, pause/resume,
hide/cancel/show, refresh and drag/clamp interactions. The two fresh native
processes execute their respective frozen inputs with no searches or advice
publications. Every input, root, successor, complete forecast, terminal state
and RNG matches. All four processes exit normally with settled effects,
protected files unchanged and capture/mismatch/unsupported/pending zero.

| Context | Source pure captures | Replay pure captures | Final Pyre in both processes |
| --- | --- | --- | --- |
| 01 | 327 | 94 | 60 |
| 00+01 | 331 | 98 | 52 |

Source/fresh replay archive prefixes are
`full-battle-player-covenant-enemy-stats` and
`full-battle-player-covenant-enemy-stats-ftue`, with `-search.mt2f` and
`-replay.mt2f` suffixes. Native sources, with prefix
`full-battle-units-spells-and-junk-`, are:

| Context | Source | Fresh replay |
| --- | --- | --- |
| 01 | `20261023-092136-cca51518` | `20261023-092521-537c8d79` |
| 00+01 | `20261023-092136-7e6f9845` | `20261023-092521-5626a6fb` |

All 56 source inputs and the corresponding 56 frozen replay inputs are checked
through `scripts/Check-PlayerAdvisor.ps1 -Case CovenantEnemyStats,CovenantEnemyStatsFtue -SkipBuild`.
Advice uses three turns, a 3,000-ms soft search budget and tactical candidates;
these victories do not establish a hard response deadline or default-budget
quality. Native source runs take 203.77/206.47 seconds and fresh replay runs
101.81/103.26 seconds, including the full game and capture validation.

This acceptance covers the original controlled first battle. Native rank-one
startup now has separate complete-battle/player/replay evidence, and native
cumulative rank two has a no-cards control; see [native startup](native-covenant-startup.md).
The original three Ring8 variants also have separate Covenant controls;
see [outer bosses](covenant-outer-bosses.md). Higher cumulative campaigns and
other boss signatures retain their separate acceptance boundaries. Startup
deck effects are never replayed by the production capture helper.
