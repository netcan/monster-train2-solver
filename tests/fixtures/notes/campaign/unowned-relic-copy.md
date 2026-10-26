# Transient relic recasts and unit-birth identities

Random13 Banished/Wurmkin seed1184964217 Ring3 exposes13 diagnostic action
mismatches. The actual player projection's30 inputs, forecasts and successors
already replay exactly; it is a defeat, not an accepted win.

RelicEffectMonsterSpellSingleToTeamTargeting.GenerateCardStateCopy creates
one private recast copy for every eligible pre-play callback, including spells
aimed at enemies. The relic clears its reference and played-cost statistic
before the next player decision. UnitBirthProbe's broad CopyCardState observer
registered these13 transient copies as birth sources, assigned permanent
diagnostic IDs24..36 and retained them in its registry. The actual player
plugin does not install that development observer and never retains them.

The development probe now scopes this exact native consumer with a Harmony
prefix/finalizer. It records original source IDs, data IDs, independent copy
references and completion without registering the transient result as a birth.
The finalizer restores the previous scope on success or exception. Other
CopyCardState consumers retain their existing birth observations. This code is
excluded from the player plugin.

The independent fixed process repeats all30 inputs with zero searches or
publications. All player roots, successors, full forecasts and RNG match the
retained original, and all four failure counters become zero. Each of the13
original native copy calls matches a new observation in the same order.
The old failed archive is preserved with13 mismatches; no old counters are
rewritten. Sources:20261026-101321-c8c0e653 and20261026-102918-3571a5d0.

Fixtures: campaign/progression/player-unowned-relic-copy[-replay]-calibration.mt2f.
Use --unowned-relic-copy-pair-only with those two archives. This establishes
the diagnostic boundary, not a native victory or a newly supported spell
replication combination.
