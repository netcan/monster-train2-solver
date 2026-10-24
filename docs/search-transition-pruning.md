# Search transition reuse and validated input pruning

The 0.2.16 planner removes repeated simulation without changing its evaluator,
beam width, logical node budget or action order. Every search worker owns a
short-lived workspace; no cache persists between observations or is shared
with another worker. Source states match by reference identity. Distinct RNG,
history, selection, allocator and card identity states cannot collide.

The workspace reuses simple card/ability transitions, complete EndTurns,
candidate enumerations and deterministic policy selections. All seven simple
action inputs participate in the cache key, including the ability's origin
room and destination spawn point. Complex recursion and mixing selections
keep the full model path because their ordered payloads need separate keys.
At most 256 simple action results are retained per visited source state.

Known illegal input families skip repeated validation after a representative
has received the exact expected rejection from the complete model. Families
include the card instance, room, target team and rejection. Disabled rooms,
monster deployment in the Pyre room, invalid unit targets and excluded target
teams use this path. If an earlier unmodeled interaction or modifier produces
another result, no family is cached. Energy, discounts, Pyre payment, target
effects, card equivalence and commutative action orders receive no additional
pruning. The public uncached action model remains the replay oracle.

`BattlePlanResult.SearchMetrics`, native publication evidence and `POJU-ADVICE`
logs now record actual candidate requests/simulations, cache hits, filtered
inputs, simulated/cached EndTurns and cached policy choices. Existing duplicate
state and beam-pruning counters are also logged. Reused advice reports zero
new search work.

## Controlled comparison

`--search-pruning-only` compares ordered legal candidates, unsupported
diagnostics and every resulting full state/RNG against uncached enumeration.
It checks distinct source references, complex selection bypass, ability-room
and spawn-point inputs, repeated enumeration/EndTurn reuse and parent purity.
The captured opening and the native turn-eight ordinary-budget fixture pass.

The same actual opening then runs four searches in alternating baseline,
optimized, optimized, baseline order. Each uses 128 logical nodes, beam width
eight, the tactical seed, whole-battle mode and a 60-second time ceiling.
Every returned action sequence, outcome, score and final state/RNG must match.

Initial .NET results on this machine:

| Work / duration | Uncached | Optimized |
| --- | --- | --- |
| Candidate simulations | 848 | 538 |
| EndTurn simulations | 56 | 46 |
| Mean search time, two runs each | 8.444s | 6.750s |
| Logical nodes | 128 | 128 |
| Beam-pruned states | 10 | 10 |

This removes 310 candidate simulations (36.6%) and ten EndTurn simulations
(17.9%), with about 20% lower measured latency in this comparison. The search
route is identical. Timing depends on runtime/JIT state and machine load;
some focused checks overlapped the initial baseline run. Operation counts
provide the deterministic work comparison. No general speedup or optimality
claim follows from a single opening.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --search-pruning-only tests/fixtures/advisor/live/player-live-empty-deployment-advice.mt2f tests/fixtures/planning/search/advisor-ordinary-budget-failure.mt2f
```

The same sources were also compiled into an isolated assembly and searched
the archived opening inside the running game's Mono runtime. Four searches
alternated baseline/optimized order, with 64 logical nodes and a 60-second
ceiling. All four returned the identical 32-step, seven-turn winning line,
19 Pyre HP, and equal full state/RNG. No live Unity calls, player inputs or
patches were used. Evidence:
`.probe-runs/live-advice-ui-debug/pruning-016-mono.json`.

| Mono work / duration | Uncached | Optimized |
| --- | --- | --- |
| Candidate simulations | 504 | 265 |
| EndTurn simulations | 19 | 13 |
| Mean search time, two runs each | 8.262s | 4.490s |
| Logical nodes | 64 | 64 |

This particular runtime comparison removes 47.4% of candidate simulations
and reduces latency by 45.6%. Full uncached replay validates each result,
and the archived parent remains unchanged. The conservative reference cache
does not increase the number of full-state transposition hits: both runs
have zero duplicates and zero beam pruning at this node limit. The savings
come from transition reuse and seven validated illegal-input skips, plus
25 policy cache hits in each optimized run.

## Native overlay acceptance and Windows delivery

The production 0.2.16 overlay passed the isolated native game run at
`.probe-runs/full-battle-units-spells-and-junk-20261024-195225-38ddbc33`:
100.84 seconds, native win with 73 Pyre HP, seven EndTurns, 21 actions and
59 room stages. Capture failures, mismatches, unsupported transitions and
pending records are all zero, and original player files remain unchanged.
Thirty-three reused publications, 19 scrolled publications, seven floor
switches and four duration changes pass presentation/continuation checks.
The two actual search publications have consistent transition counters and
together reuse four EndTurns and eight policy choices. Reused publications
report zero new simulation work.

Focused deployment, atomic and shorter budget, parallel/cancellation,
live moon continuation, and native floor-scroll checks pass. The exact
production Model.dll also preserves the opening's verified 10-second win.
The 1360-archive catalog passes. No full regression or macOS build was run.
`--search-frontier-only` runs the ordered candidate/state/RNG and parent
immutability checks without repeating the four search benchmarks.
That focused path also passes the captured live moon continuation's ordered
frontier and complete state/RNG comparison.

Implementation `7971c4184ee76c314231696dcd2c24a7e3f8912f` was pushed to
`origin/master`. Installed at 2026-10-24 19:55:00 in
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
All three installed files match the accepted payload:

| File | SHA256 |
| --- | --- |
| MonsterTrain2Poju.Advisor.dll | 68f442c26a8111b4e34699fb286489056da82d9bedd2ab906fb588c84e6f8915 |
| Model.dll | 0a9ee3975a0167ea56b056f12a9c770fb1b643fe20ad0fee754473136b1d41cd |
| FixtureArchive.dll | c78e8bc21a68ee3bc3e9c4b49f1c48c77edf43a9026efb58955b0d334e9d0687 |

Prior and accepted payloads are retained at
`BepInEx/PojuBackups/SearchPruning-20261024-195459`. All 28 other
loader/plugin/config hashes are unchanged. The Windows package is
`results/mod-dist/PojuAdvisor-0.2.16-Windows-20261024-195459.zip`.
The player process PID 134544 still has 0.2.14 loaded and was left running;
a normal complete exit and restart is required to activate 0.2.16.
