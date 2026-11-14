# Inactive Mushy Room two-turn continuation

The unchanged first-wave MageT1_Junker starts in the original Mushy Room.
Moon tracking remains off; global New phase leaves its Full mask 6 stale at
the decision root. Two normal native EndTurns move it from room 0 to 1 to 2.
Its mask is 5 after both moves; the global phase cycles New, Full, New.

`BattleTurnModel.EndTurn` reproduces each complete settled successor and
gameplay RNG. The second step matches both when resumed from the native
first-turn state and when continued from the first modeled successor. A
two-step planner replay matches the same final state. Four immutable parallel
replays pass. Two muted Instant native processes produced identical 12,388-byte
archives (1,524 nodes), SHA-256
`e9c623ac7790bfc43c71bf59e9b143da565c65060fc575223c0205c0f523b9b3`.
Original game files were unchanged.

The witness has no player card play and does not establish other enemies,
unknown callbacks or longer continuations.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -RoomMoonInactiveMushyTwoTurns -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-mushy-two-turns-calibration.mt2f
```
