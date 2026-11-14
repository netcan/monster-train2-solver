# Inactive Mushy Room natural EndTurn

An unchanged first-wave MageT1_Junker starts in an original Mushy Room with
moon tracking disabled. The room source card is detached from the hand; the
enemy's old Full understanding remains at mask 6 after the global phase is
set to New. The game executes its normal EndTurn command.

The enemy naturally ascends from room 0 to room 1. Its Mushy marker upgrade is
removed and its mask changes from 6 to 5. The global phase then resets to Full
while tracking remains disabled. The complete settled decision successor and
gameplay RNG match `BattleTurnModel.EndTurn`, including four immutable parallel
replays. Two muted Instant processes produced identical 11,504-byte archives
with SHA-256 `02ef8650347f1e999329470f26adee6a37fcf5fc2c8c5ba3cb90d61fe6a5a51e`.
Original game files were unchanged.

This is one complete natural turn for an original enemy and room. Later turns,
other enemy callbacks and card or relic combinations remain separate.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -RoomMoonInactiveMushyTurn -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture room-moon-inactive-mushy-turn-calibration.mt2f
```
