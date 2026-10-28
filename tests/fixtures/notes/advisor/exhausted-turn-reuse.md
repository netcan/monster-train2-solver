# Exhausted turn continuation

The 0.2.394 isolated player search for random campaign 25's eighth battle
logged an old-plan matching cost of 5.5 seconds after a three-turn forecast had
only its final End Turn left and the native battle had advanced to the next
turn. A second such transition logged 5.8 seconds. These are individual Unity
samples, not guarantees or a general benchmark.

0.2.395 still checks the verified continuation and full captured state. When
the latest published plan consists of exactly one End Turn and the captured turn
has advanced, it skips the old selection-replay and rebase attempts and starts a
fresh search. The original matching path returns no executable continuation in
the archived native counterexample; its offline measurement was 1.27 seconds
on one run. Same-turn, multi-step and card-action controls remain eligible for
ordinary matching. Independent full plan verification is unchanged.
