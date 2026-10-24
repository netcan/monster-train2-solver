# Native capture profile · 2026-10-24

## Allocation and idle follow-up · Windows0.2.9

State keys now reuse instance-local UTF-8/character buffers and recursive string
builders, and cache repeated scalar/string encodings within one immutable key
session. Disposal returns pooled buffers and clears retained references. No
snapshot fields or key formats are removed. Decimal scale, floating-point signed
zero, invalid Unicode fallback and independent concurrent workers retain their
previous behavior. Player played-room cache diagnostics are compiled only in
the development probe. Idle effect readiness uses cached typed field readers,
preserving the pending-effect, resolving-effect and external-input guards.

One focused .NET8 measurement of20 warm fresh-root hashes of the accepted Ring7
fixture changes from641.8ms /498,401,656 allocated bytes /31 generation-zero
collections to452.0ms /86,647,576 bytes /5 collections:82.6% fewer allocated bytes
and29.6% less elapsed time in this sample. This measures background key work,
not Unity allocations or game FPS. All12 frozen pre-optimization keys in strict,
preview-allocator and floor-browsing modes remain identical; Unicode/large-buffer
and scalar representation controls also pass.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --state-key-only tests/fixtures/full-battle-player-natural-ring7-ordinary-search.mt2f
```

## Snapshot-local native reuse · Windows0.2.8

Windows0.2.8 removes the repeated global context/card projections. Replaying
the same21 captures on Unity thread1 reduces the warm no-GC mean from51.99ms
to12.92ms (75.2%). Both runs exclude their first instrumented/JIT sample and one
later GC-overlapping sample, leaving19 samples each. Matching capture indices
and excluding GC in either run leaves18 pairs averaging52.25ms/12.02ms.
These are instrumented capture measurements, not game FPS or an end-to-end
advice response benchmark; cold/JIT and GC spikes still exist.

| Capture work | Before0.2.7 | After0.2.8 |
| --- | ---: | ---: |
| Global context constructions per decision | 5 | 1 |
| Warm capture mean | 51.99ms | 12.92ms |
| Card projection exclusive mean | 28.89ms | 3.54ms |
| Owned card mask calls per capture, mean | 169.21 | 17.53 |
| Owned card mask exclusive mean | 9.16ms | 1.11ms |
| Retained player branch-mask calibration rows after21 captures | 105 | 0 |

Canonical train capture first discovers all rooms' units and movement, then
builds one immutable `CombatContext` and attaches that same instance to every
room and the train. Its existing reference-closure loop still discovers detached
cards/actors before freezing registry counters. Separate development combat
stage captures retain their existing behavior and deferred room observations
receive the completed context.

Live card instances and registry closure reuse a local dictionary of card
projections for that context only. Native costs, masks, upgrades and interactions
are still captured/validated afresh at the next decision; nothing is cached
across inputs. Branch-mask calibration history and room query observations are
compiled only in the development probe; the player retains required model mask
metadata and its before/after native state/RNG purity checks.

Optimized evidence: `.probe-runs/full-battle-units-spells-and-junk-20261024-161325-77d88118`.
The independent frozen process wins after the same three plays/seven EndTurns
with51 Pyre HP, all four failure counters zero and protected originals unchanged.
The pair checker against the accepted0.2.7 source matches every input, root,
successor, forecast and RNG plus fourteen train phases per process. Its paused
player performs zero searches/publications and21 captures. Runtime is50.84s
including startup/driver checks. All21 profiled captures have zero retained
player branch-mask history and zero capture errors. A later GC-overlapping
capture still takes77.85ms; no zero-allocation or stutter-free claim is made.

Real0.2.8 search/UI acceptance in
`full-battle-units-spells-and-junk-20261024-161444-35fe90b5` also wins with51 Pyre
HP after three plays/seven EndTurns. All18 publications use complete fresh
roots, all nine later decisions reuse their verified winning continuation,
seven floor switches add no searches and the two-second idle control adds no
captures/publications/searches. Its independent full-state/RNG pair check with
the optimized frozen process passes. The complete archives are curated as
`full-battle-player-capture-reuse-{search,replay}.mt2f`; their hashes are in the
fixture manifest. This also registers the previously unregistered0.2.7
whole-battle pair. Recheck the optimized pair without a full regression:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --parallel-planner-pair-only tests/fixtures/full-battle-player-capture-reuse-search.mt2f tests/fixtures/full-battle-player-capture-reuse-replay.mt2f
```

Fresh native cost/moon component
`full-battle-no-cards-20261024-161841-a5c0442f` and its independent
`--room-moon-spell-upgrades-only` checker pass15 boundaries,105 trait snapshots,
40 active changes, original cost-relic controls and eight immutable branches per
boundary, with originals unchanged. An attempted replay of the older October22
player cost-relic fixture is rejected at decision0 because its mask definition
lacks `UnmodifiedSpawnHealth`, added in `dab254e8` before this optimization.
That rejected run is not acceptance evidence; no state/schema comparison is
relaxed to admit it, and no fresh full player cost-relic victory is claimed.

The original diagnosis follows. An isolated frozen replay of the real0.2.7
player records21 captures on Unity thread1. After excluding
the first instrumented/JIT warmup sample and one sample with GC, the remaining19
average51.99ms (37.83-66.44ms). This is capture latency, not a game FPS benchmark.

| Warm capture component | Average | Scope |
| --- | ---: | --- |
| Card modifiers, definition resolution and native cost/statistic checks | 28.89ms | Exclusive time, excluding the separately measured masks below |
| Owned card mask construction | 9.16ms | Exclusive time, average169 calls per capture |
| Remaining measured work and capture overhead | 13.94ms | Remainder of the51.99ms capture |

The original call graph rebuilds global `CombatContext` five times: once per room in
`TrainCombatProbe.Capture`/`FullBattleTrace.Capture`, then once for the train itself.
Those five contexts together account for46.49ms on average (89.4% of the total;
inclusive of the card work above). Each context captures live card instances and
the known-card registry separately. The two passes overlap heavily and each
rebuilds modifiers/masks and checks native costs in all four rooms. There is no
snapshot-local reuse between these passes or between the five contexts.

Card rules are explicitly invalidated for each decision; however, the measured
`BattleActionProbe.CaptureRules` averages only0.86ms. The standalone before/after
native state/RNG purity check in the previous accepted0.2.7 logs costs0.2-0.5ms.
Neither is the main bottleneck in this starter-battle sample. Background search,
hash preparation and replay verification are outside these capture measurements.

The second original issue is retained diagnostic history. `CardBranchMaskProbe.Enabled`
is true in the player, and `Observe` appends a context and native calibration
views to a static list without a bound. Each measured capture adds five entries;
the run grows from0 to105 retained entries. It costs about0.97ms per warm capture
including its child work, but also retains the old context graphs. A later sample
takes136.96ms and all three `GC.CollectionCount` counters increase. That establishes
a GC-overlapping latency spike, not proof that this list alone triggered the GC.
The initial945.52ms sample includes profiling/Harmony/JIT warmup and GC and must
not be presented as the normal player startup capture cost.

The opt-in development profiler is absent from the player. No allocation byte
claim is available: this
Unity Mono build returns zero from `GC.GetAllocatedBytesForCurrentThread` even
while allocations occur; the profiler reports that value as unavailable.

Reproduce with the already built player/probe assemblies:

```powershell
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Seed 424242 -BattleDistance 0 -Policy units-spells-and-junk -PlayerAdvisor -RollingAdvice -AdvisorFloorBrowse -PlannerWholeBattle -PlannerSearchWorkers 2 -ProfileCapture -PhysicalSpawnPoints -BranchCardMasks -RollingInput tests/fixtures/full-battle-player-whole-battle-search.mt2f -SkipBuild -TimeoutSeconds 600
```

The profiler dynamically patches only the separately loaded player assembly in
the isolated development process. Its type is excluded from the player through
`DEVELOPMENT_PROBE`; its per-method timings include instrumentation overhead and
should be interpreted as approximate attribution. Exclusive times subtract nested
instrumented children; nested inclusive times must not be summed.

Evidence profile: `.probe-runs/full-battle-units-spells-and-junk-20261024-160151-869f384a`.
The frozen run wins after three plays/seven EndTurns, takes57.95s including startup
and driver checks, and has all four capture/mismatch/unsupported/pending counters
zero with protected original files unchanged. It performs zero searches and
publications after pause, isolating capture from active search work. The capture
archive SHA256 is `7970880d56b22eede9adda0d38035bd97cd764cc99f9256aafa5991f7e0cbbf0`.
