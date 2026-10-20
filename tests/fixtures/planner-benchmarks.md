# Bounded multi-turn planner

`BattlePlanner` returns a model-verified executable recommendation under node,
time, beam-width and per-turn action budgets. It seeds a complete no-card
continuation and completes promising branches to the requested horizon early.
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

Run only these checks with:

```powershell
pwsh -NoProfile -File scripts/Check-BattlePlanner.ps1
```

The full state includes the captured deck order and RNG: this is deterministic
planning with known captured state, not a probability policy for hidden draws.
Beam pruning, action caps, budgets and unsupported frontiers prevent any claim
of global optimality. Existing exact-frontier `BattleSearchModel` checks retain
their separate entry point and semantics. Native execution of selected plans
is the next acceptance gate.
