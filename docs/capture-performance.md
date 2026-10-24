# Native capture profile · 2026-10-24

The main capture cost is repeated card/context projection. An isolated frozen
replay of the real0.2.7 player records21 captures on Unity thread1. After excluding
the first instrumented/JIT warmup sample and one sample with GC, the remaining19
average51.99ms (37.83-66.44ms). This is capture latency, not a game FPS benchmark.

| Warm capture component | Average | Scope |
| --- | ---: | --- |
| Card modifiers, definition resolution and native cost/statistic checks | 28.89ms | Exclusive time, excluding the separately measured masks below |
| Owned card mask construction | 9.16ms | Exclusive time, average169 calls per capture |
| Remaining measured work and capture overhead | 13.94ms | Remainder of the51.99ms capture |

The call graph rebuilds global `CombatContext` five times: once per room in
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

The second issue is retained diagnostic history. `CardBranchMaskProbe.Enabled`
is true in the player, and `Observe` appends a context and native calibration
views to a static list without a bound. Each measured capture adds five entries;
the run grows from0 to105 retained entries. It costs about0.97ms per warm capture
including its child work, but also retains the old context graphs. A later sample
takes136.96ms and all three `GC.CollectionCount` counters increase. That establishes
a GC-overlapping latency spike, not proof that this list alone triggered the GC.
The initial945.52ms sample includes profiling/Harmony/JIT warmup and GC and must
not be presented as the normal player startup capture cost.

Priorities for an implementation change:

1. Discover all card/unit references first, then build one shared immutable
   global context per settled capture. Preserve identity closure and snapshot
   ordering; blindly caching the first room's context can miss later discoveries.
2. Reuse card-instance projections within that capture, and cache immutable
   definition metadata at an appropriate battle/version boundary. Native dynamic
   costs/upgrades still require invalidation or recapture when they change.
3. Keep development calibration history in the isolated probe, while retaining
   the mask metadata the player model actually needs.

This diagnosis adds opt-in development profiling only. It does not change or
install player capture behavior. No allocation byte claim is available: this
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
