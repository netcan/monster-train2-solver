# Discard recursion keeps an installed Holdover flag

Hellhorned/Wurmkin seed770862752, sixth-battle offline candidate: input44
returns card30 from discard using card27's ChooseToHand recursion. The returned
card already has an installed CardTraitRetain with HoldoverPending=true and
receives WurmReturnEnhanceCorrupt, adding Corrupt and Consume. Native keeps
the installed Holdover flag true. It draws this card now, while the next
pre-draw callback is still responsible for clearing the pending flag.

CardManager.RestoreExhaustedOrEatenCard returns immediately for ordinary
discard cards. The model had reset traits for every recursion source.
0.2.52 limits that reset to Exhaust/Eaten for spell and triggered recursion.
It preserves native resets for actual restoration from those two piles.

Two independent native processes match all44 complete successors,
forecasts and RNG states with immutable parents. The historical expected
state retains its wrong HoldoverPending=false. The checker projects just
that historical flag in memory; the repaired forecast exactly matches native.
Holdover's100-operation variant calibration and original Unearth Treasure's
4-play/5-EndTurn exhausted-return control also pass.

The following card30 cast still requires its own Corrupt Restricted signature
calibration. This component is not a sixth-battle victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-discard-holdover-return-calibration.mt2f
```
