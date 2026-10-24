# Exact deterministic battle search

`ExactBattleSolver` traverses the complete enumerated model frontier with one
shared node/time budget. It has no beam width, per-turn action cap, heuristic
pruning or child-budget allocation. Iterative traversal avoids recursive stack
growth; full-state keys retain identities, histories and every captured RNG.
Transpositions include the remaining turn horizon when one is specified.

The result separates three questions:

| Result | Meaning |
| --- | --- |
| `CertifiedTerminalOptimal` | Every reachable enumerated branch terminates, with no unsupported transition, budget cutoff or horizon leaf. The returned terminal score is minimal. |
| `CertifiedHorizonOptimal` | Every branch up to the requested EndTurn horizon is explored, including all within-turn action sequences, and at least one horizon leaf remains. |
| Neither | A node/time/cancellation cutoff, unsupported transition, or absence of terminal/horizon leaves prevents a certificate. The returned path is still independently replayable. |

Certificates apply to the deterministic **model action space** and captured
deck/RNG. They do not certify that the native action enumerator is complete or
that a bounded recommendation is globally optimal for the native game. A score
function must depend only on state/outcome, rather than the path to that state.

The accepted Holdover ring-seven victory supplies unchanged native roots:
its last EndTurn has no remaining legal plays and reproduces the native terminal
state/RNG with a terminal-optimal certificate. Its last card decision has ten
legal actions; exhaustive one-EndTurn search expands 36 states and visits 30
terminal and six horizon leaves. It retains a winning route with Pyre104 or
better and correctly reports horizon optimality. The six unresolved continuations
prevent a whole-battle certificate. Tests also reject certificates after missing
definitions, node/time exhaustion and cancellation, and distinguish a zero-turn
horizon from a terminal result. Every selected plan is replayed from its root.

The horizon-optimal choice omits the original final card play. Appending its
EndTurn to the first 40 original inputs yields a shortened 41-step plan.
`full-battle-progressed-ring7-holdover-exact-replay.mt2f` retains its fresh native
win with 32 plays/nine EndTurns, Pyre104, complete state/RNG equality, four zero
failure counters and unchanged input/protected files. Its source process is
`20261021-182647-a9f9b1fd`; the native run takes 174.05 seconds. This archive is
native evidence of the chosen route, separate from the search certificate's
one-EndTurn scope. See [Holdover evidence](../cards/holdover.md).

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --exact-battle-only tests/fixtures/campaign/progression/full-battle-progressed-ring7-holdover-win.mt2f

# Native archive or an external request with a Root property; output is a model
# prediction with independent full-state/RNG replay, not a new native witness.
# Arguments: input, new output, maximum nodes, milliseconds, optional EndTurns.
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll solve-exact tests/fixtures/campaign/progression/full-battle-progressed-ring7-holdover-win.mt2f .probe-runs/exact-ring7.mt2f 10000 30000 2
```

The interactive advisor continues to use its bounded planner by default.
Exhaustive search is an explicit offline operation because full deployment
frontiers can be much larger than a near-terminal certificate.
