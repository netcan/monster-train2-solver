# Room moon masks and character callbacks

`room-moon-callback-calibration.mt2f` captures original Mono game 2.2.1,
module `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, in profile
`full-battle-no-cards-20261021-223304-6bc946da` (28.25 seconds).
The original Selene Acolyte characters are created in floor order 1, 0, 2;
their definitions and the Eclipse Chamber definition remain unchanged.

The 13 rows cover nine original resource/character refreshes and four global
phase assignments: Full gained/lost, resource-only refresh, same-phase no-ops,
Eclipse attachment/removal, mixed masks and global transitions while New stays
active on the masked floor. Character masks preserve the native None bit 4.
Automatic preview updates are suppressed only in this isolated component so
idle preview allocation does not change the physical identity counter between
the two snapshots. All gameplay RNG stays unchanged and protected game files
retain their original signatures.

The focused checker independently reproduces every complete train/context and
RNG boundary, including 18 upgrade-count changes and four mixed-mask witnesses.
Each row also passes eight parallel immutable branches. Native understanding
compares Full before New. Resource refresh queues all character callbacks
without firing phase-shift relic callbacks; global shifts retain their own
phase-shift path. Existing Eclipse and unmasked moon callback checks pass.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonCallbacks -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-callback-only tests/fixtures/room-moon-callback-calibration.mt2f
```

This calibrates player temporary-upgrade add/remove payloads. Enemy callbacks,
other callback payloads, moon resource trait callbacks and preview refreshes
remain guarded. It does not establish an integrated Eclipse/character action
chain or a real player encounter. No new Mac package is built for this batch.

## Integrated original-callback carrier chain

`full-battle-eclipse-chamber-callbacks.mt2f` records profile
`full-battle-units-spells-and-junk-20261021-224510-3982295e` (57.16 seconds).
The two original Selene triggers are copied to the existing authored Silent
carriers; ordinary units remain callback-free. Original room/spell definitions
are unchanged. The battle wins with 80 Pyre HP, eight exact plays, five EndTurns,
two character refreshes, eight queued support refreshes and zero failure counters.
Terminal effects settle and original files remain unchanged. Six Silent growth
witnesses and six floor/subtype controls pass, as do 16 immutable action branches.

The card pipeline now prevalidates global refresh using the entire train rather
than one selected room. All nine isolated refresh rows also pass through the
same card-effect pipeline. Eclipse attachment/removal accepts settled Full
understanding, whose callbacks are unchanged by the extra New bit. Stale Full
understanding and New-moon attachment callbacks remain guarded. This is a
complete authored action chain; real player-entry acceptance remains separate.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberCallbacks -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-callbacks-only tests/fixtures/full-battle-eclipse-chamber-callbacks.mt2f
```
