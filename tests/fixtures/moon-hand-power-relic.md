# Moon spell upgrades with original end-turn hand magic power

The unchanged `MoonPhaseRoomsGainMagicPower` owner
(`e96497a1-3c1e-4adf-b897-8ef09b931d1d`) applies its original
`MoonPhaseRooms_UpgradeData` (`ba427938-c3ad-480b-aa27-44a6426a059c`)
before hand discard on global Full. Each matching hand spell receives a
non-unique +10 damage/+10 heal upgrade that remains after discard. The original
positive/exclusion filters remain intact. Room Eclipse masks do not change
this global-phase gate.

Component `full-battle-no-cards-20261024-105706-0189aff3` retains 19 settled
native boundaries: the existing 15 room/phase/resource controls plus four
explicit original pre-discard callbacks. Three Full callbacks apply 15 upgrades
across the five unchanged moon spells, including repeated stacking in one
duration. New and nonmatching Lunacy provide nine exclusions. Subsequent phase
changes rescale installed upgrades by 5x/1x on Celestial Storm. Full states/RNG,
133 trait snapshots, 40 active changes, both room-mask modes and eight immutable
branches per boundary pass. Native exit is normal in 36.93 seconds, protected
originals unchanged. The old model rejects the first unmasked resource refresh;
the new composition guard admits only the calibrated owner, numeric upgrade,
phase/lifetime and filters. Amount/phase/uniqueness/filter/pool changes reject.

Full battle `full-battle-units-spells-and-junk-20261024-105844-c07af178` wins
at Pyre80 after 20 plays and five EndTurns, exiting normally in 123.80 seconds
with protected originals unchanged and all four counters zero. Two Full and
three New original callbacks retain seven positive moon-spell applications.
Their complete before/after contexts are independently replayed, in addition
to whole-turn states/RNG and 16 immutable full-battle chains. Lunacy retains
its original 2/3/0 paid controls.

The callback witness exposed one intermediate omission: native magic-power
reapplication dispatches CardUpdated, which removes/reappends Conduit's tracked
upgrade after the newly added hand upgrade. The model now reproduces that
dispatch immediately. Later discard refreshes had previously hidden this order
within settled turn snapshots; the original callback contexts remain the direct
fix evidence.

The scene authors durable enemies, strong Stewards and setup controls; original
spell and relic definitions remain unchanged. This is controlled composition
acceptance, separate from natural/default-budget victory. No full regression or
new Mac package is built.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellHandPowerRelic -NativeWorker 1 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-hand-power-relic-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-hand-power-relic.mt2f
```

The full scene flag is `-OriginalMoonCardUpgradesHandPowerRelic`; the dedicated
player gate is `--player-moon-room-hand-power-relic-pair-only`.

## Actual player source and independent native replay

Source `full-battle-units-spells-and-junk-20261024-110007-3c6ed95e` and
frozen replay `full-battle-units-spells-and-junk-20261024-110833-0c405e5e`
both win at Pyre80 after 48 inputs: 41 plays/abilities and seven EndTurns.
They exit normally in 460.03/199.90 seconds, preserve protected originals and
have all four failure counters zero. Every input, complete root, forecast,
successor and RNG matches, including 47 room stages and 13 train phases in
each process. The pair gate also checks both room-mask modes, all five spells,
paid Lunacy/Conduit and 16 immutable complete-battle branches.

Each process retains two Full and five New original pre-discard callbacks,
with two positive moon-spell upgrades and complete native before/after contexts.
Source started before the intermediate Conduit-order fix landed. Its complete
forecasts/successors and callback contexts are independently checked through
the corrected model; the fresh frozen process loads the corrected assemblies.
The paired full states remain exact without another redundant source run.

The source has 50 publications with 50 fresh complete-root checks and 400 pure
captures. Two executed decisions consume searches; nine ordinary and 37 winning
continuations reuse their verified plans. The overlay records ten searches in
total, including searches not consumed by an input. Frozen replay performs one
initial startup search, then zero searches/publications after pausing, with
97 pure captures. These counts are separate from response latency.

This uses six-turn/10000ms/top/tactical settings and the controlled scene above;
it is separate from a natural/default-budget victory. Curated files are
`full-battle-player-moon-room-hand-power-relic-{search,replay}.mt2f` and
`player-moon-room-hand-power-relic-accepted.json`. The focused wrapper is
`scripts/Check-PlayerAdvisor.ps1 -Case MoonRoomHandPowerRelic -SkipBuild`.
`--moon-hand-power-callbacks-only <native.mt2f>` isolates the original callback
check before spending time on another native replay. The existing owner-only
baseline's 16 recorded actions and six captured EndTurns still match; its
generic fixed-policy whole-battle gate cannot reproduce the harness adding
new witness cards between turns, so that policy result is not an acceptance.
