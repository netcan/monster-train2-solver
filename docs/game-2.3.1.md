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

本次开放直接非预演伤害/治疗 API，最大生命由下述独立样本验收。Undying 致命伤害另见下述 20 项组件；原生伤害阶段最终死亡及移除另见下述 18+2 项。触发安装/移除、
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
这份升级归档只含空调度；带实际触发效果的单位另由下述 33 项伤害组件校准。

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

## Bloodshed / Punish 的实际伤害回调

33 项原生组件对照已通过，包含 34 次直接伤害调用、136 份完整调度载荷。
场景通过原生扩容接口和真实付费出牌，在同层放置两只列车员，再分别安装触发器。
每项重置全部参与角色、触发器、召唤牌字段和统计缓存，并比较完整房间/上下文后继。

- 玩家单位承受伤害或吸收伤害时，先向同层所有玩家单位排入 Bloodshed，再执行
  受击 OnHit；友方来源还会对受伤单位派发 Punish。敌方目标没有 Bloodshed，
  仍可因友方来源触发 Punish。伤害量、类型和 Bloodshed 的受伤目标引用均保留。
- 来源判定优先看攻击者：有敌方攻击者时，附带卡牌或遗物仍算敌方来源；
  没有攻击者时，卡牌和遗物才各自构成友方来源。自伤也算友方来源。
- 护甲、伤害护盾、Titanskin、零伤害及按实际伤害值判定的阈值均有对照；
  零伤害吸收仍会派发回调，但正数阈值可以阻止其效果。
- 单次触发跨两次伤害、沉默/mute/忽略沉默、Bloodshed 指向受伤目标的治疗/伤害、
  Punish 自身治疗/伤害，以及这些回调先于半血判定的顺序均匹配。

每项独立模型复放及 32 路不可变并发分支通过，游戏与 Unity RNG 未变；
采样结束恢复全部 RNG 和原始列车上下文。采样期间持有独立的预演抑制计数，
避免后台预演混入伤害记录，结束时释放自己持有的计数。
缺失新版伤害统计快照、预演和原生卡牌伤害阶段之外的最终死亡仍受保护。
这批组件不计作新卡牌、新遗物或完整战斗的整体验收。

```powershell
pwsh -NoProfile -File scripts/Check-Game231DamageReactions.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -DamageReactionsOnly -GameSpeed Instant -SkipBuild
```

归档 `mechanics/damage-reactions/damage-reactions.mt2f` 为 14,448 字节，SHA256
`636b627f9d0784a97ce502fda01d71ed98c1756c9b4e9a8711d9a296c8a292e7`。
来源 `.probe-runs/full-battle-steward-once-20261114-144339-1736dab5`，原生约 35 秒，
差异/待执行队列零、`DEPTH-PASS`、退出码零及玩家文件未变均通过。
已有半血伤害/治疗和 28 项升级效果，以及两版七回合基线各 16 路分支均回归通过。
新版伤害统计和列车员七回合链/16 路分支也通过；旧受击/击杀归档完整通过，
旧攻击触发 57 个房间/14 个列车阶段、濒死升级 56/14 阶段通过。
旧攻击触发归档的 EndTurn 第 0 项曾有 `LastAttackerId: 0 != 2` 差异，
修改前 `a7720df6` 的独立模型副本亦复现。原生 2.2.1 保留自伤者引用，
2.3.1 则显式排除自身；现在按新版伤害统计快照区分这条规则。
修复后旧攻击触发、受击/击杀、濒死升级三份归档的完整回归通过：163 个房间、
39 个列车阶段、60 次出牌和 20 次 EndTurn，各 16 路独立策略分支。
旧攻击触发另核对 47 次单位行动和 54 份攻击调度；新版 33 项仍全部通过。
零/正伤害、无来源/自身/其他单位/薪火四种来源的版本控制及 32 路分支可单独运行：

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --attack-trigger-pure
```

## Undying 致命伤害及半血回调

20 项原生 Undying 组件已通过，包含 21 次直接伤害调用和 332 份完整调度载荷，
每项均比较全部单位、召唤牌、统计缓存、站位、回调参数与游戏/Unity RNG。
独立复放和每项 32 路不可变并发分支通过。

原生致命伤害先在零生命时更新半血来源并入队，随后 Undying 将角色恢复为一血，
减少一层状态，排入 Harvest、OnReanimated 和 `OnDeath(paramInt=1)`，
再更新半血来源并派发死亡信号。模型保留这段延迟，回调执行时读取复活后的角色。
OnDeath 的显式参数对象带空字符串载荷，与没有参数对象时的 null 区分。

矩阵覆盖卡牌/敌方/自伤/遗物/其他友军来源、双方目标、两次连续复活及一次性
死亡统计监听器、OnReanimated/OnDeath 治疗、半血 gain 中治疗、最大生命为 1
时先 gain 后 lost，以及原本已处于半血的两项控制。Bloodshed/Punish 在零生命时
仍可执行金币效果；针对已死目标的伤害/治疗会因目标不合法而跳过。
两次复活的角色始终保留真实召唤牌和物理站位，玩家的一次性统计监听器只计一次。

```powershell
pwsh -NoProfile -File scripts/Check-Game231DamageReactionRevival.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -DamageReactionRevivalOnly -GameSpeed Instant -SkipBuild
```

归档 `mechanics/damage-reactions/revival/revival.mt2f` 为 13,168 字节，SHA256
`d0a0012f4059af99f42961a8999b81088657015ca4cd6f9968f59acbe4a04ee8`。
来源 `.probe-runs/full-battle-steward-once-20261114-150344-e4d513b9`，原生约 34 秒，
差异/待执行队列零、全部 RNG 与原始列车上下文恢复、`DEPTH-PASS`、退出码零和
玩家文件未变均通过。原有 33 项伤害、13 项半血伤害/治疗、28 项升级效果通过；
旧版 11 个复活边界/7 个直接操作/90 个回调阶段，以及附魔来源的 4 个自然复活
边界/24 个阶段，各 16 路分支通过，相关生命周期纯检查也通过。

旧历史反伤/复活归档的 78 输入复放仍被缺失 `MoonPhaseMask` 保护拦住，错误为
`Movement moon understanding requires a captured prior mask.`；本次与修改前
`a7720df6` 的独立模型副本复现一致，未计为通过，需补充原生位移证据。
无 Undying 的最终死亡见下述新证据；预演、共享世界半血调度及完整出牌/整层战斗仍待校准。

## 最终死亡与实际移除

18 项无 Undying 的原生伤害返回边界及两次实际玩家/敌方移除通过。
前者包含 77 份完整调度载荷，后者另有 22 份；每项独立复放及 32 路并发分支
比较完整房间/上下文、回调、游戏和 Unity RNG、父状态不可变性。

矩阵覆盖六种伤害来源、双方目标、针对死者的 Bloodshed/Punish 嵌套伤害/治疗、
半血 gain、最大生命为一、已有半血来源及半血/死亡治疗控制。
原生在零生命时排入半血请求，并在死亡信号前派发；死者的半血效果不执行。
伤害返回时已结算死亡统计、移除统计监听器，角色仍保有物理站位，
玩家召唤牌仍在 Standby。模型输出一项延迟物理移除，保存完整死者状态。

两次实际移除从同一原生上下文顺序执行。OnDeath、对应团队的 Harvest 与
OnAnyUnitDeathOnFloor 的顺序一致，玩家/敌方金币分别增加 190/180，
死者治疗未生效，玩家召唤牌转入 Exhausted，实际站位被清除。

采样使用原生效果队列持有整个可重置矩阵，防止后台主循环提前移除死者。
原生协程栈自行处理 `BreakZeroFrame`，不能将它压平后转交给原生控制器。
18 项结束后先验证原始列车和全部 RNG 已恢复，再进行两次不可恢复的实际移除；
归档的 `RestorationBoundary=BeforePhysicalRemovalSequence` 明确记录该检查点。
实际移除只发生在隔离、静音的 worker3，玩家文件未变。

```powershell
pwsh -NoProfile -File scripts/Check-Game231DamageReactionFinalDeath.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -DamageReactionFinalDeathOnly -GameSpeed Instant -SkipBuild
```

二进制 `mechanics/damage-reactions/final-death/final-death.mt2f` 为 12,833 字节，SHA256
`5e148ad737d74f420be2e7b2ca83fc7137c29739ae30bf4fbad89d8024fc32a0`。
来源 `.probe-runs/full-battle-steward-once-20261114-152858-a060d445`，原生 36.82 秒，
差异/待执行队列为零、退出码零、`DEPTH-PASS` 及玩家文件签名未变均通过。
已有 33 项伤害、20 项 Undying、13 项半血伤害/治疗、28 项升级效果及旧版
直接/附魔来源复活与生命周期纯检查均通过。
旧版攻击触发、受击/击杀和濒死升级三份完整回归也通过：163 个房间、39 个列车
阶段、60 次出牌、20 次 EndTurn，各 16 路独立策略分支与完整战斗链一致。

上述死亡组件本身只验收非预演的原生卡牌伤害阶段及其已记录移除流程。
本节采样时其他伤害阶段、共享世界、外部半血伤害回调队列、完整出牌和整层战斗仍待校准；
下节补充半血机制的整牌、自然回合与预演验收。
没有增加整张新版卡牌或遗物的完整覆盖数。

## 半血 Deathwish 的整牌、自然回合与预演

半血条件现可贯穿实际出牌、出牌后的战斗预演和自然 EndTurn，使用统一回调队列。
原生完整战斗获胜，25 次出牌、7 次 EndTurn、62 个房间阶段、14 个列车阶段匹配。
336 份完成的半血回调中，68 份属于真实结算、268 份属于预演，均保有独占触发源 ID；
预演不增加金币。独立模型从第一步连续推进到终局，比较每次输入前后完整状态与 RNG，
16 路分支均一致且父状态未变；没有在步骤之间载入下一份原生状态修正模型。

校准场景只在隔离 worker3 修改定义：列车员设为 12 生命、保留原始费用，附加一层 Undying、
进入半血时奖励金币并自伤 2、退出时奖励金币并自疗 1、回合开始时自疗 8 后自伤 6。
先支付费用召唤两名列车员，再实际打出三张友方自伤 7/治疗 8 的校准法术，后续沿正常策略
打出原牌堆卡牌。证据包含预演和真实结算中的嵌套伤害/治疗、来源重复请求、自然 Undying
复活及最终移除；并未将这些改写定义计入原版卡牌或遗物覆盖数。

一次诊断确认，若将进入时自伤改为 1，同批退出/进入回调会在生命 6/7 间形成原生无限循环。
最终场景用自伤 2 保留嵌套调度而可结束。任意循环候选的通用预算/取消机制仍需独立完善，
本次不据此宣称所有组合都能终止。

共享附魔世界中的半血条件、直接升级 API 的预演、外部运行队列中的直接升级、
致命最大生命/升级变化、升级安装触发/技能/状态等仍保持各自边界。
实际 Bloodshed/Punish 效果的预演也仍受保护；本场景观察到的是原生伤害请求及半血效果。

```powershell
pwsh -NoProfile -File scripts/Check-Game231HalfHealthBattle.ps1
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -HalfHealthBattle -GameSpeed Instant -SkipBuild
```

二进制 `mechanics/half-health-deathwish/battle/battle.mt2f` 为 57,216 字节，SHA256
`d96997d5e6a3ee84372e622ec71cd5542c7c99cda89aab46fd125407be5d5dd7`。
来源 `.probe-runs/full-battle-units-spells-and-junk-20261114-155345-7872c673`，原生 82.57 秒，
四类失败计数均零、退出码零、`DEPTH-PASS` 和玩家文件签名检查通过，使用隔离静音采样。
五组既有半血组件、三组 Bloodshed/Punish 伤害/死亡组件及旧版三份受影响定向策略回归通过。
旧归档仍按原有快照新鲜度规则检查房间、列车、出牌和 16 路策略分支。
额外严格整链检查在旧 `full-battle-hit-kill.mt2f` 发现房间 Enabled 的 False/True 差异；
修改前 `fe26bbde` 的独立模型副本也复现，不能计作该归档的严格全字段整链通过。
新版场景保留全部字段比较，没有为此放宽检查。

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
