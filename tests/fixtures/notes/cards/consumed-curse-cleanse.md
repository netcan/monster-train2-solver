# TitanCurse updates a retained consumed triple-Juice Cleanse

Sample22 Ring5's natural default238 route consumes card11's triple-Juice,
ChaosScepter PurifyingCleanse at input25. Keeping its unchanged29-input prefix
and ending turn4 without playing TitanCurse13 causes the native game to add
TitanCurse_Upgrade (+1 cost) to that retained consumed card. Observation
172811-b4764c13 captures a nonterminal turn4->5, unchanged play count1 and
exhausted private trait state, with the exact new cost-only temporary upgrade.
Release238 rejects this branch before producing a prediction.

Release242 permits that one exact already-consumed owner/upgrade/private-state
combination. Active cards, plain triple Cleanse, multiple curse upgrades, changed
cost/damage/heal payloads and altered consumption counters remain guarded. It
does not infer a native cast of the unavailable consumed card.

The30-input nonterminal observation now replays complete states/forecasts/RNG,
four immutable parallel EndTurns and ten unknown-signature controls. The earlier
independent52-input terminal observation164938-1ae891e9 also replays full native
state/RNG to the same defeat; terminal capture alone had been insufficient to
identify the retained-card upgrade. Original triple/Consume Cleanse birth and
27-input cast controls pass. Independent repaired nonterminal replay
173707-e9bda91c also matches every full input/state/forecast/RNG; the original
rejection and repaired prediction are both retained in the catalog. A60s local
strategy batch completes80 of81 candidates,zero unsupported branches and no
improvement from its1HP model defeat. These components are not new battle wins.
