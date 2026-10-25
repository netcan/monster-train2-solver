# Native player-advisor Mix choices

The isolated player driver now consumes the published Mix selections in
effect/mix order. It observes the offers produced by the original
ElixirDraftScreen and confirms the indexed offer through ApplyDraft, the
same native callback used after player selection. Offer generation,
upgrade application, callbacks and gameplay RNG stay native. Unavailable
choices and unconsumed pending drafts fail acceptance.

The previously blocked Stygian/LazarusLeague second-battle prefix now
completes all29 inputs, including six new-elixir choices. Independent model
replay matches every full successor, RNG and immutable parent. Evidence:
`.probe-runs/full-battle-units-spells-and-junk-20261025-121305-01a37482/unsupported-branch-native.mt2f`.
This is a validation-driver change; the production advisor assembly excludes
PlayerAdvisorProbe. It does not establish a campaign victory. The first
fresh 0.2.41 player battle also wins while using Mix; its independent
campaign replay is tracked separately.
