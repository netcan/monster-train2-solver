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

## Controlled complete battle

`relics/modifiers/full-battle-original-moon-card-upgrades-threshold-relic.mt2f`
retains isolated profile `full-battle-units-spells-and-junk-20261025-020426-3ca445c6`.
The original owner and five moon spells, Eclipse and Lunacy remain unchanged.
As in the existing composition controls, durable units/enemies, frozen hand,
numeric upgrades and authored energy/phase cards make each boundary visible.
Two extra zero-cost Full phase controls make the third spell occur at Full;
Eclipse is a room card and does not contribute to the spell count.

The native battle wins with Pyre80 after 22 plays / five EndTurns in 121.82s,
with all four failure counters zero, 95 complete native branch contexts and
the protected real files unchanged. One third-spell activation adds 20
scalable-card offsets, including three actual fivefold Celestial Storm
increments. Seventeen later card plays preserve the once-per-turn limit;
three non-scalable/non-spell exclusions remain unchanged. Subsequent New/Full
refreshes preserve these fixed offsets. The focused complete-battle checker
also checks the full hand callbacks, original paid Lunacy at 2/3/0 and
sixteen immutable full state/RNG chains.

```powershell
./scripts/Run-FullBattleProbe.ps1 -OriginalMoonCardUpgradesThresholdRelic -NativeWorker 2 -SkipBuild -TimeoutSeconds 240
./scripts/Check-Models.ps1 -Fixture full-battle-original-moon-card-upgrades-threshold-relic.mt2f -SkipBuild
./scripts/Run-MoonSpellRelicPlayerPair.ps1 -Relic Threshold -NativeWorker 2 -SkipBuild
```

Explicit selections of the two threshold component/battle fixtures now use
their dedicated gates without starting unrelated public pure checks. The
normal regression path retains its existing checks. Actual player search and
fresh frozen replay remain a separate acceptance stage; the controlled
policy victory alone is not player-search acceptance.
