# Conduit physical movement component

`conduit-movement-calibration.mt2f` records 34 actual `CharacterState.SetSpawnPoint`
calls in an isolated game 2.2.1 process. Native profile
`full-battle-no-cards-20261021-213038-caea6e79` completes in 32.57 seconds and
preserves the protected original files. The boundary is immediately after the
native call returns; this archive is a component calibration, not a battle win.

The controls use an original Steward carrier with four Conduit stacks, an
unchanged original damage spell, and an authored copy carrying the original
StrongerMagicPower base trait. A temporary trait has a separate guarded lifecycle
and is not substituted for this base-trait control. All three selected floors and
all nine source/destination floor pairs are exercised, followed by detach,
reattach and dead-character controls. Native upgrades are four damage/heal for
the ordinary spell and twenty for its multiplier control.

Run:

```powershell
scripts/Run-FullBattleProbe.ps1 -ConduitMovement -SkipBuild -TimeoutSeconds 180
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --conduit-movement-only tests/fixtures/conduit-movement-calibration.mt2f
```

The independent checker reproduces complete contexts, physical point ownership,
the contributor's private tracked-card and multiplied-card collections, and all
captured gameplay RNG streams. Parent snapshots stay immutable. The 34 rows have
eight tracking additions, nine clears, seventeen unchanged controls, five
detach/reattach operations and three dead-character operations.

Native order matters: entering a selected floor adds hand tracking even for a
dead actor. Dead actors are excluded from the room contributor enumeration while
their private modifier still exists. Detached actors also retain that modifier.
The calibration therefore captures it separately and the pure component receives
it explicitly. Ordinary battle snapshots do not yet retain those detached/dead
trackers; `ConduitModel.Validate` continues to reject physical whole-battle
contexts. The component-only validation path does not relax that guard.

This does not establish preview movement, mixed conduits, room-mask callbacks,
arbitrary multiplier/trait combinations or an integrated player action chain.
The existing original Conduit relic fixture remains a separate affected check.
