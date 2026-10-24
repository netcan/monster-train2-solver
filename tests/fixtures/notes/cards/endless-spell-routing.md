# Endless spell death routing

The original Angel Duo search rejected a legal candidate with
`Missing dead spawner routing during the trigger queue.`. Its recorded
twelve-turn scout yields 23 supported prefix inputs, ending at turn four.
The next input plays card 14 on Endless unit 10 in room zero; that unit's
spawner is card 2. The character queue returns this card to Draw, but the
spell's outer cleanup used to try routing it to Exhausted again.

Two fresh native processes resume the unmodified five-win seed424242,
Covenant2 checkpoint. Its save SHA-256 remains
`6317f0ff5c254722dac6ebc8892f50de1b80af78ee16a5493b8bc326df4865ba`.
No cards, units, health, statuses, relics or encounter definitions are injected.
The development driver plays only the recorded legal prefix and final
candidate, then stops after capturing its settled native successor. The actual
player projection stays active and pure while background search is paused.

Source `full-battle-units-spells-and-junk-20261024-081957-2480d547` observes
the old model rejection. Replay `full-battle-units-spells-and-junk-20261024-082345-9fa68299`
loads the repaired model and predicts that same native successor correctly.
Both exit normally, preserve protected original files and take 140.08/123.97
seconds. These are 24-input component observations, not terminal battle wins.

The independent gate checks every native root/input/successor and complete
state/RNG in both processes. The killed carrier is removed; card 2 appears
exactly once at the end of the unchanged Draw order, outside every other pile.
Its temporary upgrades survive, `TimesExhausted` is unchanged, and the repaired
action is present in normal candidate enumeration. Every replay root remains
immutable. The original source's missing expected successor remains null;
the fresh replay contains the supported prediction. No historical prediction
or full-battle failure counter is rewritten.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --endless-spell-routing-pair-only tests/fixtures/advisor/presentation/player-endless-spell-routing-calibration.mt2f tests/fixtures/advisor/presentation/player-endless-spell-routing-replay-calibration.mt2f
```

To observe another rejected branch from an original restored checkpoint:

```powershell
./scripts/Run-FullBattleProbe.ps1 -InputProfile <profile> -BattleDistance 5 -UnsupportedBranchInput <extracted-branch.mt2f> -OuterBossTurns -PhysicalSpawnPoints -BranchCardMasks -GameSpeed Instant -TimeoutSeconds 900
```

The scoped fix recognizes an already returned Endless carrier in Draw and
cancels deferred exhaustion. Other missing spawner routes still reject.
The original Endless combat-death controls and accepted Angel Duo player
source/replay also pass. No full regression or new Mac package is run.
