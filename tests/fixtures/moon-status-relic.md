# Moon spell upgrades with original Silent armor

The unchanged `MoonPhaseAddArmorToSilent` owner
(`e013b17d-753b-47b8-a837-02af85addb4b`) adds 10 Armor to live friendly
Silent units on global New. Full and room Eclipse masks do not change that
global phase gate. The composition guard reuses the original owner's exact
identity, effect, subtype, status/lifetime and phase validation. Unknown
owners/payloads and unsupported target status callbacks still reject.

Component `full-battle-no-cards-20261024-112555-ec49c61f` retains 15 settled
native boundaries with the five original moon spells, original Lunacy, an
original Silent Sentinel and a non-Silent Steward on separate floors. The
actors are created cardless for this component. Two New boundaries each add 10
Armor to Silent; 13 room/phase/resource/selection controls add none, and Steward
is excluded throughout. The previous model rejects the first unmasked resource
refresh. The new focused check matches all complete states and gameplay RNG,
105 trait snapshots and eight immutable branches per boundary. Changed armor
amount, phase mask or required subtype reject. Native exit is normal in 35.14
seconds; protected originals remain unchanged.

The full native scene uses a Silent Sentinel as the second durable actor;
its original subtype, triggers and ability definitions remain unchanged while
cost/attack/health/size are explicit controlled witnesses, like the Steward.
Full battle `full-battle-units-spells-and-junk-20261024-112745-5bf2ba10`
wins at Pyre80 after 20 plays and five EndTurns, exiting normally in 148.11
seconds with protected originals unchanged and all four counters zero. Complete
states/RNG, 32 room stages, nine train phases and 16 immutable whole-battle
chains pass. Five New and six Full callbacks match complete original before/
after train states, retaining five positive armor applications and 11 non-Silent
exclusions. Original Lunacy retains the 2/3/0 paid controls. Actual player and
independent frozen replay acceptance are pending.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellStatusRelic -NativeWorker 1 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-status-relic-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-status-relic.mt2f
```

Full scene: `-OriginalMoonCardUpgradesStatusRelic`.
Dedicated player gate: `--player-moon-room-status-relic-pair-only`.
The callback-only gate is `--moon-status-callbacks-only`.
