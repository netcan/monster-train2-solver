# Original Covenant true-final-boss birth

Four `true-final-boss-spawn-calibration-covenant{00,01,04,08}.mt2f` archives
compare complete native five-actor births with the pure model. Each process
activates one complete original Covenant definition, discloses its native
cheat jump to Ring9 and records normal exit with original files unchanged.
These are controlled component observations, not natural unlocks or wins.

| Definition | Source profile suffix | Seconds |
| --- | --- | ---: |
| Covenant00_FTUE | 20261023-072409-ab4aa564 | 26.80 |
| Covenant01_StrongerEnemiesMoreCards | 20261023-072757-fee27d6c | 26.92 |
| Covenant04_StrongerBosses | 20261023-072138-b27bd7d6 | 28.04 |
| Covenant08_StrongerEnemies | 20261023-072506-21d718fe | 26.79 |

Each source name starts `full-battle-no-cards-`. The manifest retains exact
archive hashes, byte lengths and node counts.

Native `GetEnemyStatAdjustmentsFromAllRelics` supplies five previews before
birth. Each query records unchanged frame and all sixteen RNG streams, the
original asset and exact active Covenant identity. The immutable definition
retains its raw attack, HP, modifiers and Horde values separately from the
previewed birth adjustment. Creation adds health once and records attack
adjustment in `DamageBuff`, preserving native raw/Horde attack.

| Boss | Baseline HP / attack | Covenant04 HP / attack |
| --- | ---: | ---: |
| Dominion | 1555 / 3 | 2055 / 3 |
| Savagery | 2750 / 10 | 3250 / 15 |
| Each of three Entropy bodies | 3500 / 15 | 4500 / 20 |

The other three original definitions leave these five actors' birth stats
unchanged. Complete model/native comparisons additionally cover Entropy
scourge generation, physical placement/anchors, shared life links, all context
registries and RNG. Each case matches 64 concurrent immutable branches;
missing or spoofed stat origins and repeated birth remain rejected.

Covenant01's original purge and card-set effects are called from
`ApplyStartOfRunRelicEffects`, and its special-character setting is consumed
by native spawn-pattern generation. Battle inputs retain the existing native
deck and spawn state. This does not implement or repeat startup deck mutation;
the route explicitly records `StartOfRunEffectsApplied=false`. Its earlier
unsupported capture stays a diagnostic; acceptance uses a fresh process
after that battle-interface classification was corrected.

Example capture:

```powershell
scripts/Run-FullBattleProbe.ps1 -TrueFinalBossBirth -TrueFinalBossSpawn -TrueFinalBossCovenant Covenant04_StrongerBosses -NativeWorker 1 -TimeoutSeconds 300 -SkipBuild
```

Focused checker: `ModelChecks --true-final-boss-spawn-only <archive>`.
The prior no-Covenant birth and the existing Covenant character-filter/wave
control also pass. No full regression or Mac build is performed.

The model admits one of these four original Covenant definitions only when
all five stat projections carry the matching captured origin. The production
capture's later-combat protection remains; multiple Covenants, natural rank
progression and full Covenant true-final-boss combat require separate evidence.
Ordinary rank-one wave capture also retains an explicit protection, so the
startup-interface classification does not silently admit uncalibrated births.
