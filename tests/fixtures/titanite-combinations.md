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
unit requires captured status order. The larger-list limit was subsequently
extended using the independent calibration below. Natural Fragile victory and
player entry require separate battle acceptance; these matrices alone do not
establish either.

## Large native status lists · 2026-10-23

`status-trigger-order-calibration.mt2f` retains 2,080 native dictionary/runtime
sorts spanning 2–66 active native statuses, thirty-two deterministic orderings
per size and retained zero-stack controls. Native priorities are regen1,
corruption2, trample3 and unstable4; all other captured definitions use zero.
Sorting all active entries before filtering the damage-shield stage reproduces
the original equal-priority reorder above sixteen entries. Using insertion
order alone was insufficient.

Another 192 native damage-stage controls cover 16/17/24 active statuses and
sixty-four orderings each. The complete model damage path agrees on health,
shield consumption, passive stacks and last-attack accounting: Titanite-first
blocks eleven of raw21; shield-first blocks twenty-one. Every root stays
immutable, sixteen concurrent branches agree, and missing captured dictionaries
or larger lists containing absent native `heal multiplier` / `inedible`
definitions are rejected. Those two model IDs are recorded as absent; no
synthetic definitions were installed. The model accepts at most the calibrated
66 active original statuses, retaining other definition guards.

Source `full-battle-no-cards-20261023-132002-c19c075d` exits normally in 52.07
seconds with `DEPTH-PASS`, protected originals unchanged and all four native
failure counters zero. The detached dictionary is restored in `finally`;
complete live decision, all RNG streams and Unity frame remain unchanged.
The original 1,536-row smaller matrix and its six native runtime sort controls
also pass. Failed `131451-0db9895a` only revealed an absent native status and
is not curated.

```powershell
./scripts/Run-FullBattleProbe.ps1 -Policy no-cards -StatusTriggerOrder -BranchCardMasks -PhysicalSpawnPoints -SettleDeathDissolves -SettleCardAnimations -NativeWorker 2 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --large-status-damage-only tests/fixtures/status-trigger-order-calibration.mt2f
```

This establishes pure status dispatch and full model damage, with original
definitions and no changes to the live unit. It does not claim a natural
campaign carrying all sixty-six statuses at once.

## Weakness stage order · 2026-10-23

`titanite-weakness-calibration.mt2f` retains 144 native stage operations from
`full-battle-no-cards-20261023-033656-8f441fe3` (48.79 seconds). The enclosing
native battle exits normally with `DEPTH-PASS`, settled effects, protected
originals unchanged and four zero failure counters. The calibration itself
uses a detached status dictionary and restores it in `finally`; live decision,
all RNG streams and frame count remain unchanged.

Native metadata puts Titanite/Damage Shield in `OnPreAttackedDamageShield`,
both weaknesses in `OnPreAttacked`, then Titanskin and armor in their later
stages. The old model multiplied weakness before halving: raw5 became five
instead of the native six. Moving weakness after the shield stage fixes both
melee and spell damage. Spell weakness remains marked for removal after the
card resolves; melee weakness is consumed immediately.

The matrix combines zero/even/odd damage, Titanite0/1, shield0/1, no defense,
armor3 or Titanskin2, and ordinary/piercing card or unit sources. The focused
checker replays actual unit attacks and card damage, compares native health,
passive stacks, shield/armor spending and weakness removal flags, and proves
each input immutable. The previous 1536-row matrix, 24-row float boundary
calibration and spell-weakness controls also pass; no full regression runs.

```powershell
scripts/Run-FullBattleProbe.ps1 -Policy no-cards -NativeWorker 2 `
  -TitaniteWeakness -TimeoutSeconds 400 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --titanite-weakness-only tests/fixtures/titanite-weakness-calibration.mt2f
```

This damage-stage proof does not grant natural Fragile winning acceptance.
