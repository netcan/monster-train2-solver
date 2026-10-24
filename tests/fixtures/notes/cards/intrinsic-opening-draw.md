# Intrinsic after deployment

`intrinsic-opening-draw-calibration.mt2f` retains the unmodified `Before`,
`Actual` and outcome of native turn transitions 0 and 1 from
`.probe-runs/full-battle-units-spells-and-junk-20261021-101348-6fb7ca98/full-battle.mt2f`.
The input is the naturally progressed fifth-ring checkpoint described in
`campaign-progressed.md`. The full source battle later found a separate
scaling-query metadata discrepancy and is not an accepted full battle.

The offered `TraitInherentSpellReduceCost` upgrade adds Intrinsic to
`SpikeOfTheBanished`. Native `CombatManager` draws the deployment hand on
turn zero and calls `CardManager.DrawOpeningHand` on normal turn one.
`CardTraitIntrinsicState.OnPreOpeningHand` draws that specific card before
the five ordinary cards, producing six cards without an extra RNG draw.
The second retained transition draws an ordinary five-card hand.

`--intrinsic-opening-only` asserts the original upgraded card identity,
the deployment/normal-turn boundary, draw statistics and ordinary-turn
control, then compares both complete modeled transitions with native state
and gameplay RNG. This component fixture makes no full-battle victory claim.
