# Bounded multi-turn planner

`BattlePlanner` returns a model-verified executable recommendation under node,
time, beam-width and per-turn action budgets. It seeds a complete no-card
continuation and the existing units/spells/junk policy, and completes promising
branches to the requested horizon early. The fixed-policy seed uses the cached
enumeration successors; the beam can reuse the seed's frontiers. Both seeds
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

Initial focused checks pass all five cases. The ordinary turn-two starter
battle improves from Pyre 74 to Pyre 79 at the three-EndTurn horizon (scores
-73884.4 and -78898.9). This is a model result awaiting a fresh native execution
of that selected alternative. The remaining cases preserve or improve their
no-card model score; small ordering-score improvements are not HP gains.

After seeding the existing fixed policy, the ordinary offline case reaches
Pyre 80 at the same horizon within the three-second budget, with score
-79919.3 and eleven steps. Its completed fixed-policy seed has the same score.
This supersedes the initial Pyre-79 model-only recommendation above. It proves
preservation of this baseline, not a strict improvement over it. The assertion
also requires every result to retain any completed fixed-policy seed's score.

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

The first curated pair is `full-battle-planner-starter-search.mt2f` and
`full-battle-planner-starter-replay.mt2f`, captured in fresh native processes
`20261021-075814-4e336ca7` and `20261021-080017-8371869d` respectively. Both use
the ordinary unmodified starter run, physical spawn points, and the original
units/spells/junk prefix policy. The root is turn two; the three-EndTurn
recommendation is four frozen steps. It ends the planning window at Pyre 75,
versus Pyre 74 for immediately ending those turns. Both full battles subsequently
end in settled wins with all four capture counters zero. The second process
loads the first binary trace and searches zero nodes; its root, complete action
sequence, every intermediate native state, final window state and RNG match.

The new histories exposed a native `CardManager.ClearCards` terminal lifecycle:
both direct-hand-play history lists clear alongside the piles. The model now
clears them at its existing terminal-card cleanup, preserving null legacy
metadata and the separate CardStatistics play history. The initial failing
capture is not curated. Its seven EndTurns pass after this implementation
fix; the refreshed native pair has zero captured mismatches, without changing
comparison exclusions. The original Lunar Echo focused chain also passes.

Reproduce the independent second run with:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -SkipBuild -PhysicalSpawnPoints -Policy units-spells-and-junk -Planner -PlannerInput tests/fixtures/full-battle-planner-starter-search.mt2f
dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --planner-pair-only tests/fixtures/full-battle-planner-starter-search.mt2f tests/fixtures/full-battle-planner-starter-replay.mt2f
```

The native three-second search can choose a different plan than the offline
three-second benchmark because their runtimes and CPU contention differ.
The frozen sequence, rather than rerunning a time-bounded search, establishes
cross-process reproducibility. Gains above compare the no-card continuation;
they do not establish improvement over the existing fixed play policy.
