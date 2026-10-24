# Moon spell upgrades with original phase-healing relic

The exact original `MooncycleRestoreFriendlyHealth`
(`6d977136-5aac-44fd-9843-54cf2a0bd01d`) heals the weakest damaged friendly
unit for10 whenever the global moon phase changes. It previously blocked
moon-owned spell upgrades through the conservative composition guard.
The unchanged native component reproduces the rejection before any fix:
`unmasked-full: Moon spell upgrades require captured phase/selection; active
relic combinations need separate witnesses.`

Native component `full-battle-no-cards-20261024-100544-39075015` exits normally
in37.66 seconds with protected originals unchanged. It runs the original five
moon-upgrade spells,Lunacy and room attachment through15 settled coroutine/
trigger-queue boundaries. An original Steward is explicitly wounded to1 HP
before each captured boundary as an isolated control. Four global phase
changes heal exactly10; eleven resource/room/selection controls do not heal.
No gameplay RNG changes. Complete states match,with eight immutable branches
per boundary,105 trait snapshots,40 active changes,ten Exclusive and25 Additive
masked-Full witnesses. Changed healing amount,phase mask or conditions reject.

The model admits only that exact original owner/effect/target/amount/mask/
condition signature. Its healing runs in the existing ordered moon callback
pipeline; resource and selected-room refresh still do not dispatch healing.
Other uncalibrated signatures remain protected.

Fresh full battle `full-battle-units-spells-and-junk-20261024-101925-a522ff69`
wins at Pyre80 after20 plays and five EndTurns. It exits normally in139.35
seconds,leaves protected originals unchanged and has all four failure counters
zero. All native successors/full states/RNG and16 independent complete chains
match,including original Lunacy paid costs2/3/0. Eleven settled original relic
callbacks cover New/Full phases,including four positive heals. Earlier captures
without those callback records are not used for this strengthened gate.
The spell and relic definitions
remain original; the controlled battle authors durable enemies,strong
Stewards and other harness setup. It is separate from natural/default-budget
player-victory acceptance.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellPhaseHealRelic -NativeWorker 2 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-phase-heal-relic-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-phase-heal-relic.mt2f
```

## Native player pair

Fresh source `full-battle-units-spells-and-junk-20261024-101935-cb389612`
and independent frozen replay `full-battle-units-spells-and-junk-20261024-102718-b0131420`
both win at Pyre80 after48 inputs (41 plays/abilities,seven EndTurns). They
exit normally in460.38/196.98 seconds,preserve protected originals and have
all four failure counters zero. Every input,complete root,forecast,successor
and RNG matches,including47 room stages and13 train phases in each process.
Both retain54 settled native New/Full callbacks with two positive heals.
The pair covers five original spells,Lunacy and room masks,with16 immutable
complete battle branches. Original definitions remain unchanged; the controlled
scene and six-turn/10000ms/top/tactical settings are separate from natural
default-budget campaign victories.

The source has50 publications with50 fresh full-root checks and402 pure
captures. Two inputs consume searched forecasts;nine ordinary and37 winning
continuations reuse verified plans. The overlay records ten searches overall,
including work not consumed by an executed input. The frozen process records
one initial startup search,then zero searches/publications after pausing and
97 pure captures. This does not establish an end-to-end latency guarantee.

The dedicated `--player-moon-room-phase-heal-relic-pair-only` gate passes and
requires explicit provenance in both processes alongside all forecast/state/
RNG controls. Curated files are
`full-battle-player-moon-room-phase-heal-relic-{search,replay}.mt2f` and
`player-moon-room-phase-heal-relic-accepted.json`. The same gate is selected by
`scripts/Check-PlayerAdvisor.ps1 -Case MoonRoomPhaseHealRelic -SkipBuild` and
the full-suite source hook. Archive hashes/registrations are checked separately;
the full regression is not run and no new Mac package is built.
