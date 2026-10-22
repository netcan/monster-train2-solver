# Titanite with shields and Titanskin

`titanite-combinations-calibration.mt2f` contains 1,536 original native status
stage comparisons from `full-battle-no-cards-20261022-121951-c61ab6f1`.
The isolated process exits normally in 48.16 seconds, passes its baseline
battle checks and leaves protected original files unchanged. Each comparison
uses a detached status dictionary, restored in `finally`; no native status
mutation, callback, frame advance or RNG restoration runs. Full live decision,
all RNG streams and Unity frame are unchanged after the matrix.

The matrix crosses both insertion orders, Titanite counts 1/3, Damage Shield
counts 0/1, Titanskin counts 0/2/5, armor counts 0/7, damage 0/1/2/3/9/10/21/255,
and ordinary/piercing card and unit sources. Original metadata places Titanite
and Damage Shield at `OnPreAttackedDamageShield`, both with trigger order zero.
Native sort controls at 2/5/8/16/17/32 entries establish the stable small-list
boundary. At most sixteen active statuses, the two effects retain dictionary
order. With damage 21, Titanite first makes Damage Shield block 11; shield
first blocks 21. Health is protected in both cases, but last-attack accounting
differs. Model dispatch now preserves the captured native order.

Titanskin runs at the later `OnPreAttackedTitanSkin` stage, before armor.
Unit piercing bypasses armor while retaining Titanite/Titanskin reductions;
card piercing bypasses all three. For 21 raw damage, Titanite + Titanskin2 +
armor7 yields 2 damage normally, 9 for unit piercing and 21 for card piercing.
Passive Titanite/Titanskin stacks remain; shield and armor consumption follows
the native stage outputs. The focused check exercises the complete model
damage path, health, last-attack accounting and root immutability for all rows.
It also runs the existing Titanskin controls with corrected unit-piercing
expectations.

```powershell
./scripts/Run-FullBattleProbe.ps1 -Policy no-cards -TitaniteCalibration `
  -TitaniteCombinations -PhysicalSpawnPoints -BranchCardMasks `
  -SettleDeathDissolves -SettleCardAnimations -SkipBuild -TimeoutSeconds 180

dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --titanite-combinations-only tests/fixtures/titanite-combinations-calibration.mt2f
```

Scope: pure native status-stage behavior. A combined Titanite/Damage Shield
unit requires captured status order and at most sixteen active statuses.
Larger status lists remain guarded because native equal-order sorting changes
their order. Natural Fragile victory and player entry require separate battle
acceptance; this matrix alone does not establish either.
