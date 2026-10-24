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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --search-pruning-only tests/fixtures/player-live-empty-deployment-advice.mt2f tests/fixtures/advisor-ordinary-budget-failure.mt2f
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
