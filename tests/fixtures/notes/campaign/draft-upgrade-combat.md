# Acquired draft-upgrade relic in combat · 0.2.82

Random09 Wurmkin/Hellhorned Ring6 deployment refuses UpgradedDrafts in181ms,
before any input. Its original owner is9e0e5d4e-6d16-43f1-8cd4-cc4c2b431afd,
with the single effect RelicEffectAddStartingUpgradeToCardDrafts.

The native sealed class implements only IAddStartingUpgradeToCardDrafts.
RelicManager.ApplyStartingUpgradeToDraftCard is called by CardDraftScreen;
its permanent card upgrades and Rewards RNG changes have already happened
before battle capture. Search now recognizes this reward-only effect while
still checking every other effect on the relic. Existing840 actual GrantReward
selections and the starting-upgrade eligibility/filter/card-state/Rewards RNG
matrix pass unchanged.

Two isolated original-checkpoint observations execute the first deployment
EndTurn with this acquired relic. Complete state/RNG,immutable parents and an
unknown companion-effect rejection pass. The retained original has a null
prediction; the fixed capture has an exact prediction. Historical room and
enchantment diagnostics contain52 duplicated rejection strings. Only the
exact old diagnostic is projected out in the checker/request; equality with
a fresh native deployment is required. Production has no old-snapshot bypass.

Fixtures are `campaign/progression/player-draft-upgrade-combat[-replay]-calibration.mt2f`.
This is a component calibration; the complete Ring6 default battle is separate
and remains a strategy gap. Windows now contains accepted0.2.82 from the new
continuous Ring5 actual-default pair (43inputs/Pyre49); the deployment fix does
not establish a default Ring6 victory.

```powershell
./scripts/Check-Models.ps1 -Fixture player-draft-upgrade-combat-calibration.mt2f -SkipBuild
```
