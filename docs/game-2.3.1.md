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
- 原生死亡愿望来源集合由 `deathWishSources` 改名为 `deathwishSources`；
  采集兼容两种字段，未知结构显式失败。
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
