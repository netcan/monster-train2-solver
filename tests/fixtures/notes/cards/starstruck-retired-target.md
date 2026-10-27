# Starstruck moon Daze after its selected enemy dies

Sample23 Ring6 deployment investigation exposes a legal first-turn play:
Starstruck12 on floor0 targets AttackerT2_Basic7 at3HP. The source has
original triple Juice, ReduceCost and StarstruckRoom_Upgrade, plus one
temporary Conduit contributor. The old model resolves the damage, removes
the victim, then rejects OnCast with `The spell target is missing.`

Native CardManager.OnCardPlayed fires OnCast after the damage. Although
ordinary collection ignores dead actors, TargetHelper.CheckTargetsOverride
restores the original drop-point object without another death test.
The Daze therefore reaches that private object and records three
AnyStatusEffectStacksAdded for Starstruck, even though no live room unit
retains Daze. Merely skipping the status gives the right room occupants
but wrong complete statistics.

Source204522-eca9ad9d and release255 independent205533-1462c1b2 agree across
all five inputs, full state/forecasts/RNG. The repair carries the actual
retained victim into this narrowly identified original moon Daze path,
uses normal status scaling/statistics and returns queued callbacks while
keeping the victim out of live rooms. Unknown owners, payloads, status
amounts and targeting retain their guards; an initially missing target is
still illegal. Four parallel branches leave the parent unchanged.

The previous30-input triple-Juice cast pair and14-input mirrored existing
Horde pair remain matching controls. This component pair does not establish
a winning Ring6 route or general support for other dead-target triggers.
