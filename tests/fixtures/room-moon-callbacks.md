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
enemy callbacks, other callback payloads and previews remain guarded. Friendly
movement is calibrated separately below. The integrated New-callback action chain
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

Real player entry, previews and physical movement require their separate
authored fixed-policy acceptance. No Mac package is built.

## Real player rolling advice and fresh frozen replay

`full-battle-player-room-moon-{search,replay}.mt2f` records the actual player
entry with authored Silent callback carriers and unchanged original room/spell
and callback payloads. Source profile
`full-battle-units-spells-and-junk-20261021-230548-15477baa` wins in 178.82 seconds:
23 rolling decisions, 18 plays, five EndTurns, 25 publications and 246 pure
captures. The UI interaction gate passes with 352 text vertices and 85,699
visible pixels. Fresh profile
`full-battle-units-spells-and-junk-20261021-230904-1abf02f5` repeats the same
inputs with zero search nodes in 84.21 seconds, recording 55 pure captures and
no publications. Both win with Pyre80, settled terminal effects, unchanged
protected files and all four failure counters zero. Their three player DLL
hashes match each other exactly. This is not a natural-deck acceptance.

The focused pair check matches all inputs, roots, successors, complete forecasts
and gameplay RNG, plus all seven native spawn boundaries in each process. Five
callback actors, both global phases, an original Eclipse cast, 46 mixed-mask
snapshots, 123 New upgrade and 82 Full upgrade snapshots provide live witnesses.
Four distinct enemies also initialize beneath Full/New room masks. Native hero
creation initializes understanding off-train with only the global phase; the
later refresh adds the room bit. The model now preserves that spawn boundary.
An intermediate full-refresh row leaves one no-op enemy phase callback queued;
the checker predicts its count and rejects pending callbacks with effects.

The earlier source/replay diagnostic had four spawn-mask mismatches and is not
curated. Winning and matching only final action boundaries were insufficient.
Existing natural-moon and Conduit player pair gates also pass after the shared
checker update. No full regression or Mac package build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberShadeCallbacks -PlayerAdvisor -RollingAdvice -PlannerDeploymentRoom 0 -PlannerHorizon 3 -PlannerTimeLimitMs 1000 -SkipBuild
scripts/Run-FullBattleProbe.ps1 -EclipseChamberShadeCallbacks -PlayerAdvisor -RollingAdvice -RollingInput <source-profile>/full-battle.mt2f -PlannerDeploymentRoom 0 -PlannerHorizon 3 -PlannerTimeLimitMs 1000 -SkipBuild
scripts/Check-PlayerAdvisor.ps1 -Case RoomMoon -SkipBuild
```

## Friendly physical movement through original room masks

`room-moon-movement-calibration.mt2f` records profile
`full-battle-no-cards-20261022-001933-f88e7ea7` (37.50 seconds), retaining
the same six original Selene/Nightingale actors and unchanged original Eclipse.
Thirty-seven complete native boundaries include eleven signed Bump moves,
fifteen refreshes, seven global phase shifts and four installation/removal
operations. Eight moves change cached understanding, with sixty upgrade-count
changes across all operations and twenty-six New-upgrade presence changes.
All complete train/context states and gameplay RNG match. Each row passes eight
parallel immutable branches. Protected original files remain unchanged.

Spawn-point callbacks refresh resources only. The original Bump coroutine then
refreshes just its moved character after Shift/Sentry, comparing Full before
New and retaining the native None bit. Its phase triggers stay queued until the
ordinary queue drain. A deliberately stale global-phase control proves that
unmoved actors retain their cached masks and upgrades; refreshing all actors
at movement time would be incorrect. Up/down, two-floor movement, entering and
leaving masks under both global phases, later no-op refreshes and a nonmasked
control are included. Existing thirteen/fifteen-row callbacks and the original
nineteen-operation Bump fixture still pass their focused checks.

Missing cached masks, friendly previews and unknown room-mask rules are rejected
by explicit negative controls. Enemy mask changes, other callback payloads and
unit-owned moon resource traits remain guarded. This isolated component does
not establish a full paid-card action chain or player rolling-advice acceptance
with movement; those require their own native execution and independent replay.
No full regression or Mac package build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonMovement -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-callback-only tests/fixtures/room-moon-movement-calibration.mt2f
```

## Paid original movement action chain

`full-battle-eclipse-chamber-movement.mt2f` records profile
`full-battle-units-spells-and-junk-20261022-002823-51609a45` (83.87 seconds).
Two unchanged Hidden Passage definitions and two unchanged Dripfall definitions
join the existing authored Silent callback carriers. A temporary Freeze trait
on these four owned spell instances keeps them in hand until paid; the harness
hand limit is twenty. Original spell/room/callback definitions remain intact.

Twelve paid plays and five EndTurns win with Pyre80. All complete native action
and turn states/RNG, two full refreshes, eighteen resource-only refreshes,
physical movement and sixteen immutable full chains match. One Full-moon exit
removes the original Nightingale upgrade. The other three moves occur under
global New and preserve the upgrade, providing actual no-change controls.
Both callback carriers return to the original masked floor; six later Silent
growth and six floor/subtype controls still match. Native terminal effects
settle, protected files stay unchanged and all four failure counters are zero.

The initial diagnostic omitted physical points and executed no movement. A
second diagnostic lacked Freeze retention and executed only three of four
intended moves. Neither is curated. Real player rolling-advice movement remains
a separate gate; a winning player run that never uses Eclipse cannot close it.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberMovement -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-movement-only tests/fixtures/full-battle-eclipse-chamber-movement.mt2f
```
