# Merchant Permafrost on the X-cost Undying copy spell

Seed972508374, champion-preserving Ring6 naturally combines the authored
Undying/Consume spell, CreateSpellCopyWithPurge and the merchant TraitPermafrost
upgrade. The latter's captured modifier has Cost -1 and XCost +1. Its installed
trait is ordinary, removable Permafrost with no additional callback definitions.

0.2.57 accepts only this captured original card/upgrade signature. Existing
permanent-upgrade installation creates a separate temporary Freeze on the new
copy. Authored status scaling survives copying; the copy upgrade is excluded
and the copy receives temporary Ephemeral. Changed authored parameters and
changed Permafrost runtime parameters remain rejected.

The deployment pair preserves the original zero-prefix refusal and an
independent repaired successor. A separate fifteen-input pair executes both
original and copy in native game processes. Paying four energy grants five
Undying; paying zero for the copy still grants one from XCost +1. Both thaw on
play; the original is Exhausted and the copy has the native two Purged references
without recursively copying. Complete immutable state, forecast and RNG chains
match. This evidence does not establish a Ring6 or eight-battle victory.

Unplayed copied cards require a separate EndTurn witness: native CardManager
explicitly bypasses Freeze when Ephemeral is present. Thus ordinary Permafrost
retention must not be inferred for this short-lived copy. The discard pair
checks that path independently from the immediate-play pair.

```powershell
./scripts/Check-Models.ps1 -SkipBuild -Fixture @(
  'player-undying-permafrost-copy-deployment-calibration.mt2f',
  'player-undying-permafrost-copy-effects-calibration.mt2f',
  'player-undying-permafrost-copy-discard-calibration.mt2f')
```

The old eleven-input positive/zero-cost copy pair and original Permafrost
draw, retention, thaw and repeated re-freeze controls also pass.
