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
understanding, whose callbacks are unchanged by the extra New bit. This initial
Full-only acceptance kept New-moon attachment callbacks guarded. This is a
complete authored action chain; real player-entry acceptance remains separate.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberCallbacks -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-callbacks-only tests/fixtures/full-battle-eclipse-chamber-callbacks.mt2f
```

## Original New callbacks and attachment boundaries

`room-moon-callback-calibration-shade.mt2f` records profile
`full-battle-no-cards-20261021-224949-b9bae29a` (30.18 seconds). Three original
Nightingales join the three original Selenes, retaining their unmodified
Full/New temporary-upgrade add/remove definitions. Fifteen independent rows
include nine refreshes, four global phase assignments and explicit original
Eclipse installation/removal. All complete states and gameplay RNG match,
including 36 upgrade-count changes, 16 Nightingale upgrade-presence changes
and five mixed-mask witnesses. Eight parallel immutable branches pass per row.

Installation adds the room upgrade before draining New gained callbacks;
removal drops the room upgrade before draining New lost callbacks. The shared
attachment model now performs these callbacks. Original Full-only and
callback-free Eclipse action chains still pass. Stale Full understanding,
enemy callbacks, other callback payloads, previews and friendly movement
through moon rooms remain guarded. The integrated New-callback action chain
and real player entry require their own acceptance below.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonCallbacksShade -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-callback-only tests/fixtures/room-moon-callback-calibration-shade.mt2f
```

## Combined Full/New original payload action chain

`full-battle-eclipse-chamber-shade-callbacks.mt2f` records profile
`full-battle-units-spells-and-junk-20261021-225815-f36d4137` (61.87 seconds).
Three authored Silent carriers retain the two original Selene triggers plus
the two original Nightingale triggers. The original definitions are unchanged.
Eight plays and five EndTurns win with Pyre80, zero failure counters, settled
terminal effects and unchanged protected files. All complete native action
boundaries, sixteen immutable branches, six Silent growth witnesses and six
floor/subtype controls pass. New upgrades appear after installation/later birth
and disappear after replacement while the control floor remains unchanged.

The refresh observer also captures the incoming native character-trigger queue.
Both full-refresh calls inherit one pending New gained trigger from attachment
installation; native monster resource refresh drains it before refreshing
understanding. The checker independently drains that captured queue and then
models the refresh. A previous diagnostic without the incoming queue was not
curated: action boundaries matched, but its intermediate refresh inputs omitted
queued work. Empty-queue legacy fixtures continue to pass their own check.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberShadeCallbacks -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-callbacks-only tests/fixtures/full-battle-eclipse-chamber-shade-callbacks.mt2f
```

Real player entry, previews and physical movement remain separate from this
authored fixed-policy acceptance. No Mac package is built.
