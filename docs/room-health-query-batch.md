# 定义驱动的失血攻击机制批量校准

2026-11-15，固定游戏 2.3.1 / build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。

首组生成器从原生 `AllGameData` 的单位卡与升级目录自动选取两种失血攻击
修饰器，不按单张卡牌编写场景或维护身份列表。当前发现五个原版升级：

| 原版升级 | 原生实现 | 系数／除数 |
| --- | --- | --- |
| Punished1Path1a | RoomStateFriendlyDamagePerOwnMissingHp | 1 / 1 |
| Punished1Path1b | RoomStateFriendlyDamagePerOwnMissingHp | 2 / 1 |
| Punished1Path1c | RoomStateFriendlyDamagePerOwnMissingHp | 3 / 1 |
| ZealotsHalo_Upgrade | RoomStateSelfDamagePerMissingHp | 1 / 1 |
| UnitUpgradeTitanSavageryAtkPerHP | RoomStateSelfDamagePerMissingHp | 2 / 1 |

原版定义只读。生成器另创建参数控制定义，将系数 -2／0／1／int.MaxValue
与除数 -1／0／1／2／3／int.MaxValue 组合；共 53 组原版或控制配置。

## 原生差分与成本

每组配置生成 720 次查询：十组生命值、三个阵营输入、三种站位、是否挂载
修饰器、是否请求属性、两个伤害类型输入。站位区分无点、点有房间和孤立点。
生命覆盖满血、受伤、零、负值、超过最大生命及整数溢出边界；使用原生 setter
的 99,999 上限，负值与极值是数值控制，不主张它们都是正常对局可达状态。

同一帧在专用未注册单位上调用原生 `GetDynamicInt`、`GetModifiedAttackDamage`
和 `GetModifiedMagicPowerDamage`，记录实际结果，不让模型预测过滤原生样本。
随后独立检查模型和全部 38,160 个结果，并做 16 次不可变并行求值。

原生查询矩阵实测 **2.214 秒**，独立比较及并行检查约 **2.353 秒**。
启动与完整基线胜局共 47.79 秒，包含上面的查询；2.214 秒不是整次运行耗时。
基线有一次出牌和七次 EndTurn，四项捕获计数为零、终局效果已结清，
原玩家文件不变；独立连续复放和 16 条分支通过。

生成器前后保留完整决策快照、原版定义快照、受检查 RNG、帧号和空触发队列，
独立验证完全一致。完成后直接销毁专用单位；不修改玩家或战斗中的单位。

## 源码结论与模型接入范围

- Self 类型要求单位自身挂载该修饰器，且有出生点和房间；忽略阵营与除数。
- FriendlyOwn 类型要求目标阵营恰为 Monsters，且有出生点和房间；无需目标
  自身挂载。先将除数归一到至少一，再整数除法，最后乘系数。
- 两种类型都只在属性查询时增加攻击，魔法伤害增量为零，伤害类型参数不参与计算。
- 减法／乘法保留原生整数溢出行为，生命不在查询函数中重新截断。

已有 Self 类型攻击计算已复用这份被原生校准的算术适配。
**FriendlyOwn 的房间来源传播、叠加、移动、死亡、预演、升级替换和冠军整局
仍保留保护；TitanSavagery 系数二的升级出生／生命周期也未由本批验收。**
本批是查询组件证据，不增加原版卡牌或遗物整场覆盖数，不声明整个类的所有
生命周期已完成。后续用同一参数计划驱动这些整合路径，避免重复手写参数用例。

## 使用与证据

```powershell
# 日常查询差分，不启动游戏
pwsh -NoProfile -File scripts/Check-Game231RoomHealthMechanismBatch.ps1 -SkipBuild
# 阶段验收另加来源基线的完整连续复放
pwsh -NoProfile -File scripts/Check-Game231RoomHealthMechanismBatch.ps1 -SkipBuild -WithBaselineReplay
# 重新采样：隐藏、静音、隔离实例
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 `
    -RoomHealthMechanismBatch -GameSpeed Instant -BinaryCapture
```

证据在 `tests/compatibility/game-2.3.1/mechanics/room-health-query-batch/`：
`batch.mt2f` 121,713 字节，`battle.mt2f` 31,301 字节及 TSV 大小／SHA-256 清单，
无 JSON 依赖。源采样：
`.probe-runs/full-battle-steward-once-20261115-165200-88ae7d45`。

参数计划位于 `src/NativeCasePlans/`，由 Probe 和 ModelChecks 共用；
正式 Model 及 Advisor 不包含生成器，原生批量驱动仅在 DEVELOPMENT_PROBE 编译。

Probe、ModelChecks、Advisor 三个 Release 构建通过，已核对生产程序集不含
生成器／原生批量驱动。直接受影响的原版 Halo 定向回归通过：155 次原生攻击
getter、九次装备操作、71 房间／13 列车阶段、25 次出牌／七次 EndTurn 的
整链／16 分支与 128 状态串并行搜索，记录和搜索零未支持。没有运行全部归档回归。

两类查询归档已通过带哈希的证据索引关联到机制清单，标记为 `native-query-fixture`，
同时列出检查器和剩余边界。该映射仅表示归档存在；不把查询证据提升为整类
生命周期支持，也不替代执行当前代码的回归检查。
