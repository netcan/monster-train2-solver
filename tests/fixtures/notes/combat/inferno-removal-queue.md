# Inferno pending removals

`player-inferno-removal-queue-calibration.mt2f` preserves the complete native
diagnostic from `025751-eabffd10`, including its recorded mismatches. The
capture-only build is preserved at `.probe-runs/probe277-pending-capture`
(base728cd590). It observes already-known, non-preview dead players which have
left the active room list but still occupy a physical point. It invokes no
callbacks or gameplay effects. The original37 canonical native successors
still pair with the pre-extension recording.

Turns3/5/6 room1 each supply a full-room and first-exchange observation.
The exchange inputs explicitly retain Sycophant5/card6, SneezyWhelp27/card9
and Truffles34/card14: death callbacks and statistics listeners have finished,
but the destruction coroutine has not returned their spawner/physical point.
The model finishes that outstanding removal without firing death a second
time. The full-room path carries earlier damage deaths across the combat
yield, then clears destroyed actors' old placement pointers.

`--inferno-removal-queue-only` checks all six complete native states/RNG and
four independent immutable branches each. This includes Endless draw return,
ordinary exhaust statistics and destroyed placement pointers. The older
original Inferno five-play/five-EndTurn/16-chain fixture remains a control.

This diagnostic captures pending players only; its turn4 enemy15 exchange
still lacks a required native input. It is not a complete accepted battle or
a sixth campaign victory. Production277 extends the same observation to both
teams; a fresh frozen-build campaign must verify that additional boundary.
