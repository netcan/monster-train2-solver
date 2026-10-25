# Draw callbacks when recursion restores a card to hand

Random10 Ring6, Banished/Wurmkin, seed1895012796, Covenant0. On turn9,
input46 plays ReturnedSoul14 and selects exhausted Benediction15. The card
already has Permafrost from the original expensive-card relic. Native
CardEffectRecursion restores its traits, applies WurmReturnEnhanceCorrupt,
then calls DrawSpecificCard. CardTraitPermafrost.OnCardDrawn installs one
temporary CardTraitFreeze, even though the Permafrost upgrade was present
before the return. The old model restored the pile and upgrade but omitted
the draw callbacks.

Version0.2.92 reuses the calibrated draw callback path for returned cards
that actually entered the hand. Cards restored to the deck or overflowing
into the draw pile do not dispatch this hand-draw callback.

The original46-input observation preserves its missing-Freeze forecast.
An independent native process repeats all46 inputs with the corrected
forecast. The dedicated checker compares every complete successor, forecast,
RNG and immutable parent, selected-card identity, upgrade and single Freeze.
Related Permafrost/high-cost relic checks and native random-hand/overflow,
discard-choice and original pile-recursion regressions pass. This remains
component calibration; the sixth battle must still complete independently.

```powershell
./scripts/Check-Models.ps1 -Fixture player-recursion-permafrost-calibration.mt2f -SkipBuild
```
