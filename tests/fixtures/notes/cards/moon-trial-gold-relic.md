# Moon hand refresh with the original trial-gold relic

Sample23's natural second battle gains ApplyUnitUpgradeWhenRich after the first
accepted default-budget victory. The original native effect is solely
RelicEffectModifyTrialGoldRewards (+50% trial reward gold), despite the asset
name. Its only interface changes post-combat reward gold; it has no moon-phase
or CardUpdated callback. Existing original campaign calibration already proves
the reward percentage and conditional trial behavior separately.

Release240 rejects a five-input natural branch ending at turn1->2, where a
moon-upgrade spell enters the hand. Native observation172356-45561c7a preserves
the complete original input/root/RNG and the rejected prediction. Release241
admits only the original ID/asset, non-Covenant/non-placement flags and complete
single reward-effect list during Moon hand refresh. It neither repeats rewards
nor changes the independently handled campaign gold calculation.

All five full native successors/forecasts/RNG, four immutable parallel branches
and six changed-owner/effect/flag controls pass. Existing BossEmber and null-boss
Moon pairs and campaign arithmetic also pass. Independent repaired native replay
172743-2db21747 passes the same complete five-input states/forecasts/RNG. Both
immutable archives retain the old rejection and new matching prediction.
Actual second-battle default-budget continuation remains in progress.
