# Direct outer-boss tower movement and room attachments

The unmodified random campaign10 Banished/Wurmkin Covenant0 checkpoint
`campaign-ring6-20261026-002452-44572b0f`, seed1895012796, reaches its52nd
input under the actual10000ms default advisor. EndTurn10 moves
`Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth` (unit8) from room2
directly to the Pyre, then loses. The native boss retains BoilerRoom's
`FuelCar_Upgrade` and its OnDeath trigger; the old forecast removes both.
The health513→368 and terminal defeat already agree. This is not a new win.

`HeroManager.DoBossTowerAttack` calls `RoomState.MoveBossToRoom`, movement
callbacks, Shift, Sentry, and tower attacks. Unlike regular ascension, it
does not call `HandleRoomUnitOrderPossiblyChanged`. The generic Bump model
previously synchronized departure/destination attachments anyway. Only
the direct tower-entry caller now skips that synchronization; ordinary
ascension and paid Bump keep their existing updates.

`player-pyre-attachment-calibration.mt2f` preserves the old bad forecast
from native run `20261026-015503-a5f23094`. Independent corrected run
`20261026-020209-f7277c46` supplies
`player-pyre-attachment-replay-calibration.mt2f`. Both replay the same51
prefix inputs and the terminal EndTurn without altering the native save.
The checker requires all52 complete native successors, forecasts, RNG,
outcomes and immutable parents, plus identical native states and inputs
between processes. The original missing upgrade/callback remains an
explicit historical counterexample.

Development-only observations read the private impacted-character list
and physical spawn points without invoking cleanup getters. They show
that primary attachment updates occur before the real boss moves; only
preview attachment updates follow it to the Pyre. Room2 still lists unit8
as impacted after terminal stop. The callback keeps native StateId2 and
canonical attachment identity `08001aa4-2e9d-42da-9ba7-ff490ed5ce80`.

Focused regular Bump, direct boss tower win/loss, complete tower EndTurn
win/loss, and original first-room attachment controls pass. No full
regression or Mac package build is needed for this correction.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --outer-boss-pyre-attachment-only `
  tests/fixtures/bosses/outer/player-pyre-attachment-calibration.mt2f `
  tests/fixtures/bosses/outer/player-pyre-attachment-replay-calibration.mt2f
```
