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
by explicit negative controls. Enemy mask changes are calibrated separately
below; other callback payloads and unit-owned moon resource traits remain guarded. This isolated component does
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

## Player advice with friendly room-mask movement

`full-battle-player-room-moon-movement-{search,replay}.mt2f` records real player
assembly publications with the same original effects, authored carriers and
four owned Freeze shims. The authored hand is ordered so the generic tactical
seed reaches Eclipse before the movement cards. Source profile
`full-battle-units-spells-and-junk-20261022-003336-e5a057dd` wins in 211.32 seconds:
thirty decisions, twenty-five plays, five EndTurns, Pyre80, thirty-two
publications, 285 pure captures and 1,620 complete native decision contexts.
The actual panel renders 300 text vertices and 85,699 visible pixels.

Five moves of a callback carrier execute original New gained/lost payloads.
One later callback-free champion enters the mask without gaining a Nightingale
upgrade, providing an ordinary-unit control. These six mask changes must not
be counted as six callback changes or two callback actors. Five callback
actors, both global phases, one original Eclipse cast and 110 mixed-mask
snapshots appear across the whole battle. After the first winning search,
twenty-nine exact roots retain its verified suffix. Their median/P95 response
is 0.93/1.32 seconds; the first consumed response is 5.98 seconds.

Fresh profile `full-battle-units-spells-and-junk-20261022-004005-67a70205`
repeats the thirty inputs with zero search nodes and no publications in 111.34
seconds. It records 69 pure captures and 520 complete native contexts, winning
with Pyre80. All three loaded DLL hashes match the source. Both native runs
have `DEPTH-PASS`, settled effects, unchanged protected files and four zero
failure counters. The focused checker matches every input, root, successor,
published forecast, gameplay RNG and all seven native spawn boundaries.

This movement case does not capture enemy birth under mixed masks. The earlier
`RoomMoon` pair retains its separate requirement for at least two such births;
the movement pair instead requires repeated actual callback moves in both
directions and the callback-free mask-change control. A previous player run
never cast Eclipse. An intermediate frozen run matched all inputs but its
PowerShell host failed the post-run assembly load; neither is curated.
This is authored-hand acceptance with a six-turn/10,000-ms tactical budget,
not a natural late-game deck or default-budget performance claim. No full
regression or Mac package build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberMovement -PlayerAdvisor -RollingAdvice -PlannerTacticalSeed -PlannerDeploymentRoom 0 -PlannerHorizon 6 -PlannerTimeLimitMs 10000 -SkipBuild
scripts/Run-FullBattleProbe.ps1 -EclipseChamberMovement -PlayerAdvisor -RollingAdvice -RollingInput <source-profile>/full-battle.mt2f -PlannerTacticalSeed -PlannerDeploymentRoom 0 -PlannerHorizon 6 -PlannerTimeLimitMs 10000 -SkipBuild
scripts/Check-PlayerAdvisor.ps1 -Case RoomMoonMovement -SkipBuild
```

## Opposing carriers and ordinary enemy movement

`room-moon-enemy-movement-calibration.mt2f` records profile
`full-battle-no-cards-20261022-125807-9f9754c9` (40.29 seconds, normal exit and
unchanged protected originals). Native HeroManager creates six opposing
Selene/Nightingale carriers from unchanged original definitions; six friendly
carriers and one unchanged first-wave enemy provide cross-team and no-callback
controls. Their placement and ownership are authored; these are not natural
enemy-deck definitions.

All 39 complete native boundaries and gameplay RNG match: fifteen refreshes,
seven phase assignments, four attachment changes and thirteen enemy Bumps.
Ten Bumps change cached understanding, 52 opposing upgrade changes occur across
the sequence, and two ordinary enemy moves change only its mask. The stale-phase
control still refreshes only the moved enemy. Full is compared before New, and
the native None bit remains intact. Each row passes eight parallel immutable
branches, including complete card-pipeline refresh comparisons.

Unknown room masks must be rejected before the no-change fast path: enemies do
not receive the friendly attachment-upgrade validation that previously caught
them incidentally. Explicit unknown callback, prior-mask and preview guards
also pass. Existing 13/15/37-row components and the twelve-paid-play friendly
chain pass focused checks. The first diagnostic attempted to select a wave
enemy before deployment had created any; it is not curated. The accepted run
explicitly spawns the original first-wave definition without changing it.

This opens the existing modeled upgrade callbacks and actual enemy Bump phase
refresh. Ordinary and callback-bearing paid enemy chains and real player entry
with this composition are validated below. Enemy movement published by the player,
previews, other callback payloads, enemy Conduit and unit-owned moon resource
traits retain their separate acceptance boundaries. No full regression or Mac
package build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonEnemyMovement -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-callback-only tests/fixtures/room-moon-enemy-movement-calibration.mt2f
```

## Paid original spells on a first-wave enemy

`full-battle-eclipse-chamber-enemy-movement.mt2f` records profile
`full-battle-units-spells-and-junk-20261022-130305-ac7ca978` (93.49 seconds).
Thirteen paid/setup plays and five EndTurns win with Pyre80, normal exit,
settled terminal effects, unchanged originals and all four failure counters zero.
The original friendly callback carriers remain authored as in the prior fixture;
the targeted `ShieldGuardT1_BasicNonAttack` is spawned by the original wave.

An extra unchanged Moonrise is held with the same instance-only Freeze shim as
the four movement cards. The fixed policy waits for turn one, uses its actual
New-to-Full transition and one-energy gain, then pays one each for two original
Hidden Passage/Dripfall pairs on the same enemy. All four moves change cached
understanding across the Eclipse boundary; other units keep their masks. The
enemy has no moon callback payload, keeping its control separate from the six
authored opposing carriers in the isolated component above.

All thirteen actions, five EndTurns, two character/twelve support refreshes and
six friendly Silent growth/six floor-subtype controls match complete native
state/RNG. Sixteen independently replayed full chains remain immutable.

Fresh profile `full-battle-units-spells-and-junk-20261022-130622-06033c87`
repeats all eighteen frozen inputs with zero search nodes in 65.54 seconds.
It also wins with Pyre80, normal exit, settled effects, unchanged originals
and four zero failure counters. Every root, successor, terminal outcome and
gameplay RNG matches the frozen independent model chain. It is retained as
`full-battle-eclipse-chamber-enemy-movement-replay.mt2f`. Both native captures
pass the focused thirteen-play/five-EndTurn checker. This fresh native run
also exercises the extracted production process waiter on its ordinary exit
path; its grace/failure paths have separate real child-process controls.

The callback-bearing paid composition is validated next. This ordinary control
does not establish real player advice for this route. No full regression or Mac package build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberEnemyMovement -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-enemy-movement-only tests/fixtures/full-battle-eclipse-chamber-enemy-movement.mt2f
```

## Paid opposing callbacks and off-train birth

`full-battle-eclipse-chamber-enemy-callbacks.mt2f` records profile
`full-battle-units-spells-and-junk-20261022-131656-ee85e1b2` (122.41 seconds).
The scene clones an original first-wave shield guard and appends unchanged
Selene/Nightingale trigger definitions. Only the authored wave references the
clone; original card, callback and enemy definitions remain intact. The queued
spawn projection reads the same cloned definition as native HeroManager.

This exposed an actual modeling gap: enemy Setup queues its initial global
phase callback off-train, before any group OnSpawn callbacks run. Previously
the model captured only its phase status/mask and missed Nightingale's upgrade
and two multistrike stacks. Enemy spawning now preserves that shared queue.
The carrier's initial New mask remains 5 even beneath a room override.

After original Moonrise changes New to Full and adds one energy, two unchanged
Hidden Passage/Dripfall pairs pay four energy and move the same opposing
carrier across Eclipse. All four moves change cached understanding and remove
or restore the original New upgrade; unrelated masks remain unchanged.
Thirteen actions, five EndTurns, seven spawn boundaries, complete state/RNG
and sixteen independent immutable full chains match. The battle wins at Pyre80.

Fresh profile `full-battle-units-spells-and-junk-20261022-132006-c63cea47`
replays all eighteen independently frozen inputs with zero search nodes in
75.71 seconds and is retained as `full-battle-eclipse-chamber-enemy-callbacks-replay.mt2f`.
Both processes settle, exit normally, preserve protected originals and have
four zero failure counters. Full frozen roots, successors and terminal state/RNG
match. The earlier setup diagnostic and the capture exposing the birth mismatch
are not curated. Ordinary opposing and friendly paid controls still pass.
No full regression or Mac package build is performed.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberEnemyCallbacks -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-enemy-callbacks-only tests/fixtures/full-battle-eclipse-chamber-enemy-callbacks.mt2f
```

## Actual player entry with opposing callbacks

`full-battle-player-room-moon-enemy-{search,replay}.mt2f` retains source profile
`full-battle-units-spells-and-junk-20261022-131707-b89cab82` (245.30 seconds)
and independent replay `full-battle-units-spells-and-junk-20261022-132130-ec152fca`
(102.65 seconds). Both load the real player assembly and win with Pyre80:
25 plays, five EndTurns and thirty decisions. Both settle, exit normally,
preserve protected originals and have four zero failure counters. All inputs,
complete native roots/successors, frozen forecasts and gameplay RNG match.

The real overlay publishes 32 times, passes UI/pure-capture controls and retains
the verified winning continuation for the 29 later decisions. The first used
search takes 3.41 seconds; later end-to-end response median/P95 is 1.01/1.34
seconds. The configured budget remains six turns/10,000 ms with tactical seed.
The opposing actor's off-train birth callback and eight live upgrade witnesses
match its cached understanding; both original friendly callbacks, two global
phases and two Eclipse casts are also exercised.

The player route does not Bump the opposing callback carrier. Its acceptance
therefore covers opposing birth/global phase callbacks and real advice in that
composition; the separate fixed paid chain above covers repeated enemy Bumps.
Natural late-game combinations and player-published opposing Bumps remain
separate boundaries. No full regression or Mac package is built.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case RoomMoonEnemy -SkipBuild
```

Original spell-owned moon upgrades with these room masks now have separate
fifteen-boundary component and paid whole-battle/fresh-replay acceptance.
See [room spell upgrades](room-moon-spell-upgrades.md). Living-unit resource
traits and preview callback combinations retain their guards.
