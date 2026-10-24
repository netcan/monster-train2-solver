# Moon spell upgrades with original Full-moon energy relic

Original `MoonPhaseModifyEmber` (`70096a91-220a-4ab3-b8d0-b8fe8f3a819c`)
adds one energy on Full once per turn. The five original moon spell upgrades,
Lunacy and room mask previously reject this combination at `unmasked-full`:
`Moon spell upgrades require captured phase/selection; active relic combinations
need separate witnesses.` Unchanged native observation
`full-battle-no-cards-20261024-103315-c02ed351` reproduces the rejection.
That first observation has already consumed the original once-per-turn condition,
so it is not used as a positive energy witness.

Fresh component `full-battle-no-cards-20261024-103517-32b26f4f` invokes the
original public `UpdateConditionsOnDurationEnd(ThisTurn)` once before the
captured rows,then keeps the live condition history across all15 boundaries.
This explicit isolated control does not advance a natural combat turn.
The first Full transition adds one energy;the second Full transition in that
duration adds none. New transitions and eleven room/resource/selection controls
do not grant energy. The full state,condition histories and RNG match with
eight immutable branches per boundary,105 trait snapshots,40 active changes,
ten Exclusive and25 Additive room-mask witnesses. Changed amount,phase mask
or trigger threshold reject. Native exit is normal in31.84 seconds and
protected originals are unchanged.

The composition guard admits only the exact original owner/effect,Full mask,
amount,preview policy and once-per-turn query signature. Existing ordered
moon/resource handling performs the energy change;no new arithmetic is added.
Full-battle and actual-player/independent frozen-pair acceptance remain separate.

Fresh full battle `full-battle-units-spells-and-junk-20261024-103625-18c3ad3b`
wins at Pyre80 after20 plays and five EndTurns. Normal exit takes123.01
seconds;protected originals are unchanged and all four counters are zero.
Three original Full callbacks grant+1 across distinct turns;two New callbacks
leave energy and condition history unchanged. Complete native successors,
hand refreshes and RNG match with16 immutable full-battle branches. The original
Lunacy controls now pay4/3/0,retaining even/odd/zero phase and paid-Conduit
witnesses. Actual paid cost is captured before casting;any Full refund follows
that payment and cannot change its phase-advance/Conduit count. The checker
accounts for that exact once-per-turn refund alongside full-state comparison.
The controlled scene authors durable enemies,strong Stewards and harness setup;
spell and relic definitions remain original. It is not a natural/default win.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellPhaseEnergyRelic -NativeWorker 1 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-phase-energy-relic-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-phase-energy-relic.mt2f
```

The full scene uses `-OriginalMoonCardUpgradesPhaseEnergyRelic` and the dedicated
player gate is `--player-moon-room-phase-energy-relic-pair-only`. That gate
requires explicit original-relic provenance in both processes and actual
multi-turn Full gains/New controls with matching condition histories.
No full regression or new Mac package is used.
