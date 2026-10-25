# Unstable explosion during Undying revival

The fifth random campaign's third battle reaches turn4 with Low Ranked
Assistant49 at9HP,4Unstable and1Undying. Input31 ends the turn. Native
revival invokes CharacterState's death signal, including Unstable's
persistent listener. The old model only exploded on the final death.
It therefore left enemy45 and boss8 each4HP too high.

Use the same explosion implementation for revival and final death. Keep
the revived actor, queued harvest/reanimation/death callbacks, one-time
statistics listener and full Unstable damage attribution. Two independent
native processes now match31 inputs, complete successors, RNG and
immutable parents. The historical forecast retains32/347 enemy HP; the
repaired native outcome is28/343. Ordinary explosion and repeated/last-stack
revival controls pass. This component is not a complete battle acceptance.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-unstable-revival-calibration.mt2f
```
