# Policies for unknown draw order

`DrawOrderBelief` creates a uniform prior over the current draw pile's hidden
order. It begins from a canonical identity pool, so reversing the captured
secret order produces the same hypotheses for the same seed. Small pools are
enumerated completely; larger pools use distinct seeded samples and retain
`ExhaustiveDistribution=false`. Sampling never receives an exact certificate.
The default cap is eight hypotheses, with an explicit upper bound of 4,096.

`BattleBeliefPlanner` performs finite-horizon expectimax over these immutable
hypotheses. Indistinguishable observations must use one common legal action.
Only subsequent visible observations can branch the policy, and posterior
weights are conditional on the observation. The observation key omits draw
order and all RNG words. It retains the current hand, visible units/statuses,
known pile contents and resources. Differing legal action sets are diagnosed
and prevent a completeness certificate. Node/time/cancellation budgets are
shared across all hypothetical worlds.

`BattleBeliefReplay.Verify` independently executes every selected input from
each original world. It recomputes observations, posterior probabilities,
expected values and complete final states/RNG without reusing planner caches.
Exported policy archives can be verified again in a fresh .NET process.

The existing accepted Holdover victory supplies an unchanged native turn-seven
root with two cards in its draw pile. Its two orders receive weight 0.5 each;
two decisions expand four hypothetical states and produce two revealed
observation branches. The actual-order hypothesis also matches its original
native EndTurn state/RNG. Independent policy replay matches the weighted value,
and deliberately corrupted probabilities are rejected.
Weighted scores use compensated summation. Verification also rejects a
one-health score error at the evaluator's 1e12 win/loss scale; 4,096 unequal
weighted aliases supply a numerical control without extra battle expansions.

A separate controlled evaluator uses the accepted native deployment root,
two hidden-order hypotheses with probabilities 0.25/0.75, and two legal original
card choices. Knowing the hidden hypothesis would permit cost zero in either
world. The executable shared policy must choose before disclosure: its optimal
expected cost is 2.5. Both the shared optimum and the separate clairvoyant
controls are checked. This payoff matrix is an algorithm test, not a native
game reward or an additional original-card witness.

The exact certificate is limited to the supplied **finite distribution and
decision horizon**. The default draw-order builder conditions on captured RNG,
enemy waves and all other model inputs. It does not infer distributions for
future reshuffles or arbitrary combat randomness. Explicit weighted hypotheses
can represent those uncertainties through `BattleBelief`; their probabilities
and completeness are inputs, not inferred facts. An exhaustive result for a
small draw pile is not a native whole-game global-optimality claim.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --belief-battle-only tests/fixtures/full-battle-progressed-ring7-holdover-win.mt2f

# Input, new output, maximum worlds, decisions, world expansions, milliseconds,
# optional native Turns array index. Default input is the first native decision
# or an explicit Root request. Index seven selects the two-card native root.
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-belief tests/fixtures/full-battle-progressed-ring7-holdover-win.mt2f .probe-runs/belief-ring7.mt2f 8 2 128 30000 7
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll verify-belief .probe-runs/belief-ring7.mt2f
```

The interactive advisor keeps its bounded deterministic planner by default;
the probability-policy commands are an explicit offline interface.
