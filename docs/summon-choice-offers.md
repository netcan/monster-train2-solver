# 选择召唤的候选生成阶段

2026-11-15，2.3.1 / build `26650011`。`SummonChoiceModel.Plan` 现按原生
`CardEffectChooseMonsterToSpawnInBack` 生成候选，返回候选顺序、后继 RNG
和同层子类型拦截结果。此提交只完成候选阶段，原版技能的动作、选择界面、
出生和后续回调仍待接入，三个升级技能保持原有保护。

## 原生规则

- 先检查同层活着的友军是否已有指定子类型，存在则不打开界面、不选择随机数。
- 输入池已经经过原生 DLC／卡牌过滤；剔除没有单位定义的卡。
- 传给 `GetCanSpawnMonster(room, null, size)` 的位置为 null，实际分支只检查
  物理空槽，不检查容量点数。因此不以普通召唤出牌的容量限制剪枝。
- 展示数为 `max(1, paramInt)`。池大小不超过展示数时，保留原顺序，且不消耗 RNG。
  否则使用 Battle RNG 不放回抽取，保留抽取顺序。重复引用按原生 `List.Remove`
  删除最前面的匹配引用；不能按抽中下标删除。

原版 GrimFlight 的三个等级各有三个对应等级的候选，展示数均为三，
所以候选集合与顺序确定；玩家的三选一需要单独成为动作分支。

## 原生证据

在已有的原版 BlindingDarkshard 单位控制场景中，于首次动作前和每次动作后，
直接调用原生私有 `RoomAlreadyHasSubtype` 和 `CollectChoices`。原单位定义
不变；原控制场景仍通过可打出的控制副本召唤，不能据此宣称原版生成链路完成。

隔离平台的 DLC 安装检查会把原版 Darkshard 池过滤为空。校准保留
`native-platform` 空池，并添加 `installed-control` 平台输入条件。
后者只在当前校准线程的同步调用中覆盖平台的安装检查；所有原版卡牌、
池及效果定义不变。每批调用在 finally 中关闭覆盖并完整恢复 Battle RNG。
该覆盖和调用点均只编译进 `DEVELOPMENT_PROBE`，玩家 Advisor 不包含此类。

13 个实际决策状态、四个房间、六种池、两种平台条件、六种展示数
（-1/0/1/2/3/10）、三个 RNG 种子，共 11,232 次原生调用：

- 1,080 次同层子类型拦截，包含移动到另一层和最终死亡后解除的对照。
- 2,136 次随机子集，逐条匹配候选顺序和四个 RNG 状态字。
- 756 次按原顺序返回三候选，936 次剔除法术，1,872 次空池，936 次重复引用池选择。
- 52 次大小 1000 的单位空槽查询仍返回可召唤，验证该调用没有容量点数判断。

每个决策状态的决策快照、帧号、受检查的全部 RNG 流均不变；独立检查
逐条重算，并为每个状态运行 16 个不可变并行分支。组件快照和源战斗的
首次 Before／十二次 Actual 完全一致，不能用另一场战斗替代验收。

89.89 秒隐藏静音 Instant 原生胜局四项计数为零。原控制场景的 12 输入、
五次 EndTurn、完整连续复放／16 分支、16 根候选／128 状态串并行搜索和
三步计划复放通过，记录与搜索均零未支持。Probe、ModelChecks、Advisor
的 Release 构建通过。没有扩大到全量回归。

## 边界与后续

本次不增加原版卡牌或遗物的整场覆盖数。DLC／遗物卡牌过滤仍是输入池的边界；
物理空槽占满、预演跳过和其他特殊死亡状态仍需原生对照。随后须把玩家选择
纳入动作与搜索，并复用无卡单位出生和回调机制，验证传送完成后的真实状态。

归档：`tests/compatibility/game-2.3.1/mechanics/summon-choice-offers/`，
仅 `offers.mt2f`、`battle.mt2f` 和大小／SHA-256 清单，无 JSON 依赖。
源采样：`.probe-runs/full-battle-units-spells-and-junk-20261115-153946-73b48d36`。

```powershell
pwsh -NoProfile -File scripts/Check-Game231SummonChoice.ps1
```

重新采样：

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 `
    -OriginalUpgradeEnchantment -SummonChoiceCalibration -GameSpeed Instant -BinaryCapture
```
