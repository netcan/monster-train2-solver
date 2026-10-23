# Advice after one candidate consumes the time budget

`advisor-atomic-budget-failure.mt2f` is a diagnostic extracted from the exact
player `UnavailableRoot` in
`full-battle-units-spells-and-junk-20261023-174148-70a2155d`. That source stopped
at turn5 of the original Loop/Resolve Angel Duo scenario after publishing34
recommendations. A first atomic candidate consumed1692ms of a1500ms soft budget;
the old planner skipped the following EndTurn and returned zero complete turns.
The failed source is not accepted as a completed battle or a player pair.

The diagnostic retains the failure text, pure-capture count, source trace hash,
exact unavailable state and original deployment state. It is43517 bytes/4452
nodes, SHA256
`8b0d5bc53a16f338b5a5d249c665e2b18d1c00d67e4299c1bfa44f59c89a6d06`.

For callers that explicitly allow shorter horizons, the planner now completes
one EndTurn after the first tactical candidate even if that candidate consumed
the soft deadline. This adds at most one atomic transition; hard node limits and
cancellation still take priority. Strict callers retain their requested horizon.
The advice verifier independently replays every step and complete state/RNG.

`ModelChecks --atomic-advice-budget-only` uses this exact native state and a
controlled delayed evaluator to reproduce the deadline independently of host
speed. It verifies a complete first-turn forecast, its independent replay,
unchanged native root, strict rejection, a one-node hard limit and cancellation
during the first candidate. Existing Fragile shorter-horizon controls are checked
separately. A fresh complete player run/replay remains required for runtime
acceptance of this change.
