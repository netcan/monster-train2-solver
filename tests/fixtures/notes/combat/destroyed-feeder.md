# Destroyed feeder reference at frame completion

In another natural Remnant/Awoken sixth-battle route, food6 keeps feeder5
from the previous turn. Heavy5 dies during turn2, while food6 survives and
ascends. The raw room result still exposes feeder5; after Unity's frame
destruction, GetLastFeederCharacter compares null and the settled native
capture reports0. The old model kept5 on the surviving food.

Cleanup now belongs to CompleteFrameRemovals. It clears references to
completed removed owners, preserves explicitly retained owners whose
destruction is unfinished, and honors an explicit completed-destruction set.
Raw room/combat transitions and stopped terminal combat retain their timing.

The two independent12-input observations compare all inputs,roots,successors,
RNG and immutable parents. The historical projection allows only food6's
LastFeederId in the live room and enchantment-world cache to change5 to0.
Living feeder10 remains a control. A third raw archive is replayed through
RoomCombatModel.Resolve and must still expose5 after the feeder disappears
from that room. All four repaired raw counters are zero.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-destroyed-feeder-calibration.mt2f
```
