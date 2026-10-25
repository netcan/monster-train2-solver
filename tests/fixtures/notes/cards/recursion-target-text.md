# Recursion target text and generated-card statistics

The natural Remnant/Awoken sixth battle generates Sting33 after the turn draw.
The new hand also contains a base `CardTraitShowCardTargets` with
`CardEffectRecursion`. At the end of the native frame its target-count text
queries `CardStatistics.GetStatValue`, which calls `UpdateDeckStats` even when
there are no eligible targets. This materializes the newly generated card's
statistics entry. Native source: `CardEffectRecursion.GetNumCardTargets` and
`CardStatistics.GetStatValue`; an isolated read-only call-stack probe confirms
the path from `HandUI.UpdateCardUIAtEndOfFrame` through the card text.

The original20-input component and independent repaired component preserve
every input,root,complete successor and RNG. Only the missing33 entry in
`Context.Statistics.StoredCards` may differ in the historical prediction.
The new component's raw capture/mismatch/unsupported/pending counters are all
zero. Parent states remain immutable. The fix is scoped to nonterminal
canonical end-turn decisions with that visible trait/effect combination;
other target-text traits are not newly claimed as calibrated.

These fixtures validate a component, not a campaign win.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-recursion-target-text-calibration.mt2f
```
