# Moon spell upgrades with third-spell magic power

The original `IncreaseMagicPowerOnSpellsPlayed` owner
(`76166da7-d9aa-4c68-9a78-64914ec49f81`) grants fixed temporary damage/healing
offsets to scalable owned spells on the third spell once per turn. Its native
card-play handler uses each card's multiplier at that callback. Later moon
refreshes retain these offsets; numeric upgrades still rescale separately.

Previously, moon hand refresh rejected this owner as an uncalibrated relic.
The new compatibility guard accepts only the captured original effect, +1
power, no required traits, third-spell/ThisTurn/once condition and exact query
parameters. The query type mask must match every current owned card's type.
This does not admit arbitrary thresholds, effects or moon/relic compositions.

## Native component

`relics/modifiers/room-moon-spell-threshold-relic-calibration.mt2f` comes from
isolated worker2 profile `full-battle-no-cards-20261025-015414-762613c5`, game
2.2.1 / module `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`. The unchanged original
owner is acquired before battle. Five original moon spells, Lunacy, a numeric
upgrade and original Eclipse room effect exercise 15 phase, resource, floor
selection and attachment boundaries. Native elapsed time: 59.08s; the real
save/config signature remains unchanged.

Before the guard change, the independent checker failed at `unmasked-full`:
`Uncalibrated moon hand-refresh relic: IncreaseMagicPowerOnSpellsPlayed.`
After it, all complete states/RNG match, with eight immutable branches per
boundary, 105 trait snapshots, 40 active-state changes and 345 preserved
fixed offsets. Twenty-four malformed owner/effect/query signatures are
rejected without mutating their source.

```powershell
./scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellThresholdRelic -NativeWorker 2 -SkipBuild -TimeoutSeconds 180
./scripts/Check-Models.ps1 -Fixture room-moon-spell-threshold-relic-calibration.mt2f -SkipBuild
```

This component does not play cards or prove positive third-spell increments.
Full controlled battles and actual player/frozen replay are separate next
acceptance stages; the component alone is not a player victory claim.
