# Useful actions on a bounded score plateau

`planner-score-plateau-calibration.mt2f` retains the exact turn-three root
of decision 12 from the naturally progressed fifth-ring search
`.probe-runs/full-battle-units-spells-and-junk-20261021-102009-37e1c7a2`.
That native full battle and its fresh frozen replay matched every state
and gameplay RNG, but both lost. This root is a search regression witness,
not a victory claim.

Ending three turns without playing reaches score -50501, whose positional
term is capped at +499. The old planner kept that idle continuation when
useful alternatives had the same capped score. It now compares remaining
enemy health/armor and allied attack, multistrike, health and armor when
public scores tie. Pyre survival ordering and archived scores are unchanged.

`--planner-plateau-only` requires a useful first play, a completed three-turn
forecast no worse than the idle continuation, exact full-state replay and
an unchanged source. A fresh native battle must separately prove any
improvement in actual outcomes.
