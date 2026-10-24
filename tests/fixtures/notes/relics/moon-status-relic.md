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
exclusions. Original Lunacy retains the 2/3/0 paid controls.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellStatusRelic -NativeWorker 1 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/relics/modifiers/room-moon-spell-status-relic-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/relics/modifiers/full-battle-original-moon-card-upgrades-status-relic.mt2f
```

Full scene: `-OriginalMoonCardUpgradesStatusRelic`.
Dedicated player gate: `--player-moon-room-status-relic-pair-only`.
The callback-only gate is `--moon-status-callbacks-only`.

## Repeatable player-pair workflow

`scripts/Run-MoonSpellRelicPlayerPair.ps1 -Relic Status -SkipBuild` runs the
actual player source, an independent frozen replay, the dedicated pair gate
and a hashed acceptance receipt in the source profile. It uses the same
controlled six-turn/10000ms/tactical scene. The seven selectors are `Cost`,
`Attack`, `Healing`, `PhaseHeal`, `PhaseEnergy`, `HandPower` and `Status`.

Pass `-SourceProfile` to reuse a completed source and run only its replay;
pass both `-SourceProfile` and `-ReplayProfile` to recheck existing evidence
without another game process. Reuse verifies the original native summary,
trace hash, declared relic, actual player capture and source/frozen roles.
Scripted baselines are rejected before replay. Status and HandPower also
check complete source callbacks before starting another native process.
Receipts are emitted only after the independent pair gate passes and both
trace hashes are rechecked. Curated fixture promotion remains a separate step.

## Actual player source and independent native replay

Source `full-battle-units-spells-and-junk-20261024-112833-a51cce3d` and
frozen replay `full-battle-units-spells-and-junk-20261024-113757-042bf936`
both win at Pyre80 after 50 inputs: 43 plays/abilities and seven EndTurns.
They exit normally in 535.45/240.93 seconds, preserve protected originals
and have all four failure counters zero. Every complete root, forecast,
successor and RNG matches, with 47 room stages and 13 train phases per process
and 16 immutable whole-battle branches.

Each process retains 26 New and 28 Full original callbacks, 26 positive armor
applications and 54 non-Silent exclusions. Complete callback train states
match independently. The pair retains all five original moon spells, both
room-mask modes, 20 masked casts and 16 original Lunacy casts with 84 paid
advances. Each process also executes two original Silent Sentinel abilities
(`SpellSlamAbility`, `7617850d-a72f-46de-87e4-dbcaa50fe1c2`). The player checker
now classifies those transient ability spells separately from hand instances;
their complete forecasts/successors still pass the shared pair replay.

Source has 52 publications with 52 fresh complete-root checks and 419 pure
captures. Two executed inputs consume searches; nine ordinary and 39 winning
continuations reuse their verified plans. The overlay records ten searches
overall, including work not consumed by an executed input. Frozen replay has
one initial startup search, then zero searches/publications after pausing,
with 101 pure captures. These counts are separate from response latency.

The reusable script above passed the live resumed-source/fresh-replay workflow,
including callback preflight and writing its receipt only after pair acceptance.
It also rejected a scripted baseline and a frozen replay supplied as a source
before starting another game. All seven selectors map to real native switches;
the other six compositions were not rerun in this acceptance.

Curated files are `full-battle-player-moon-room-status-relic-{search,replay}.mt2f`
and `player-moon-room-status-relic-accepted.json`. The focused wrapper is
`scripts/Check-PlayerAdvisor.ps1 -Case MoonRoomStatusRelic -SkipBuild`.
This is controlled composition acceptance with six-turn/10000ms/tactical
settings, separate from natural/default-budget victory. No full regression
or new Mac package is built.
