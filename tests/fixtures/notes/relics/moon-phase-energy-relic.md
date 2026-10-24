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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/relics/modifiers/room-moon-spell-phase-energy-relic-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/relics/modifiers/full-battle-original-moon-card-upgrades-phase-energy-relic.mt2f
```

The full scene uses `-OriginalMoonCardUpgradesPhaseEnergyRelic` and the dedicated
player gate is `--player-moon-room-phase-energy-relic-pair-only`. That gate
requires explicit original-relic provenance in both processes and actual
multi-turn Full gains/New controls with matching condition histories.
No full regression or new Mac package is used.

## Native player pair

Source `full-battle-units-spells-and-junk-20261024-103737-2f0a5bd4` and
independent frozen replay `full-battle-units-spells-and-junk-20261024-104339-01a6c138`
both win at Pyre80 after45 inputs (40 plays/abilities,five EndTurns).
They exit normally in360.04/164.20 seconds,preserve protected originals and
have all four failure counters zero. Every input,complete root,forecast,
successor and RNG matches,including36 room stages and ten train phases in
each process. Each has five positive Full callbacks and four New controls
that grant no energy,with the exact original once-per-turn histories.
The gate checks paid Lunacy costs separately from energy refunded afterward,
original Conduit amounts,all five spells,both room-mask modes and16 immutable
complete battle branches. The dedicated player gate passes.

The source has47 publications with47 fresh complete-root checks and374 pure
captures. One executed input consumes a search;44 winning continuations reuse
its verified route. The overlay records six searches overall,including work
not consumed by an executed input. Frozen replay has one initial startup
search,then zero searches/publications after pausing and91 pure captures.
These counts are separate from end-to-end response time.

The controlled scene retains original spell/relic definitions but authors
durable enemies,strong Stewards and harness setup. Six-turn/10000ms/top/
tactical settings remain separate from natural/default-budget victories.
Curated files are `full-battle-player-moon-room-phase-energy-relic-{search,replay}.mt2f`
and `player-moon-room-phase-energy-relic-accepted.json`. The focused wrapper
is `scripts/Check-PlayerAdvisor.ps1 -Case MoonRoomPhaseEnergyRelic -SkipBuild`;
the full-suite source hook selects the same pair gate. Hashes/registrations
are checked separately without running the full regression.
