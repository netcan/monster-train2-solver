# Bounded multi-turn planner

`BattlePlanner` returns a model-verified executable recommendation under node,
time, beam-width and per-turn action budgets. It seeds a complete no-card
continuation and the existing units/spells/junk policy, and completes promising
branches to the requested horizon early. The fixed-policy seed evaluates only
the hand cards needed to select its next action and reuses that action's cached
successor. Ordinary policy seeding omits unit/room ability enumeration; the
beam's full frontier still includes both. Checks compare the lazy seed's exact
choice and successor against the existing full-enumeration policy. Both seeds
consume the same planning budgets. A completed fixed-policy seed is reported
as `PolicyBaselineScore`; a null value means the seed did not finish within
the budget, not that the recommendation beat that policy.
Win precedes survival; surviving Pyre HP precedes the bounded positional
ordering term. Wins also prefer higher remaining Pyre HP.

`BattleStateKeys` hashes every public branch-state property and ordered
collection, reusing shared immutable subtrees within one planning call. It
retains histories, card/unit identities, selected room and every RNG stream.
It must not be reused with mutable objects or concurrently. Hashing is a search
optimization; full-state replay comparison remains the correctness oracle.

The five initial benchmarks reuse manifest-verified native battle captures:

| Archive | First decision at turn | EndTurn horizon |
| --- | ---: | ---: |
| full-battle-units-spells-and-junk.mt2f | 2 | 3 |
| full-battle-original-relic-batch.mt2f | 1 | 2 |
| full-battle-ability-activation.mt2f | 1 | 2 |
| full-battle-original-moon-card-upgrades.mt2f | 1 | 2 |
| full-battle-original-lunar-echo-ability.mt2f | 1 | 2 |

These are captured native scenarios, including the documented controlled
ability owners and isolated moon/Lunar setups. They are not five naturally
played campaign runs or five different Bosses. Original capture provenance
and setup boundaries remain in the main fixture README.

Each check uses 256 nodes, beam width eight and a three-second soft budget.
One model expansion is atomic, so finishing an expansion can overrun the time
budget; the result reports measured elapsed time and timeout explicitly.
The selected horizon must complete (or terminate the battle), independently
reproduce the entire model state, leave its root unchanged, encounter no
unsupported branches, and score no worse than its no-card baseline. Checks
also cover key sensitivity to energy, room, RNG and previous hand-play history,
pre-cancellation, exhausted time and node budgets, and win/survival ordering.

Focused checks pass all five cases. The ordinary turn-two offline case reaches
Pyre 80 rather than the no-card baseline's 74 at the three-EndTurn horizon,
with score -79919.3 and eleven steps. Its completed fixed-policy seed has the
same score. This proves preservation of that policy baseline, not a strict
improvement over it. Every result must retain any completed fixed-policy seed's
score. Small positional-score improvements in other cases are not HP gains.

Run only these checks with:

```powershell
pwsh -NoProfile -File scripts/Check-BattlePlanner.ps1
```

The full state includes the captured deck order and RNG: this is deterministic
planning with known captured state, not a probability policy for hidden draws.
Beam pruning, action caps, budgets and unsupported frontiers prevent any claim
of global optimality. Existing exact-frontier `BattleSearchModel` checks retain
their separate entry point and semantics. Native execution of selected plans
is checked separately below.

## Fresh native execution and independent replay

Each case has `full-battle-planner-<case>-search.mt2f` and `-replay.mt2f`.
All ten full native battles end in settled wins with CaptureFailures,
Mismatches, Unsupported and Pending equal to zero and `DEPTH-PASS` in the log.
Every independent second process loads the first binary trace, searches zero
nodes, and reproduces the root, frozen actions, every intermediate native state,
final planning-window state and RNG. It checks the frozen sequence independently
of whichever bounded-search implementation produced it.

| Case | Root turn / EndTurn horizon | Steps | Window Pyre HP / no-card HP | Search ms | Completed policy seed |
| --- | --- | ---: | --- | ---: | --- |
| starter | 2 / 3 | 12 | 80 / 74 | 3005.5 | Same score |
| relics | 1 / 2 | 4 | 80 / 80 | 3147.5 | Incomplete |
| ability | 1 / 2 | 4 | 80 / 80 | 3266.3 | Incomplete |
| moon | 3 / 2 | 2 | Win at 80 / win at 80 | 3002.2 | Incomplete |
| history | 3 / 2 | 2 | Win at 80 / win at 80 | 3101.9 | Same score |

The starter pair uses the ordinary unmodified starter run, physical spawn
points and the original units/spells/junk prefix policy. Its completed policy
seed and selected plan both score -79919.3, versus -73884.4 for no-card turns.
The relic and ability cases improve only positional scores (-80017 versus
-80016.6 and -80003.9 versus -79995.5); their incomplete fixed-policy seeds do
not support a comparison with that policy. These captures span planner
revisions; only the starter pair uses the final lazy-enumeration optimization.

Moon and history start after their required calibration prefix at turn three,
so their native roots differ from the turn-one offline benchmarks. Both frozen
plans are two EndTurns that finish the battle. The moon case retains all fifteen
required calibration plays. History retains the original Torch and a positive
Lunar Echo activation before the planning root, including its original creation
pool and hand-play history. Neither claims the planner selected an ability.

| Case | Search run suffix (20261021) | Independent replay suffix |
| --- | --- | --- |
| starter | 081309-ca4c9225 | 081410-4c551c23 |
| relics | 075814-c715be55 | 080035-834b155b |
| ability | 080146-8bf3887b | 080457-151794d6 |
| moon | 080251-e31ce535 | 080709-da5963f2 |
| history | 080707-a52ad7ef | 080924-1d66d8e8 |

The prefixes are `.probe-runs/full-battle-units-spells-and-junk-` except
ability, which uses `full-battle-units-and-junk-`. Curated byte counts, SHA-256
and node counts are in `manifest.tsv`; these binary files are the retained
evidence, independent of ignored local run directories.

The new histories exposed a native `CardManager.ClearCards` terminal lifecycle:
both direct-hand-play history lists clear alongside the piles. The model now
clears them at its existing terminal-card cleanup, preserving null legacy
metadata and the separate CardStatistics play history. The initial failing
capture is not curated. Its seven EndTurns pass after this implementation
fix; the refreshed native pair has zero captured mismatches, without changing
comparison exclusions. The original Lunar Echo focused chain also passes.

Run search, native execution, a fresh frozen replay and the pair checker with:

```powershell
pwsh -NoProfile -File scripts/Run-PlannerBenchmark.ps1 -Case starter
```

`-Case` also accepts `relics`, `ability`, `moon` and `history`. `-SkipBuild`
reuses already-built Probe and ModelChecks assemblies. To run only a fresh
native replay of a curated source, or check a saved pair without starting Unity:

```powershell
pwsh -NoProfile -File scripts/Run-PlannerBenchmark.ps1 -Case starter -SourceTrace tests/fixtures/full-battle-planner-starter-search.mt2f -SkipBuild
dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --planner-pair-only tests/fixtures/full-battle-planner-starter-search.mt2f tests/fixtures/full-battle-planner-starter-replay.mt2f
```

The runner presets reproduce the original scenario controls: `OriginalRelicBatch`
for relics; `AbilityActivation` with units-and-junk for ability;
`OriginalMoonCardUpgrades` for moon; isolated original Torch plus original Lunar
Echo ability graft with fail-fast validation for history. They use worker one
by default; `-NativeWorker 2` selects the second isolated sandbox.

The native three-second search can choose a different plan than the offline
three-second benchmark because their runtimes and CPU contention differ.
The frozen sequence establishes cross-process reproducibility. The roughly
3.00-3.27 second observed searches are soft-budget measurements, not a hard
three-second deadline. This milestone covers the probe/command-line workflow.
The subsequent [live advisor milestone](live-advisor.md) adds an interactive
native overlay, action invalidation/replanning and natural miniboss pairs.
Hidden-draw planning remains future work.
