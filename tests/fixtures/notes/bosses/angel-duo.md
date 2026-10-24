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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-birth-only tests/fixtures/bosses/angel-duo/outer-boss-birth-calibration-angel-duo.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-battle-only tests/fixtures/bosses/angel-duo/full-battle-native-covenant-02-angel-duo.mt2f
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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-incant-only tests/fixtures/bosses/angel-duo/full-battle-angel-duo-incant.mt2f
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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-forced-relentless-only tests/fixtures/bosses/angel-duo/angel-duo-forced-relentless-calibration.mt2f
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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-only tests/fixtures/bosses/angel-duo/angel-duo-inner-death-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-only tests/fixtures/bosses/angel-duo/angel-duo-outer-death-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-companion-phase-only tests/fixtures/bosses/angel-duo/angel-duo-companion-phase-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-battle-only tests/fixtures/bosses/angel-duo/full-battle-angel-duo-inner-death.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-battle-only tests/fixtures/bosses/angel-duo/full-battle-angel-duo-outer-death.mt2f
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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-relentless-entry-only tests/fixtures/bosses/angel-duo/angel-duo-relentless-entry-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-paired-phases-only tests/fixtures/bosses/angel-duo/angel-duo-paired-phase-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-paired-pyre-only tests/fixtures/bosses/angel-duo/angel-duo-paired-pyre-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-death-battle-only tests/fixtures/bosses/angel-duo/full-battle-angel-duo-paired-relentless.mt2f
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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-angel-duo-pair-only tests/fixtures/bosses/angel-duo/full-battle-player-angel-duo-search.mt2f tests/fixtures/bosses/angel-duo/full-battle-player-angel-duo-replay.mt2f
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
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle tests/fixtures/bosses/angel-duo/full-battle-player-angel-duo-search.mt2f <new-output.mt2f> <room-index>
```

The player-continuation workflow also tests six turns/10000ms, configured top
preference and tactical seed off. It still loses after 44 inputs: 36 searches
and eight ordinary continuations, with every complete forecast/successor,
terminal state/RNG and immutable root independently verified. This local run
takes 468.19 seconds including verification. A speculative candidate is
rejected with `Missing dead spawner routing during the trigger queue.`; the
executed route remains supported. That guarded branch needs its own isolated
native witness before changing routing. More search time alone has not
established a winning route, and no fresh native run is spent on this loss.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-battle tests/fixtures/bosses/angel-duo/full-battle-player-angel-duo-search.mt2f <new-output.mt2f> 6 10000 2 --player-continuation
```

A second candidate uses twelve turns/10000ms, bottom preference and tactical
seed on. It also loses: 55 inputs/eleven EndTurns, 55 searches, no retained
continuations, 728.68 seconds including independent full-forecast/state/RNG
verification. Its candidate frontier rejects the same dead-spawner routing
signature. Defeat forecasts intentionally replan after every input; the player
continuation option does not retain an already losing route. Neither expanded
budget establishes a native victory or justifies repeating the native UI test.

The recorded twelve-turn scout now yields a concrete rejected branch without
another search: 23 supported inputs reach turn four, where card 14 targets
unit 10 in room zero. The target is the original Endless carrier (spawner
card 2). `extract-unsupported-branch` scans unique recorded forecast roots,
independently replays the entire prefix, reproduces the exact unsupported
action and verifies the deployment root is unchanged. Its archive is explicitly
`ModelUnsupportedBranch`; it contains no assumed native successor.

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll extract-unsupported-branch .probe-runs/angel-tactical-player-floor0-0754.mt2f <new-output.mt2f> 'Missing dead spawner routing during the trigger queue.'
```

The isolated original-checkpoint observation and a fresh repaired native replay
now close this routing gap. All 24 native inputs, complete states/RNG, returned
draw order, temporary upgrades and unchanged exhaustion statistics match.
The spell no longer tries to exhaust a carrier already returned by Endless.
Both component processes exit normally without changing protected originals;
they take 140.08/123.97 seconds. The accepted full Angel Duo source/replay and
original Endless combat-death controls still pass. See
[Endless spell routing](../cards/endless-spell-routing.md) for the two curated component
archives and independent pair gate. This repair alone is not a new boss victory.

Three additional cheap strength-policy scouts use preferred rooms 0/1/2 after
the routing repair. They lose after 50/51/56 inputs (nine/nine/eleven EndTurns).
Each frozen route independently verifies full forecasts, successors, terminal
outcome and state/RNG. They do not justify another native full-battle attempt;
the remaining issue is strategy quality beyond a fixed floor preference.

## Natural cumulative Covenant3 birth

The original rank-three route now reaches this same Incant-Health/Slay-Attack
encounter after five native victories. Input save SHA256 is
`ce146df48e0ba6082af5edd7684a00944939d1f524a6cd76064557e06304b6de`.
`campaign-native-covenant-03-ring6.zip` has SHA256
`a6a8b81a663d49fc856d11ff44859f8d3b412f2851003fb3b8d3fe6feff1bda7`;
all18 payloads verify on restore. Difficulty,seed424242,original encounter,
cards and preceding history remain unchanged,without a cheat flag.

First actual-default source `full-battle-units-spells-and-junk-20261024-095402-92b51a01`
rejects before its first recommendation: the outer-boss gate still allowed
only owners01+02 for this exact pair. Unchanged native observation
`full-battle-no-cards-20261024-095528-edbf3794` confirms the same original birth
stats; its two unsupported counters remain diagnostic,not accepted evidence.
The additional original03 owner has only merchant reroll/purge effects.

Projection and modeled birth now admit the exact ordered01+02+03 owner prefix
for `Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth`. The previously
calibrated Rage identity keeps its separate acceptance. Other uncalibrated
Angel variants/Cael,changed merchant combat effects and reordered/duplicated
owners still reject. Existing rank-two Angel and rank-three Rage birth gates
also pass. No general higher-Covenant allowance is introduced.

Fresh native `full-battle-no-cards-20261024-095745-83a13ca9` exits normally in
74.18 seconds,preserves protected originals and records all four counters zero.
The original11/1250 definition becomes14/1500 with titanskin10; directed
companion registration,battle-relic installation,complete creation/placement/
controller/RNG and sixteen immutable birth branches match. The seven-EndTurn
no-card loss also matches61 room stages,14 train phases,full successors/RNG
and sixteen independent complete chains. This calibration restores supported
planning; it does not establish a player victory or completed rank-three run.

Curated archives are `full-battle-angel-duo-natural-covenant03.mt2f` and
`outer-boss-birth-calibration-angel-duo-natural-covenant03.mt2f`. Focused gates:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-natural-birth-only tests/fixtures/bosses/angel-duo/outer-boss-birth-calibration-angel-duo-natural-covenant03.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-natural-battle-only tests/fixtures/bosses/angel-duo/full-battle-angel-duo-natural-covenant03.mt2f
```

## Natural Covenant3 default loss

Fresh player source `full-battle-units-spells-and-junk-20261024-095906-0093a502`
loads the unchanged five-win input above and now publishes actual default
advice (three turns/3000ms/top preference/tactical off). It completes46 inputs,
37 plays/abilities and nine EndTurns,then loses at Pyre0. Normal exit takes
782.72 seconds; protected originals are unchanged and all four counters are
zero. Complete native successors,all published forecasts and RNG match.
There are82 room stages,18 train phases,54 publications with54 fresh root
checks,437 pure captures and seven floor selections with no additional search.

The curated `full-battle-player-native-covenant-campaign-03-ring6-default-loss.mt2f`
and `player-native-covenant-campaign-03-ring6-default-loss-native.json` preserve
this counterexample. Its dedicated source-only gate verifies natural input,
unchanged cumulative owners,actual defaults,complete defeat,player UI and
state/RNG. It claims neither a victory nor an independent native pair.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-native-covenant-default-loss-only tests/fixtures/campaign/covenants/full-battle-player-native-covenant-campaign-03-ring6-default-loss.mt2f 3 5
```

Three strength-policy candidates at floors0/1/2 lose after53/55/56 inputs.
A six-turn/10000ms bottom/tactical player-continuation candidate also loses
after57 inputs (33 searches,24 ordinary reuses,446.48 seconds). Each offline
candidate independently reproduces all frozen successors/full forecasts,
terminal outcome and complete state/RNG. These are model diagnostics; no
extra native attempts are spent on them. More time or a fixed floor has not
established a solution for this deck.

The continuous default driver stops before replay/rewards on defeat. Future
runs additionally emit `player-default-loss.json` with the checked input and
trace hashes,then append a `default-loss` journal entry retaining the next
input profile. Only accepted victory pairs can advance the campaign. These
receipt changes are syntax-checked; the archived loss predates them and is
not described as a newly generated receipt.
