# Native cumulative Covenant outer bosses

Original native run setup selects the cumulative rank and executes its
startup callbacks once. An explicit isolated map jump selects an unmodified
Ring8 boss scenario; route selection preserves all sixteen RNG streams. This
is a controlled later-battle acceptance with a fresh starting deck, separate
from naturally completing seven preceding battles or earning unlocks.

The boss is created with raw authored stats, placed, then receives the pure
native stat projection and complete Covenant status/trigger callbacks before
ordinary OnSpawn. Rank two adds 1000 HP; rank ten adds 3500 HP, with zero
attack adjustment in all three variants. Raw HP/attack remain 3800/35 for
AllyBuffer, 3000/30 for Fragile and 2800/20 for Corrupter. All original owners
and ordered effect signatures are retained; altered definitions and Titan
Trial compositions remain guarded. Higher cumulative true-final-boss
composition acceptance remains separate.

All six no-card battles below have normal exits, protected files unchanged,
settled terminal captures and all four failure counters zero. Each includes
a complete immutable creation/placement comparison, full native controller,
physical state, statuses/triggers, RNG and sixteen immutable full chains.
All are losses with Pyre0. Native profiles have prefix
`full-battle-no-cards-20261023-`.

| Rank | Boss | Profile suffix | Native seconds | Turns | Room stages | Wave definitions |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| 2 | AllyBuffer | `115444-7aee9375` | 58.72 | 5 | 43 | 43 |
| 2 | Fragile | `115550-a13977d3` | 45.76 | 4 | 35 | 31 |
| 2 | Corrupter | `115643-86edafe9` | 57.81 | 5 | 43 | 42 |
| 10 | AllyBuffer | `115330-2041e723` | 67.71 | 5 | 43 | 44 |
| 10 | Fragile | `115341-028cf9d3` | 54.39 | 4 | 30 | 36 |
| 10 | Corrupter | `115448-298f7adf` | 66.80 | 5 | 41 | 45 |

Battle archive prefix is `full-battle-native-covenant-outer-boss-`; birth
prefix is `outer-boss-birth-native-covenant-`, both followed by lowercase
variant and two-digit rank. Run `Check-CovenantOuterBosses.ps1 -StartingRank
10 -SkipBuild`, optionally selecting `-Variant AllyBuffer`.

Rank ten exposed the original chosen cleric's fixed tower attack Incant and
room healing. Their native iterators ignore shared copy/filter/upgrade
fields, and fixed enemy tower buffs now use the existing global target
resolver with boss exclusion. Terminal kill previews also update shared
enchantment effect objects on remote tower targets; those objects must be
restored without importing stale remote numeric state. Shared effects must
also survive successive whole-train rooms; see the focused regression and
repair in [cumulative final-boss evidence](native-covenant-final-bosses.md).
Earlier diagnostics `114437-6c672288`
(24 unsupported stages) and `114747-ceff7e4e` (four mismatches) are not curated.
After repair all 43 rooms of the latter match diagnostically, and the fresh
six runs above provide strict native acceptance. Existing rank-one original
AllyBuffer creation and full battle also pass the focused regression.

Positive tower Incant execution now has a separate unchanged-original
legal-input control: six native casts, thirty-six remote attack buffs and
six excluded bosses; complete native successors are searchable and match
sixteen immutable branches each. Its queue and remote attack-state repairs
are documented in [chosen Incant](chosen-incant-attack.md).
The repaired rank-ten AllyBuffer now also passes a fresh actual player source
and an independently launched frozen native replay. Both execute nineteen
inputs (fifteen plays, four EndTurns), thirty-three room stages and eight
train phases. Every complete forecast, root, actual successor, controller
reference and RNG matches. Both lose with Pyre0. Source/replay captures are
239/65, source publications twenty-one, UI interaction and capture purity
pass, exits are normal, protected files unchanged and all four native failure
counters zero. Native runtimes are 322.65/151.58 seconds. Profile suffixes
`124631-bad84ab3` / `125155-1b90c300` have prefix
`full-battle-units-spells-and-junk-20261023-`.

Archives use `full-battle-player-native-covenant-outer-boss-allybuffer-10`
with search/replay suffixes. These actual player runs cannot enable the
separate chosen-floor forced-input control. The curated bundle additionally
requires that independently validated positive trigger fixture. Run
`Check-CovenantOuterBosses.ps1 -StartingRank 10 -Variant AllyBuffer -Player
-SkipBuild`; its manifest checks cover all three artifacts. Other cumulative
boss player pairs remain separate; this controlled fresh-deck later battle
does not replace natural campaign history or demonstrate a winning deck.

Rank-ten Fragile also passes actual player advice and fresh independent native
replay: nineteen inputs (fifteen plays, four EndTurns), twenty-one source
publications, UI and every complete forecast/root/successor/RNG match. Both
lose with Pyre0, normal exit, protected files unchanged and all four native
failure/purity counters zero. Native runtimes are 303.99/141.41 seconds.
Both include thirty-four room stages and eight train phases; captures are
250/64. The existing enemy moon callback/conduit player pair also passes
after the queued tower-target repair, including its eight contributor
snapshots and sixteen tracked hand upgrades.
Profile suffixes are `125606-c3b50bd7` / `130112-9ffcf2f1` using the same
player profile prefix; archives use
`full-battle-player-native-covenant-outer-boss-fragile-10` and search/replay
suffixes. Select `-StartingRank 10 -Variant Fragile -Player` for a focused check.

`scripts/Run-NativeCovenantPlayerPairs.ps1` now runs missing controlled player
pairs sequentially on a selected isolated worker. For example:

```powershell
./scripts/Run-NativeCovenantPlayerPairs.ps1 -Rank 2 -Battle Outer -Variant AllyBuffer,Fragile,Corrupter -NativeWorker 2 -SkipBuild
```

Each pair waits for native process exit and protected-file verification,
checks persisted `native-accepted-summary.json` and the trace SHA-256, then
launches a new native process for frozen replay. It runs the independent
focused full-state checker and writes `player-pair-accepted.json` only after
both runs and the pair check pass. It never registers failed captures or
automatically curates archives. This full path was exercised first with
rank-ten Fragile above; `-Battle Startup` and `-Battle Final` select the
corresponding existing native paths. Use only one batch per worker.

Rank-two AllyBuffer also passes the same actual player/fresh frozen replay
acceptance: nineteen inputs (fifteen plays, four EndTurns), twenty-one source
publications, thirty-three room stages and eight train phases; complete UI,
forecasts, roots, successors, controller references and RNG match. Both lose
with Pyre0, normal native exits, protected originals unchanged and four zero
native failure/purity counters. Captures are 269/65 and native runtimes
272.48/125.76 seconds. Profile suffixes `130020-29f03403` /
`130454-71530897` use the same player prefix; archive prefix is
`full-battle-player-native-covenant-outer-boss-allybuffer-02` with
search/replay suffixes. Select `-StartingRank 2 -Variant AllyBuffer -Player`.

The rank-two Fragile and rank-ten Corrupter actual player pairs also pass
complete forecasts/root/successor/controller/RNG comparison, independent
native frozen replay and UI. All four native processes exit normally, leave
protected originals unchanged and record zero native/purity failure counters.
All lose with Pyre0; their fresh starting decks and explicit map jump remain
separate from natural campaign victory. Profiles share the player prefix above.

| Rank | Boss | Source suffix | Replay suffix | Native seconds | Inputs / plays / EndTurns | Source publications | Captures | Rooms / train phases |
| --- | --- | --- | --- | --- | --- | ---: | --- | --- |
| 2 | Fragile | `130718-7fb6b6b9` | `131145-0dfc03ed` | 266.02/122.25 | 19/15/4 | 21 | 294/68 | 40/8 |
| 10 | Corrupter | `130358-7267e4a5` | `131043-489fac1b` | 403.67/195.52 | 23/18/5 | 26 | 292/78 | 46/10 |

Archives use `full-battle-player-native-covenant-outer-boss-` followed by
lowercase variant, two-digit rank and search/replay suffixes. Rank ten's
three-variant actual player matrix is now complete; rank two only lacks
Corrupter. Select the variant/rank with the focused checker above.
