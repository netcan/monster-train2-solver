# 游戏 2.3.1 迁移 · 2026-11-14

本机 Steam 已更新到 2.3.1，build `26650011`。采样工具已完成基础接口适配，
同一份新版编译产物可以在 2.2.1 和 2.3.1 中采集目录并跑完七回合基础战斗。
新内容与变更规则仍在迁移，旧版卡牌和遗物的覆盖率不能直接算作新版覆盖率。

## 版本及目录变化

| 项目 | 2.2.1 | 2.3.1 |
| --- | ---: | ---: |
| 原生卡牌定义，含技能和内部牌 | 668 | 724 |
| 图鉴卡牌 | 563 | 614 |
| 可收集遗物 | 224 | 236 |
| 八类遗物/契约/增强等定义合计 | 662 | 676 |

按稳定定义 ID 对比：卡牌增加 65、移除 9，净增 56；图鉴卡牌增加 51、
没有移除。共有卡牌中，10 个定义的 Effects/Traits/Triggers 有变化，15 个
模型 Rule 投影有变化。这两个数量衡量不同字段，不可相加当作变更卡牌总数。
可收集遗物增加 12、没有移除，另有 13 个共有定义变化。
这些是相同采集代码下的定义差异，不能证明其他原生运行逻辑没有变化。

| 版本 | Assembly-CSharp MVID | SHA256 |
| --- | --- | --- |
| 2.2.1 | `8fb07b96-f4db-4d2b-884d-c00536d6ccf4` | `2cb423be8346a1e72c263b98b64a9450b0e38aef2f3ff6cfaeb15e7af9e7efdf` |
| 2.3.1 | `bf93f060-beda-46b1-b808-95ebf1e9a99d` | `49c71329eb27ae77462e6e59e1fd300d6a249dba95af5b22f9aea766432e27b9` |

新版 StreamingAssets build 标识：
`17948,51a921d3af0927dc11cf6b8716c301b27f86ce2c`。

## 已验证的兼容范围

- 新版移除了预演暂停 Boolean，改为 Push/Pop 计数接口。兼容层只释放采样器
  自己持有的计数，保留原生调用方的计数；新旧原生组件均通过重复设置、嵌套保存、
  外部持有者和最终恢复检查。
- 旧版前排 Deathwish 在新版改名为 Vanguard，使用独立的 `vanguardSources`；
  新版 Deathwish 则按生命比例不高于 50% 判定，使用 `deathwishSources`。
  采集分别保留两类成员关系；旧版仍读取 `deathWishSources`。新版半血触发
  带显式 `HalfHealth` 标识。半血条件、入队和非致命直接伤害/治疗的回调生命周期
  已独立校准；最大生命另有 23 项、直接数值升级另有 27 项 API/队列边界及
  嵌套效果校准。数值升级卡牌效果及真实召唤牌回写另有 28 项组件证据。
  升级附带的触发/技能/状态、累加缩放、死亡复活、共享世界和整层调度仍受防护。
- 组件归档的版本号取自运行游戏；目录采集校验本次原生枚举数量和唯一 ID，
  不再把旧版 668 个定义写死在采集器中。旧版固定目录验收仍保留。
- EndTurn 调用后，原生协程有时尚未推进就暴露旧的 ready 状态；复放器在原有
  超时范围内继续等待回合数推进，再校验完整后继。
- 两版都取得 `no-cards` 七回合原生胜局，采集失败、差异、未支持转换、待完成
  记录四项均为零。独立模型分别从一个根状态连续推完七回合，逐决策比较状态及
  RNG，再执行 16 路不可变并发分支，全部通过。
- Probe 和 Advisor 均可针对新版程序集编译。尚未验收新版实际玩家会话中的建议，
  本次工作没有替换 Steam 安装目录内的玩家 Mod。

纯结束回合基线没有出牌，不计作新卡牌、遗物效果、复杂战斗或搜索胜率的验收。
目录投影没有异常也不等于所有效果都已实现。

## Vanguard 组件进展

原生 `TryUpdateVanguard` 的前排判定、silenced/muted 限制和来源集合迁移已接入。
新版组件矩阵 41 项独立复放通过：74 次状态 API 调用、135 份完整队列载荷、
金币/护甲子效果、共享世界续推、父状态不可变及 32 路分支均匹配。其中五项
专门覆盖前排来源激活、mute/解除、untouchable 引起的前排切换和返回。
旧版同一 41 项矩阵及旧版装备成长遗物的完整策略复放也通过。

```powershell
pwsh -NoProfile -File scripts/Check-Game231Vanguard.ps1
```

新版样本为 `mechanics/vanguard-enchantment.mt2f`，单独由其 `manifest.tsv` 固定
哈希和字节数；通过的组件采样来源是
`.probe-runs/full-battle-steward-once-20261114-124724-735d6888`。
组件自身 `LiveContextUnchanged=true`、`Mismatches=0`，五项 Vanguard 的完整来源
成员关系、独占触发 ID 和回调效果通过独立检查。组件矩阵要求两个敌方来源和
同层友方列车员目标，采样器现显式等待这个前提。

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -EnchantmentCombatOnly -GameSpeed Instant -SkipBuild
```

该次采样约 34 秒，原生 `DEPTH-PASS`、进程退出码零及玩家文件未变均通过。
它与较早的整场模式组件归档逐字节相同，SHA256 为
`19338ac248b2958af9d3885094eac9363cafe4ba4489219d03321d658b9f2596`。
组件模式在完整 41 项采样及现场恢复通过后退出，后续无需重复跑完外围战斗。

较早两组整场采样各有四项统计缓存差异，失败记录保留用于定位，不作为成功样本。
现已接入新版 `OnMonstersDamaged` 的四个原始计数及卡牌缓存刷新，区分友方来源、
护甲与过量伤害，预演不累计，回合切换只清本回合计数。旧版捕获保留空标识，
不会因此改变旧版缓存刷新规则。依赖新统计的卡牌更新回调仍显式返回未支持。

独立原生组件覆盖 42 次增量调用（21 次预演对照）、无操作、惰性缓存、带符号
整数溢出、回合/整场重置和六项查询；43 项转换各执行 32 路不可变分支。
重新采集的列车员七回合战斗四项计数全零，55 项房间和 14 项列车转换、
从根状态连续复放及 16 路独立分支通过。该次原生采样约 48 秒，玩家文件未变。

```powershell
pwsh -NoProfile -File scripts/Check-Game231MonsterDamage.ps1
```

二进制样本及哈希保存在 `mechanics/monster-damage/`。组件来源为
`.probe-runs/full-battle-units-spells-and-junk-20261114-125708-7cfaaf97`，
七回合战斗来源为 `.probe-runs/full-battle-steward-once-20261114-125515-ed0b3633`。
装备成长遗物的新采样同样四项计数全零，并通过新版数值定义的专用独立验收：
24 次完整出牌、5 次 EndTurn、最终薪火 80，以及策略从根状态复放、战斗中间
根状态和 16 路不可变并发分支。40 项房间与 9 项列车转换也匹配。

该场景覆盖 `UpgradeEquipmentOnDeath`、`AdvancedGrowth`、
`FriendlyUnitsGainDeathwishValor`、`FriendlyUnitsGainDeathwishPermanentHealth`
四件原有遗物，其中后两件在新版使用 Vanguard。永久生命遗物的原生增益
由 +6 改为 +8；专用检查按精确版本/MVID 分别验证，三个战斗结束应用均匹配，
旧版 +6 回归同时通过。新样本保存在 `mechanics/equipment-growth/`，约 61 KiB，
原生采样约 115 秒，玩家文件未变。

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-equipment-growth-only tests/compatibility/game-2.3.1/mechanics/equipment-growth/full-battle.mt2f
```

这些是四件原有遗物的新版证据，不能据此沿用旧版 224 件总覆盖数。
新增卡牌/遗物的效果覆盖不由此获得验收。

## 半血 Deathwish 条件与队列

27 项原生矩阵验证半血进入/离开、重复更新、奇数最大生命、零/负 HP、零/负
最大生命、99999 合法上限两侧、mute/silence 和 untouchable 对照。
原生 `SetHealth` 会把高于 99999 的 HP/最大生命截断，矩阵使用合法值验证；
模型计算保持原生 Single 除法。零血对象退出房间枚举后，直接更新仍可作用于
保留的对象，样本显式补录这类对象，避免把“没有出现在房间中”当成没有触发。

原生队列还验证：Vanguard 对全体单位更新完毕后才开始半血 Deathwish，来源
集合与独占 trigger ID 分开保存；直接更新不受房间管理器的禁用或重入标记限制，
全局 enchantment 更新则遵守这两个标记。27 项完整后继和队列载荷独立复放及
每项 32 路不可变分支通过，旧版前排 Deathwish 和新版 Vanguard 的 41 项矩阵
回归通过。原生采样约 29 秒，现场/玩家文件未变，全部随机流及帧均未变化。

```powershell
pwsh -NoProfile -File scripts/Check-Game231HalfHealthDeathwish.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -HalfHealthDeathwishOnly -GameSpeed Instant -SkipBuild
```

样本在 `mechanics/half-health-deathwish/activation.mt2f`，哈希由同目录 manifest
固定，来源为 `.probe-runs/full-battle-steward-once-20261114-131651-f51c0d60`。
该矩阵验证条件与队列入队，主动保留未执行的队列边界。受伤先 OnHit、治疗先
OnHeal，再进行半血更新；最大生命、触发安装/移除与复活也有独立调用点。
伤害/治疗的直接 API 后继现由下述独立样本验证；其余效果生命周期仍须逐项
原生复验，整层主战斗转换的 `HalfHealth` 防护继续保留，不把组件通过计作全部
半血卡牌、遗物或整场搜索的验收。

## 半血 Deathwish 伤害与治疗生命周期

13 项原生协程验证非致命直接伤害和治疗：进入/离开半血、保持原有状态、零伤害、
零治疗、护甲完全吸收及满血截断。嵌套对照包括 OnHit 先自疗、OnHeal 先自伤，
以及半血获得回调自疗后失去、失去回调自伤后重新获得。模型在 OnHit/OnHeal
队列完成后才检查半血，正在执行队列时追加独占回调，保持原生执行顺序。
两项自伤对照也修正了最后攻击者记录：原生不会把自己登记为最后攻击者。

样本使用原生敌方宿主和现场创作的金币/伤害/治疗回调；完整房间、上下文、触发
成员关系、执行标记及全部回调载荷逐项匹配。原生金币调整将该场景的 2/3/5/7
奖励各变为 5，检查使用调整后的实际值，并单独比较回调及独占来源身份。
每项执行 32 路不可变并发分支，额外检查父状态不变及未校准入口继续拒绝。

原生台词与表现会消耗 `Chatter`、`NonDeterministic` 随机流，原始前后值全部保留；
其余每条随机流及 Unity RNG 在每步都不变，退出前全部随机流和现场上下文恢复。
该边界允许协程推进帧，不沿用条件组件的同帧断言。

```powershell
pwsh -NoProfile -File scripts/Check-Game231HalfHealthLifecycle.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -HalfHealthLifecycleOnly -GameSpeed Instant -SkipBuild
```

样本 `mechanics/half-health-deathwish/lifecycle.mt2f` 为 9,803 字节，由同目录 manifest
固定哈希，来源 `.probe-runs/full-battle-steward-once-20261114-133558-78d06ace`。
原生约 26 秒，组件差异零、`DEPTH-PASS`、退出码零和玩家文件未变均通过。
旧版治疗触发、攻击触发及依赖最后攻击者的按血伤害三份房间/列车回归通过。

本次开放直接非预演伤害/治疗 API，最大生命由下述独立样本验收。致命伤害与复活、触发安装/移除、
共享光环世界、外部伤害回调队列、原生法术分阶段 API 及整层调度仍受保护；
尚未获得新增半血卡牌/遗物或整场搜索的完整验收。

## 半血 Deathwish 最大生命生命周期

23 项原生协程同时记录 API 返回时和显式执行队列后的完整状态。增加最大生命
并回血时，治疗内部会执行半血队列；不回血的增加、无法接受治疗及非致命削减
最大生命，则先返回待执行的独占回调。九项返回边界保留共 18 份完整队列载荷，
随后执行效果；检查逐字段核对两个边界的状态、成员关系、回调及来源身份。

矩阵包含回血/不回血、扣减 HP/仅截断、生命下限、99999 上限、零/负参数、
治疗免疫绕过、治疗倍率、无法接受治疗、OnHeal 先自伤，以及获得/失去回调
再触发治疗或伤害。新增一项回调内增加最大生命，验证在同一个执行队列中追加
半血回调，避免另建引擎而越过先前已入队的工作。卡牌效果入口继续保留临时
召唤牌偏移及 OnHeal 抑制，原生角色 API 则不写该偏移。

23 项 API 返回后继和 23 项队列执行后继独立复放通过，每项另执行 32 路不可变
并发分支。逐步检查全部游戏逻辑随机流和 Unity RNG 不变；台词/表现的两条流
保留原始观察，最终与其他随机流、单位、触发器及上下文一起恢复。

```powershell
pwsh -NoProfile -File scripts/Check-Game231HalfHealthMaximum.ps1
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --unit-health-pure
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -HalfHealthMaximumOnly -GameSpeed Instant -SkipBuild
```

样本 `mechanics/half-health-deathwish/maximum-health/maximum-health.mt2f` 为 10,795
字节，其子目录 manifest 固定哈希；来源
`.probe-runs/full-battle-steward-once-20261114-135128-3642a4cc`。原生约 31 秒，组件
差异零、`DEPTH-PASS`、退出码零及玩家文件未变均通过。旧版最大生命法术、
致命削减和触发治疗三份房间/列车回归，以及既有最大生命纯模型检查通过。

直接数值升级的分段修改见下一节。最大生命致死/复活、菌群伤亡组合、
半血触发安装/移除、预演、共享光环世界或完整战斗搜索仍需另外验收。

另一次玩家光环来源互相施加菌群的观察
`.probe-runs/full-battle-units-and-junk-20261114-123510-0d0945c7`，在最终菌群移除
导致两来源死亡时，模型保留来源 ID 而原生活跃投影为空，且采样现场未恢复。
该观察未通过，也不计入当前组件覆盖；来源死亡与保留状态须另行隔离校准。

## 半血 Deathwish 的直接数值升级

27 项原生组件按 `ApplyCardUpgrade` / `RemoveCardUpgrade` 的真实步骤校准：
普通生命、未回血生命及升级归属的额外生命分别修改并更新半血成员关系。
回血阶段执行当前队列后，后续阶段从回调后的单位状态继续；添加升级最终
执行队列，移除升级则保留未被回血阶段执行的回调。不能把三个阶段合并成
一次净生命变化，也不能在各阶段创建互相独立的队列。

矩阵含正负/零生命、回血与不回血、增加和移除、生命上限、治疗倍率/免疫、
无法接受治疗、三段变化中的失去/获得/再次失去，以及回调内伤害、治疗、
增加最大生命。回调后的升级列表和归属生命账目随单位一起刷新。

每项同时核对 API 返回状态/待执行队列及显式执行队列后的完整后继，包含
独占来源 ID、全部调度载荷和游戏 RNG；独立复放另执行 32 路不可变分支。
归档及来源见 `mechanics/half-health-deathwish/unit-upgrades/manifest.tsv`。

12,180 字节归档来源为 `.probe-runs/full-battle-steward-once-20261114-140637-6f059c6c`，
原生约 27 秒，差异零、退出码零、`DEPTH-PASS` 及玩家文件未变。直接升级、
濒死升级、菌群升级和濒死菌群升级四份旧版归档共 227 个房间阶段、56 个列车
阶段通过；既有半血伤害/治疗、最大生命和最大生命纯模型检查也通过。

```powershell
pwsh -NoProfile -File scripts/Check-Game231HalfHealthUpgrade.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -HalfHealthUpgradeOnly -GameSpeed Instant -SkipBuild
```

本项验收直接角色 API 的非致命数值升级；卡牌效果的召唤牌回写见下一节。
附带触发/技能/状态的升级、累加缩放、外部正在执行的直接 API 队列、死亡复活、
菌群伤亡、预演、共享世界和整层战斗仍需分别校准。

## 半血 Deathwish 的数值升级卡牌效果

28 项原生 `CardEffectAddCardUpgradeToUnits`、`CardEffectAddTempCardUpgradeToUnits`
及 `CardEffectRemoveTempUpgradeFromUnit`，在有真实召唤牌的列车员上校准。
组件覆盖三种寿命、正负普通/未回血生命、重复移除全部副本、唯一升级、克隆跳过、
单位没有记录时清理召唤牌、治疗限制，以及回调内伤害、治疗和增加最大生命。

回写顺序由同一升级的嵌套移除/重加直接区分：添加过程中，失去半血的回调先
移除单位升级，外层效果随后仍将升级写入召唤牌；移除过程中，延迟回调在
临时牌面清理后重新添加升级，该新记录保留。若移除负生命升级的回血阶段
立即执行回调，回调中新加的临时记录则会被外层随后清理。永久牌面记录在
临时移除效果中保留，单位死亡前寿命不写召唤牌。

新版友军伤害路径会派发空 `OnBloodshed` / `OnPunish`，现在保留其载荷及顺序。
这不代表已经支持带这两类实际触发效果的单位；这些定义仍被拒绝，需后续校准。

每项比较 API 返回状态/完整队列及显式执行队列后的完整后继，包括单位、召唤牌、
统计缓存、所有调度载荷及游戏 RNG。七个返回边界保留共 16 份待执行载荷，
每项另执行 32 路不可变分支。采样恢复角色主状态、触发器、克隆标记、召唤牌
字段和修改器、伤害统计/牌组缓存及全部 RNG，并核对原始列车上下文。

```powershell
pwsh -NoProfile -File scripts/Check-Game231HalfHealthUpgradeEffects.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -HalfHealthUpgradeEffectsOnly -GameSpeed Instant -SkipBuild
```

二进制归档 `mechanics/half-health-deathwish/upgrade-effects/upgrade-effects.mt2f`
为 12,605 字节，哈希见同目录 manifest；来源
`.probe-runs/full-battle-steward-once-20261114-142231-4b814106`。原生约 27 秒，
差异零、上下文/RNG 恢复、退出码零、`DEPTH-PASS` 及玩家文件未变均通过。
旧版动态、濒死和数值升级三份归档共 151 个房间阶段、35 个列车阶段通过；
已有半血直接升级/最大生命及两版七回合基线、每版 16 路独立分支也通过。

这是卡牌效果组件验收，尚未开放带半血单位的完整出牌流程或整层战斗调度。
升级触发/技能/状态安装、累加缩放、死亡复活、菌群伤亡、预演和共享世界仍待验收。

## 可复验样本

[版本化样本目录](../tests/compatibility/game-2.3.1/) 保存八份二进制归档：
每版各一份卡牌目录、遗物目录、原生规则目录和完整基础战斗，合计约 550 KiB。
`manifest.tsv` 固定字节数及 SHA256；它们独立于旧版 2,874 份正式机制回归归档。

```powershell
pwsh -NoProfile -File scripts/Check-Game231Compatibility.ps1
```

该检查核对八份归档的哈希、版本/MVID、目录数量/身份、采集不改动帧及 RNG、
原生胜局与四项零计数，再运行两版独立整链复放。不会启动游戏，也不会跑旧版全套。

原生采样命令如下；Worker 3 是新版私有副本，Worker 1 仍是旧版基线：

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -Policy no-cards -CardCatalog -RelicCatalog -FullRuleCatalog -GameSpeed Instant -SkipBuild
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 1 -Policy no-cards -CardCatalog -RelicCatalog -FullRuleCatalog -GameSpeed Instant -SkipBuild
```

来源分别为 `.probe-runs/full-battle-no-cards-20261114-121845-0cc53e5a`
和 `.probe-runs/full-battle-no-cards-20261114-121846-62cd26b1`。
两次采样均由启动器确认玩家文件未变，且使用相同编译代码。
新版约 55 秒；独立复放无需原生进程。

## 后续适配清单

优先从共有定义的变更和新版公共机制开始，按原生效果类归组适配，避免逐卡重复实现。
每组仍需原生正向/对照观察、完整后继与 RNG、独立模型复放，再计入新版覆盖。

Rule 投影发生变化的共有卡牌：`DantesInferno`、`FlickerRaiseOnSlayAbility1/2/3`、
`FormlessChild`、`HornedWarrior`、`LunarEchoAbility`、`MagicPowerHand`、`PyreSteward`、
`PyreStewardAbility`、`Railgun`、`RuneboundGuardian`、`ScrivenerAbility`、`SilverInk`、`Swordmaiden`。

新增可收集遗物：`ChainedDamageIncrease`、`DamagedMaxHealthOnCelebrate`、
`DamageFriendlyOnSpawn`、`DeathwishAttack`、`DrawCardOnEarlyDiscard`、`DrawCardOnTurnDamage`、
`EmberForDamagedPerFloor`、`FriendlyUnitPunish`、`MonolithDevotion`、`RevengeTriggerTwice`、
`SpellsNoKillFriendlies`、`ZealDamageIncrease`。

变更的共有遗物：`ApplyDazedOnAscendOrDescend`、`CopyConsumedCard`、`DamageOnEaten`、
`FriendlyUnitsGainDeathwishPermanentHealth`、`FriendlyUnitsGainDeathwishValor`、
`GarfieldBoxRandomCardPerTurn`、`HephsBlueprints`、`MoltenManifold`、
`MoonPhaseRoomsGainMagicPower`、`MoreRares_Relic`、`NonBannerUnitsHaveDeployable`、
`OnDecayDeathApplyStatusEffects`、`ReduceStarterCost`。
