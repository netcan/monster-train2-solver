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

A second candidate seed now uses captured card/unit strength to deploy units,
place healthier tanks in front, activate available unit abilities and select
beneficial versus harmful spell targets. It shares the planner's node/time
budget, legality checks and evaluator. The fixed verification policy remains
unchanged. The native-root plateau check now reaches -58501; starter and unit
ability planning checks also pass their replay, immutability and budget gates.

`--heuristic-forecast-only <rolling-source.mt2f>` runs that seed to a terminal
model outcome and replays its entire chain without Unity. On the progressed
fifth-ring root it predicts a win at 63 Pyre HP in 38 decisions. It reports
omitted unsupported alternatives explicitly; this is a fast strategy diagnostic
and does not establish a native victory. Native rolling acceptance is separate.
