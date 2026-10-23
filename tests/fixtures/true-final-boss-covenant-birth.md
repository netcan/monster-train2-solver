# Original Covenant true-final-boss birth

The `true-final-boss-covenant-battle-{00,01,08}.mt2f` controls also finish
complete native no-cards losses with zero capture/mismatch/unsupported/pending
counters, normal exits and protected files unchanged. Each has five EndTurns,
ten train phases, 41 room phases and sixteen independent immutable full chains.
Their 26 authored ordinary-wave definitions match pure native stat previews;
spoofed Covenant origins are rejected. There are thirteen distinct wave
assets plus three unused player-template control queries; all sixteen queries
preserve frame and every RNG stream. Capture now queries only actual waves.

| Covenant | Complete native source |
| --- | --- |
| 00 | `full-battle-no-cards-20261023-081358-7cbcd476` |
| 01 | `full-battle-no-cards-20261023-081149-bc5dac5d` |
| 08 | `full-battle-no-cards-20261023-081149-734c0bf1` |

Ordinary-wave capture retains authored/Horde bases and projects native birth
adjustments separately. A pure preview reads the exact original active
Covenant; the model requires matching origins, modifier snapshots and bounded
attack/health arithmetic. Other ranks and combined ranks retain their guards.

`true-final-boss-covenant-battle-04.mt2f` extends the stronger-boss control
through a complete original no-cards loss. Source
`full-battle-no-cards-20261023-080549-fef7e51c` exits normally with protected
files unchanged and all four native failure counters zero. Six EndTurns,
ten train phases, forty room phases and sixteen independent immutable full
chains match complete native state/RNG. Dominion/Savagery/Entropy retain
2055/3250/4500 maximum HP and the native attack adjustments. No health,
phase, encounter, card or wave substitution is applied. This remains an
explicit native jump with a single original Covenant activated after run
startup; it is neither a natural high-rank campaign nor a win.

Run `ModelChecks --true-final-boss-covenant-battle-only <archive>` for the
dedicated native-counter, origin, stat, room/train and full-chain acceptance.
Missing/spoofed origins and off-by-one stat adjustments are rejected. The
single-Covenant04 player gate now also passes actual-player advice and fresh
native replay, documented in [player evidence](player-true-final-boss-covenant04.md).
Other player Covenants, combinations and terminal/relentless extensions remain
guarded pending separate native evidence.

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
capture's other-rank protection remains; multiple Covenants, natural rank
progression and further Covenant combat signatures require separate evidence.
Ordinary rank-one wave capture also retains an explicit protection, so the
startup-interface classification does not silently admit uncalibrated births.
