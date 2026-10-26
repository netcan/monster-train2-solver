# Parallel search merge on equal scores

The single-worker planner already uses uncapped positional cost to break ties
in its public score. The portfolio merge omitted this term and instead compared
completed turns and step counts. With equally scored complete defeats it could
discard a worker's stronger continuation because another contained fewer cards.

Version 0.2.112 shares the existing positional calculation between the planner
and the portfolio merge. Completion and public score retain precedence. Winning
Pyre health, custom primary scores, node/time limits and cancellation are unchanged.
The positional value is only an ordering heuristic, not a promise of victory.

The two `parallel-planner-merge-model-*.mt2f` archives are explicitly **model
predictions requiring native validation**, not native wins. They share the full
deployment root/RNG and source SHA from random campaign 11's Pyre31 final battle.
Both use 30000ms, 4096 total nodes and two workers. The older forecast contains
seven inputs (one card, six EndTurns), positional cost 4719. The newer contains
49 inputs (43 cards, six EndTurns), cost 4489.8. Both predict PlayerDefeated and
retain public score 1e12. Runtime-dependent search paths are not asserted as a
deterministic performance comparison.

`--parallel-planner-merge-only preferred alternative` independently replays both
complete routes, checks identical source/root/RNG, and feeds these fixed candidates
to the actual portfolio selector in both orders. The old selector prefers the
seven-input route; the corrected selector retains the lower-cost route. A primary
score control ensures the secondary term cannot override the objective. These
checks consume the saved routes rather than repeating the time-limited search.

The related native default trace is
`full-battle-random-campaign-11-ring8-pyre31-release111-budget10000-loss.mt2f`:
41 inputs, 35 cards, six EndTurns, 12 train stages, zero capture/mismatch/
unsupported/pending counters and unchanged original files. It is a distinct
10000ms actual-default defeat, not either of the model candidates above.
