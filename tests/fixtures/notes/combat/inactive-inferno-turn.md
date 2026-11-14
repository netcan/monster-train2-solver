# Inactive Inferno natural EndTurn

An unchanged HeavyT3_Basic starts in the original Inferno room with moon
tracking off. Its room source card is detached. The enemy retains old Full
understanding (mask 6) while the global phase is New. Native EndTurn moves it
from room 0 to room 1, removes the original room upgrade and changes its mask
to 5. The global phase then returns to Full. With no player unit present,
the enemy remains at 325 health; this witness does not demonstrate a damage
hit from the Inferno PreCombat effect.

Two muted Instant native processes produced byte-identical 11,572-byte
archives with SHA-256
`ef0e8a87a50610faf8d1167803b663806f3f6cf8e2eca85f5d1d7cba18e6ffa3`.
The model matches the full settled successor and gameplay RNG with four
immutable parallel replays. Original game files were unchanged. Other room
occupants, damage targets and later turns remain separate.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -RoomMoonInactiveInfernoTurn -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-inferno-turn-calibration.mt2f
```
