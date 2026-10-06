# Monster Train 2 multi-turn search probe

This is an isolated feasibility experiment, not an in-game solver. The probe
plugin lives in `src/Probe`, the copied game is in `.sandbox-game`, isolated
profiles are in `.probe-runs`, and captured Unity logs are in `results`.

## Verified on the installed build

- Game build: Steam 22787024. The probe uses BepInEx/Harmony and redirects
  `SaveDirStandalone.GetRootDirectory` before `AppManager.Awake` accesses saves.
  It now also redirects `StandaloneProdLogSystem.GetLogRootDir` before the
  game's file logger initializes. Before this additional patch, isolated probe
  runs still wrote and rotated `logfile*.log` in the original user directory;
  their non-log save files were not written.
  It accepts only a direct child of this workspace's `.probe-runs` as its data
  root. The installed game's plugin directory was not modified.
- An earlier isolation check found 40 non-log files. Its aggregate SHA-256 was
  `4FFEDB89C1DA02AB19DEC8F9D2BDAB52D5392A4A170C3F23D30B443E4B5AD0AD`
  before and after those probe runs. A fresh enumeration now finds 39 non-log
  files, none modified after July 11, 2026, and no `Probe.dll` in the installed
  game's plugin directory. The count difference was not traced, so the earlier
  aggregate is not presented as a current integrity check.
- With seed 424242, a real `Level1BattleHealers` scenario (four spawn groups)
  reached turn 2. The no-card branch is in `results/run9.log`. Runs 10 and 11
  played the first `TrainStewardShield` in room 0 before ending two turns.
- The initial state and seeded RNG streams matched between the no-card and
  card-play branches. The eight state/RNG log records in runs 10 and 11 matched
  exactly. After two turns, the no-card branch had a `HeavyT1_Basic` at 45/45;
  the card-play branch had that enemy at 37/45 and a `TrainStewardBig` at 21/25.
  Energy fell from 4 to 3 on play. Pyre HP remained 80/80 in both branches.
- The saved replay had three entries without the card and four with it. The
  additional entry is a card-play action. This demonstrates that the game can
  reproduce and compare action-dependent combat states across two turns.

## Branch rollback experiments

- A natural non-FTUE run entered `Level1BattleJunker` through the map and
  BattleIntro UI. Its replay contained `ClickBattleNode`, `StartBattle`, and
  normal in-battle actions; `InTestScenario` was false.
- In `results/run23-undo-success.log`, the probe directly played a
  `TrainStewardShield` using `CardManager.PlayCard`, then called `UndoTurn` in
  the same turn. The pre-play and post-undo signatures matched for the logged
  hand/piles, units, HP, energy, and every seeded RNG stream. Undo took 1631 ms
  in that run. Its subsequent no-card two-turn path matched the independent
  `results/run19-natural-normal-baseline.log` at all six comparable records.
- In `results/run24-restart-branch-success.log`, two no-card turns were followed
  by `RestartBattle`. Battle-start signatures matched; restart took 2878 ms.
  The second path played a Steward on turn 0 and reached the same state as the
  independent `results/run25-natural-direct-play.log` (six comparable records).
- In `results/run26-turn1-branch-success.log`, restart took 2868 ms. The probe
  repeated the no-card first turn, matched the original turn-1 state and seeded
  RNG, then played a Steward on turn 1. The continuation matched independent
  `results/run27-independent-turn1-play.log` (four comparable records). This
  demonstrates a branch at an intermediate turn using restart plus an action
  prefix, within one game process.
- `HandUI.PlayCard` proved unsuitable for this automated fixture: a one-time
  capacity dialog interrupted its timed UI coroutine. `CardManager.PlayCard`
  executes the model action and records a normal `PlayCard` replay entry.

## Native arbitrary-prefix replay

- `src/Probe/NativeReplayScenario.cs` records the original battle's replay
  entries and a state checkpoint at every quiet player decision turn. It then
  restarts the battle, uses `ReplayManager.StartPlayback` with the prebattle
  entries skipped and the suffix trimmed to a selected checkpoint, compares
  the restored state, chooses a new card play, and continues to a configurable
  horizon. `MT2_PROBE_DEPTH` and `MT2_PROBE_TARGET_TURN` are integers; the
  latter may be any turn before the horizon while the battle remains active.
- `results/run28-native-depth4-target1.log` replays one no-card EndTurn to turn
  1, restores the checkpoint, branches with a Steward, and reaches turn 4.
  `results/run29-direct-depth4-target1.log` independently performs the same
  actions from the opening hand. Their final logged state and seeded RNG agree
  after excluding only `NonDeterministic`; Pyre HP is 77/80 in both.
- `results/run33-native-card-prefix-depth4-pass.log` includes a real Steward
  card-play entry at turn 0 in the recorded prefix, then replays to turn 3,
  plays a different legal unit, and reaches turn 4. Its turn-3 checkpoint
  matched the expanded battle state and every seeded RNG stream, including
  `Chatter`. Its final state/RNG matched the independent
  `results/run34-direct-card-prefix-depth4-pass.log` exactly; Pyre HP was
  77/80. Restart took 3375 ms and the four-entry prefix took 26966 ms in that
  run. These are single-run timings, not a throughput benchmark.
- `results/run35-native-depth6-target3-pass.log` repeats the no-card prefix
  to turn 3, branches with a Steward, and continues to turn 6. Its final
  expanded state and seeded RNG match the independent
  `results/run36-direct-depth6-target3-pass.log`. The original no-card path
  and the branched path both had Pyre HP 72/80 at turn 6. The three-entry
  prefix took 20398 ms in that run.
- The probe marks the isolated profile's one-time Capacity and FirstMonsterPlayed
  prompts complete before recording, so native replay can perform card plays
  without a UI tutorial dialog interrupting its coroutine.

The `native-replay` scenario defaults to normal-speed playback with
`VerificationMode.None`. The game's replay entries still wait for their
recorded counterparts, while the probe imposes its own 180-second timeout and
checks the saved entry count, turn, expanded state, and seeded RNG at the
decision checkpoint. `MT2_PROBE_DIRECT_BRANCH=1` runs an independent control
without rollback. `MT2_PROBE_SOURCE_PLAY_TURNS=0` requests a Steward play on
that original-path turn; `MT2_PROBE_BRANCH_ANY_UNIT=1` allows the branch to
play the first legal unit when no Steward is in hand. Only no-target unit plays
and EndTurn have been exercised. The isolated save-root guard still applies.

## Replay mode boundaries

- `results/run30-native-card-prefix-timeout.log`: normal-speed
  `VerificationMode.ContinueRun` timed out after six seconds on a later
  EndTurn, even though the preceding card play and EndTurn were recorded.
- `results/run31-native-card-prefix-fast-rng-mismatch.log`: 100x replay
  avoided that timeout and reproduced the full expanded battle state at turn
  3, but `RngId.Chatter` differed. In this installed build that stream is used
  for voice lines and idle expressions, so fast playback changes presentation
  RNG even when combat RNG agrees.
- `results/run32-verify-abandons-run.log`: `VerificationMode.Verify` gives a
  longer timeout but appends `AbandonRun` and `GameOver` to a truncated replay.
  It cannot be used to stop at a playable branch node.

## Pruning and parallel search

- In-process combat simulation is not thread-safe. `AllGameManagers.Instance`
  and `RandomManager` are static, `HadesRNG` exchanges the process-global Unity
  RNG state, and battle/replay/restart progress through Unity `MonoBehaviour`
  and coroutines. Worker threads can rank candidates or hash immutable copies
  of decision data, but game actions and replay must stay on that process's
  main thread. Parallel simulation needs separate game processes or a new pure
  combat simulator.
- The probe's game-log redirect was tested in `.probe-runs/log-isolation-check`:
  `logfile.log` appeared inside that isolated profile, the run passed, and
  hashes plus timestamps of all original `logfile*.log` files stayed unchanged.
  Parallel workers also use different game copies so BepInEx logs and assembly
  caches are separate, in addition to separate save roots and Unity log paths.
- `scripts/Measure-ParallelWorkers.ps1` runs two equal branch evaluations
  serially or in parallel on this 8-core/16-thread machine. For a two-turn
  direct path, one serial pair took 75.07 s versus 34.48 s in parallel. For
  the same path with battle restart and native prefix replay, the serial pair
  took 91.41 s versus 47.45 s in parallel (1.93x throughput). All eight
  workers exited successfully; each pair had matching expanded final state
  and gameplay RNG, and none changed the original game logs. Peak working set
  was about 1.18-1.21 GiB per process. Direct-path results are in
  `results/benchmark-{serial,parallel}-*.json`; replay results are in
  `results/benchmark-replay-{serial,parallel}-*.json`. These are single
  measurements, not scaling forecasts beyond two workers. The benchmark
  starts a fresh process per candidate; a persistent worker queue is not yet
  implemented.
- For a final-Pyre-HP objective, safe initial pruning is fully contextual
  action-legality filtering, duplicate elimination for byte-identical replay
  prefixes from the same deterministic start and RNG state, and an incumbent
  bound based on a *proven*
  upper limit for recoverable final HP. A completed full-HP win reaches the
  fixed-max-HP optimum when there is no secondary objective.
- Current HP is not a universal optimistic bound: cards and relics can heal
  the Pyre, raise its maximum HP, and resurrect it. HP also affects some draw
  and room-capacity effects. Exact state-transposition pruning is likewise
  premature because the present digest omits `CardStatistics`, relic internals,
  delayed actions, and identity-sensitive duplicate cards. Beam search or
  heuristic ordering is possible but does not guarantee the best result.

## First independent model slice

`src/Model` is a pure .NET Standard library with no Unity or game assembly
references. `SimpleUnitPlayModel.Apply` returns an independent child projection
for one supported unit play, leaving the parent intact. Cards get probe-local
reference identities, so two identical Steward cards remain distinct branches.
`src/ModelChecks` checks parent isolation, parallel child expansion, identity,
and unsupported actions without starting the game.

The native differential probe is deliberately narrower than a battle solver:
on seed 424242, natural `Level1BattleJunker`, turn 0, it models one unupgraded
`TrainStewardShield` play into an empty room with no relics or extra triggers.
It reads the unit definition and dynamic cost before the play, predicts the
new energy, ordered card piles, spawned unit, room capacity, resources, and
gameplay RNG, then compares those fields with the native game at the next quiet
decision point. The played unit card has left the visible piles at that point;
the room's enemy `nextSpawn` cursor does not change on player summon. Unknown
effects, upgrades, relics, or other starting states return `Unsupported`.

Run `pwsh -NoProfile -File scripts/Run-ModelProbe.ps1` for the isolated native
check and `dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release`
for the pure branch check. Each native run writes structured before, predicted,
and actual projections plus the game module MVID to its own
`.probe-runs/model-unit-play-*/model-unit-play.json`. A `MODEL-PASS` means this
explicit projection matched, not that unobserved game state or EndTurn behavior
was simulated. Room combat, train movement, card cycling, and the integer RNG
now have independent implementations and native differential checks; their
coverage and remaining full-battle requirements are in `SIMULATION.md`.

## Complete native battle oracle and independent combat components

`scripts/Run-FullBattleProbe.ps1` continues the natural seed-424242 battle to a
terminal win/loss instead of stopping at a turn horizon. The Steward-once and
no-card policies both reached victory at Pyre health 49/80. Their JSON fixtures
are in `results/full-battle-{steward-once,no-cards}.json`.

The latest Steward run has 55 room stages, 13 card-cycle operations, 14
train combat/movement phases, 11 spawning operations and seven EndTurns.
The independent model matches all of them, including card generation and
treasure escape. The no-card control matches the corresponding 52 room stages
and the same counts for all other phases. Both fixtures have zero differences
and zero unsupported transitions.

`RoomCombatModel` supports ordered normal/ambush initiative, multistrike
retargeting, sweep/sniper selection, armor/shield absorption, daze, stealth,
fragile, spikes, lifesteal, basic rage/sap and regeneration/poison processing,
status decay, relentless, and exact cycle detection within its supported
trigger-free room state. Most of those mechanics currently have independent
rule checks; the native fixture only verifies the mechanics it actually uses.
`TrainCombatModel` applies top-to-bottom room combat and enemy movement, with
same-turn Pyre combat on arrival. `UnityRng` reproduces 768 sampled native
integer draws and their complete states. `CardCycleModel` reproduces reverse
hand discard, draw insertion order, and seeded reshuffling for supported cards.

Run `pwsh -NoProfile -File scripts/Check-Models.ps1` to recompute the independent
model results against captured native states without starting Unity. It also
starts from the first EndTurn input, runs the pure `BattleSimulator` to terminal
without injecting future native states, and compares every decision point. Each
full chain is repeated in 16 parallel branches to check isolation and determinism. This
completes the supported initial-battle transition chain; additional card actions,
relics, additional effects and boss state machines remain part of the full goal.

`BattleActionModel` now applies independent unit plays at arbitrary decision turns
and selected positions, enforcing energy, room capacity and spawn slots. It also
routes generated self-purging junk. `results/full-battle-units-and-junk.json`
verifies 11 plays and seven EndTurns to native victory at Pyre 60/80. The pure
policy chooses actions from its own state and matches every intermediate result,
including a mid-battle starting state and 16 parallel branches. The initial simple
Steward model above is retained as the first modeling experiment.

The policy with unit cards and the two starting targeted spells matches 21 plays,
seven EndTurns and every modeled intermediate state, finishing at Pyre 73/80.
Its fixture is `results/full-battle-units-spells-and-junk.json`. Damage, pyregel,
floor rearrange and valor are now integrated into the independent full chain;
statistic-driven effects, additional upgrades, equipment, abilities, relics and broader battle rules
still need coverage before exact pruning or arbitrary live-battle solving.

`results/full-battle-statistics.json.gz` adds native card-statistics state to the
same 21-play policy. Per-card counters and durations, mapped Any counters, floor
and subtype spawn totals, death/exhaust attribution, end-turn energy, turn-start
gold and last attack damage match through all seven turns. The independent
root-only chain, a mid-battle suffix and 16 parallel branches also pass. Battle
previews affect the native last-attack statistic; the model reproduces that
effect on copies, and the probe waits for a completed preview before capturing
a quiet decision. Other statistic-driven mechanics remain outside this fixture.

`results/full-battle-numeric-upgrades.json.gz` includes every owned card's ordered
permanent/temporary modifiers and play history. Different upgrades on two
identical Stewards and one damage spell match all 19 plays, six EndTurns, 51 room
stages, 11 card cycles, 11 train phases and nine spawns, finishing at Pyre 80/80.
The root-only independent policy, mid-battle suffix and 16 parallel branches
also pass with zero unsupported stages or differences. Temporary upgrades
marked RemoveOnDiscard are removed from their source card after play or hand
discard, while already spawned units keep the applied stats and statuses.
The separate modifier calibration covers 256 native calculations across eight
scalar fields, including ordered immediate clamps and permanent/temporary group
differences. Trait/trigger upgrades, abilities, persistent health and equipment
interactions remain outside this fixture.

## Limits

- Rollback uses full scene reload and replay. It is not an in-memory clone,
  and observed resets and replays take seconds to tens of seconds in this
  fixture. Exhaustive branching at each card or turn would be too slow without
  aggressive pruning, parallel isolated game instances, or a faster combat
  simulator.
- `SaveData.CreatePreviewCopy` serializes part of the save but reuses references
  to deck and other collections. It is not a safe full branch snapshot by
  itself. Combat also depends on managers, active coroutines, triggers, and RNG
  streams outside that save object.
- `SaveManager.UndoTurn` removes current-turn actions; it does not undo a whole
  completed turn. The older `results/run12-undo-failed.log` used a cheat-started
  test battle with a non-persistent scenario and no `StartBattle` replay entry.
  On reload it returned to the map. The natural-battle tests above supersede
  that fixture for rollback feasibility.
- The expanded digest includes card pile order and upgrades, unit statuses and
  equipment, room capacity/corruption/attachments, next spawn phase, resources,
  and RNG streams. It still omits some private `CardStatistics`, delayed effect
  queues, relic-specific state, and distinct identities of identical cards.
  `CardState.GetID()` is the card-data ID, not a per-instance UUID. Thus a
  digest match is strong evidence for these fixtures, not proof of equivalence
  for all cards and relics.
- Earlier replay benchmarks measured six turns and the two alternatives
  happened to tie at 72/80. The native rollback probe does not optimize
  a score, keep worker processes alive across candidates, or solve an arbitrary
  live player's battle.

## Next engineering gate

Expand the pure model's supported action enumeration to additional spells, abilities, and follow-up
choices; capture their replay entries at quiet decision points. A worker queue
should assign unique prefixes to persistent isolated game processes, rank
candidates using immutable snapshots, and collect complete-battle scores.
Measure dozens of different branches from a real mid-run hand before choosing
worker count, search horizon, and any stronger conditional HP bounds. The
present results establish parameterized rollback and two-process parallel
throughput, but not a production solver.
