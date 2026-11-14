# Inactive Inferno enemy movement

The original first-wave MageT1_Junker moves into and out of an unchanged
InfernoRoom attachment that affects both teams. A detached original card is
the source so native moon tracking remains off. Full-moon entry/exit applies
and removes the original room upgrade without changing mask 6. After setting
New without tracking callbacks, entry changes stale mask 6 to 5 and applies
the upgrade; exit removes the upgrade while preserving mask 5. All four
post-coroutine train/context and gameplay-RNG successors match the model.
Two muted Instant native processes produced equal complete archive roots and
preserved original files. The curated 9,851-byte binary has 1,138 unique nodes
and SHA-256 `967a5dbcfa58e17efd5d811b7e5aacdcda7b34928f0def6fd122ee95a9ffcf49`.

The model uses the original Inferno attachment shape and a cardless enemy
without moon callbacks, without an enemy asset-name list. Eight parallel
immutable branches pass per row. Changed attachment identity, missing mask,
preview, unknown spawner and moon callback controls remain rejected. This
isolated Bump evidence does not validate natural EndTurn ascension.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -RoomMoonInactiveInfernoMovement -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-inferno-movement-calibration.mt2f
```
