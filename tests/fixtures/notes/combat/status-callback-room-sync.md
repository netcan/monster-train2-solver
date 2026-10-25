# Status callbacks retain current preview deaths

Random Stygian/LazarusLeague seed972508374, third battle input6, summons
Twisted Assistant card19. The old forecast incorrectly cached preview-born
Fleshweaver mini13 as alive on the Assistant's second OnAttacking callback.
Passive native tracing shows the mini dead and excluded from both caches.

The model's first cache was correct. A later OnNewStatusEffectAdded callback
reimported the retained world snapshot with the mini's initial5HP, reviving
it inside the same preview. Publish current room and dead/retained actors
before that callback. The fix preserves complete native cache semantics;
living retained enemies remain valid weak references. At preview completion,
the original primary restoration still leaves mini13 at5HP outside the room.

Two independent native processes match all six inputs, full successors,
RNG and immutable parents. The original request's provisional diagnosis
is preserved unchanged; the historical forecast includes extra cache ID13,
while the new native prediction exactly matches IDs2,3,4,8,11.
This component check does not establish a complete third-battle victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-status-callback-room-sync-calibration.mt2f
```
