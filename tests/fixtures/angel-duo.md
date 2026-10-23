# Original Angel Duo: native mechanisms and player advice

The input is the unmodified native cumulative rank-two Ring6 checkpoint in
`campaign-native-covenant-02-ring6.zip`: seed424242, five preceding original
victories, 34 cards and Pyre59. Its history is a separate fixed-policy branch
from the accepted Ring2 player victory; the two branches are not one playthrough.

The exact encounter is `Level6AngelDuo_RallyAttackIncantHealth`. The outer
`Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth` retains authored attack11,
HP1250 and titanskin10. The native cumulative owners add attack3/HP250. The
inner `Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack` has attack16/HP1350,
multistrike1 and pyrebane1 after those same native adjustments.

Creation already points the outer actor at its companion, but registration
updates the inner actor's reciprocal pair and disables individual boss-death
victory. The original battle relic installs the outer Incant health trigger
only after registration. Non-Covenant character-added notifications and the
shared aura world must retain the newly installed triggers.

The no-card battle exercises ordinary paired movement, enemy spawning, retained
auras, complete turn/room/train state and RNG, including defeat at the Pyre.
The companion retains its pending aura preview actions across UI restoration.
Capture collects each floor separately: native `AddCharactersToList` sorts its
entire destination by local spawn-point index, so a shared cross-floor list
would corrupt the companion roster ordering.

Accepted native source: `full-battle-no-cards-20261023-143545-75219401`,
79.25 seconds, normal exit and protected files unchanged. Final persisted
acceptance has capture/mismatch/unsupported/pending all zero. Its six EndTurns,
51 room stages and 12 train stages match the model. Archives:
`full-battle-native-covenant-02-angel-duo.mt2f` (58102 bytes, 7676 nodes,
SHA256 `c080782e6444052e5fa15110d7c6fae56620cc5ff55033bcdaaa80e818cfb85f`)
and `outer-boss-birth-calibration-angel-duo.mt2f` (33521 bytes, 3579 nodes,
SHA256 `3c6a610be3fd3d2bb25b29f89ef43652d742e1e0d0da2089c83f619b2e853234`).

Focused commands:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-birth-only tests/fixtures/outer-boss-birth-calibration-angel-duo.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-battle-only tests/fixtures/full-battle-native-covenant-02-angel-duo.mt2f
```

These checks include sixteen independent immutable birth and complete battle
branches. A no-card loss does not establish player victory or forced Relentless
after companion death. This boss is not yet enabled in player outer-boss capture.

## Original cross-floor Incant and Slay

`full-battle-angel-duo-incant.mt2f` preserves a separate fixed-policy run from
the same original input. Only legal native spell selection is prioritized;
the deck, units and authored effects remain unchanged. Five original spells
cast on the outer boss's floor heal the remote Savage companion by30 and
increase its maximum by30 each time. The outer boss's maximum is unchanged.
An independent native exchange also witnesses the inner boss's original Slay
granting its remote Entropic companion +3 attack.

Triggered health applies in the target's physical room. Temporary kill previews
track the exact copied actors, including the attacking actor and any paired
boss on another floor, while other actors on that floor remain primary.
Aura updates copy shared effect caches back into borrowed actors before the
room's final synchronization. They preserve those actors' current numeric
state, including real Pyre damage.

Accepted source: `full-battle-units-spells-and-junk-20261023-150213-246ae9df`,
296.42 seconds, native loss, normal exit and protected files unchanged. Final
persisted acceptance has all four failure counters zero. All28 card plays,
seven EndTurns, 64 room stages, 14 train stages and complete state/RNG match.
The focused check also runs sixteen immutable branches per positive cast and
sixteen complete recorded chains. The archive is94013 bytes/15234 nodes,
SHA256 `38f2014f41f21c9277e9455402ca0e8a2abbbe35d5e7cdd0221bce7951fb4ff8`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-incant-only tests/fixtures/full-battle-angel-duo-incant.mt2f
```

Existing chosen-cleric Tower attack casts, aura-world primitives, the cumulative
rank-two final-boss battle and ordinary unplayed lethal Pyre previews pass their
focused checks. No full catalog regression was run for this batch.
Companion death forcing the native turn/spawn queue, companion Relentless/Pyre
phases and actual player advice with a fresh frozen replay remain separate.

## Forced Relentless global transition

`angel-duo-forced-relentless-calibration.mt2f` calls the original native
`CombatManager.ForceRelentless` at five temporary turn boundaries in this
original battle. It records complete state/RNG plus the native turn, spawn
phase and early-Relentless scoring counter. Three early calls set the turn and
spawn phase to the final wave and replace the scoring counter with the skipped
turn count. Final/post-final calls retain all counters, including a sentinel.
Turn zero also leaves deployment immediately, including the shared aura world.
The model matches all five full snapshots and eighty immutable branches.

The temporary controls restore complete original battle state/RNG before play
continues. Fresh source `full-battle-no-cards-20261023-151909-e57266e4` completes
the original no-card loss in64.41 seconds, exits normally, preserves protected
files and has final persisted acceptance with all four failure counters zero.
The component archive is33639 bytes/3588 nodes, SHA256
`7e5df0ef850f6cbf932c514b0802ea2d982f53f52074aed936f6dd03b5deb0b0`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-forced-relentless-only tests/fixtures/angel-duo-forced-relentless-calibration.mt2f
```

This calibrates the global primitive; integrated original death controls follow.

## Original companion deaths and surviving-boss continuation

Two separate original Ring6 inputs explicitly lower one boss to one HP and
apply100 native damage. Deck/assets remain original; these controls establish
mechanics, with no naturally earned victory claim. Inner death grants the
outer boss its original multistrike1; outer death grants the inner boss its
original titanskin10. Both clear directed pairing, update terminal-death
eligibility and apply the native forced-turn/spawn/scoring transition.

The readiness check now recognizes the already drawn hand across the native
turn jump. Native `HaveCardsBeenDrawnForCurrentTurn` becomes false because its
counter still belongs to the old turn; the focused original-scenario check uses
the captured skipped-turn count while retaining other native UI/settlement gates.

| Death control | Accepted source | Seconds | EndTurns | Rooms | Train |
| --- | --- | --- | --- | --- | --- |
| Inner | `full-battle-no-cards-20261023-154316-7354ccde` | 71.26 | 5 | 39 | 10 |
| Outer | `full-battle-no-cards-20261023-155023-0fd66ef4` | 56.61 | 5 | 35 | 10 |

Both native no-card losses exit normally, preserve protected files and have
final persisted acceptance with all four failure counters zero. Their component
death archives match complete battle state/RNG and sixteen independent branches.
The full archives include the disclosed control transition: sixteen full chains
simulate the health control and native death from model-derived states before
checking every later EndTurn. Native successor states are never injected.

The surviving inner boss also matches three complete companion-phase boundaries,
including its original loop, relentless status, trigger removal, selection and
physical slots. A partial ascension compacts its destination and preserves the
unrelated source-floor slots. The original pair uses this synchronized phase
instead of the older companion phase that could restore a removed trigger.
An absent outer boss after original forced death no longer looks like a queued
boss awaiting birth, including after its Unity object finishes destruction.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-only tests/fixtures/angel-duo-inner-death-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-only tests/fixtures/angel-duo-outer-death-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-companion-phase-only tests/fixtures/angel-duo-companion-phase-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-battle-only tests/fixtures/full-battle-angel-duo-inner-death.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-battle-only tests/fixtures/full-battle-angel-duo-outer-death.mt2f
```

## Both live companions through Relentless and Pyre

Fresh source `full-battle-no-cards-20261023-160056-0c9bc1a3` keeps both original
bosses at1350/1500 HP, preserves their assets/deck and calls the original native
`ForceRelentless` during turn1. The entry archive records this explicit control
and matches the global model in complete state/RNG and sixteen branches.

The accepted no-card loss completes in64.79 seconds, exits normally, preserves
protected files and has final persisted acceptance with all four counters zero.
Its five EndTurns, 36 room stages and ten train stages match; sixteen independent
complete branches execute the native control from model-derived states.
Both live paired bosses reach the Pyre after all three ordinary floors are
destroyed. Their arrival waits for the outer boss before fighting, as native
`DoAscension` suppresses the earlier companion-only tower attack.

Eight complete pre/post boss boundaries match, including the composed companion
transition. The inner boss destroys each floor during ascension, before the
outer post phase; the outer phase therefore observes no additional newly
disabled floors. The separate terminal Pyre archive matches full battle,
physical, controller, aura and RNG state with sixteen branches. Its cached
`PlayRules.Cards` are excluded under the existing terminal-card-manager disposal
contract; room rules and runtime card state remain checked.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-relentless-entry-only tests/fixtures/angel-duo-relentless-entry-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-paired-phases-only tests/fixtures/angel-duo-paired-phase-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-paired-pyre-only tests/fixtures/angel-duo-paired-pyre-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-battle-only tests/fixtures/full-battle-angel-duo-paired-relentless.mt2f
```

## Actual player advice and independent native replay

The player capture gate now admits this exact calibrated outer definition.
Source `full-battle-units-spells-and-junk-20261023-162725-a2525f9c` and fresh
replay `full-battle-units-spells-and-junk-20261023-164238-a5515fed` resume the
same unmodified five-win native checkpoint. No health/deck/asset/death/phase
control is used. The deployment root retains the authored queued outer boss;
after the first EndTurn both original bosses have native rank-two full-health
stats and reciprocal pairing. Neither boss exists on a floor during deployment.
Artificial early placement/death remains guarded.

Both accepted processes exit normally, preserve protected files and persist
zero capture failures, mismatches, unsupported or pending records. They execute
the same 44 inputs: 36 card/ability actions and eight EndTurns, with 71 room and
16 train stages each. All roots, successors, full forecasts, RNG and the actual
player panel interactions pass the independent frozen-pair check. The source
performs actual search and publishes 46 times with 458 pure captures; replay
performs no search. Both are bounded losses with terminal Pyre 0.

The source uses a three-turn/3000ms soft search budget and tactical seed. First
response is 6.64 seconds; response median/P95 is 6.78/9.67 seconds. Native source
and replay durations are 910.26/535.90 seconds. These observations establish
advice correctness for this original encounter, not a natural victory or a
guarantee that the configured planning horizon completes within its soft budget.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-angel-duo-pair-only tests/fixtures/full-battle-player-angel-duo-search.mt2f tests/fixtures/full-battle-player-angel-duo-replay.mt2f
```

To reproduce from the restored original Ring6 profile, use
`Run-NativeCovenantPlayerPairs.ps1 -Battle Campaign -Rank 2 -BattleDistance 5
-AngelDuo -InputProfile <profile> -PlannerHorizon 3 -PlannerTimeLimitMs 3000
-TimeoutSeconds 1200`. Other campaign pairs retain their victory requirement;
the scoped Angel Duo checker explicitly records its actual outcome.

### Deployment policy diagnosis · 2026-10-24

Three focused offline runs from that unchanged player deployment root use
`FixtureTools policy-battle` with preferred rooms 2/0/1. All finish as losses,
after 44/52/48 inputs respectively; each frozen route independently reproduces
its terminal outcome and complete state/RNG. The original capture is not
changed. These are model diagnostics, not new native attempts or proofs that
the encounter cannot be won. Changing only the global preferred floor is not
an established solution; no additional native source/replay is run for these
losing candidates.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle tests/fixtures/full-battle-player-angel-duo-search.mt2f <new-output.mt2f> <room-index>
```
