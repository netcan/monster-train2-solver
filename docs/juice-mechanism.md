# 共用状态倍率与按机制放行

固定游戏 2.3.1、build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。新增路径检查效果和回调结构，
不按卡牌 ID 或升级 ID 新增白名单；旧的复杂组合路径保留现有边界。

## 实现与适用范围

`CardTraitJuice`、`CardTraitJuice3`、`CardTraitJuice4` 共用倍率规则。
倍率取决于类型；原版三个 Mollis 魂魄升级的特性参数均为零，不能把参数
当作倍率。状态回调先对输入取非负，再计算额外层数；升级层数查询先做
unchecked 乘法再取非负，两者的溢出顺序不同。

当前通用卡牌路径接受：原始特性为空、非 X 费用、主效果全部为普通加状态、
一个完整捕获的倍率升级、可选 Freeze，以及已映射的 OnCast 加状态效果。
升级的其他属性、特性、回调和外部依赖逐项检查；参数升级 cast effects、额外有序缩放、
未知伴随效果仍拒绝。普通 OnCast 继续使用既有目标、状态和队列分派器，
不为 Mollis 单独重写护甲／中毒逻辑。

因此，具有相同完整机制结构的另一个卡牌身份可直接复用；不是任意法术、
任意升级组合或四倍特性的全部生命周期认证。尤其
`OnCardBeingUpgraded` 的来源判断、队列重复变化、深复制和过滤短路已由后续
[来源升级回调](juice-upgrade-mechanism.md)补齐独立组件和连续战斗证据。
本文的原始归档仍只验证状态施加与升级层数查询；其他消费者和混合回调继续分开审查。

## 已冻结证据

归档位于 `tests/compatibility/game-2.3.1/mechanics/juice`，仅二进制与哈希清单。

- 自动发现五个原版升级：商店双倍／三倍、Mollis 三个等级。一次进程生成
  12 个边界值 × 三种状态 × 五个定义，共 180 组原生回调对；覆盖负数、零、
  正数与整数溢出。完整状态、定义、RNG、帧和触发队列不变。
- 同一隔离原生进程执行两个原版状态法术和三个原版魂魄升级的九次实际
  施放，覆盖友军／敌军、主效果与 OnCast 的护甲／中毒，升级与法术定义不变。
  两张支援单位和一个能量控制另行构造，不算原版内容覆盖。
- 12 出牌／4 EndTurn、32 房间／8 列车阶段、122 份完整上下文，原生正常
  退出，四项失败计数均零；宿主 61.100 秒。实际结果为玩家战败：给敌人
  增加攻击也属于受测输入。保留原始败局，不修改胜负标记。
- 独立检查验证状态增量、完整连续整局和 16 个不可变分支、全部录制决策的
  完整候选、串并行根搜索与返回路径。替换卡牌与描述符身份后机制仍可用；
  改为自升级效果、未知特性／伴随效果时继续拒绝。

```powershell
# 日常机制矩阵，不启动游戏
pwsh -NoProfile -File scripts/Check-Game231JuiceMechanism.ps1 -ComponentOnly
# 本组阶段检查：矩阵 + 冻结整局、候选与根搜索
pwsh -NoProfile -File scripts/Check-Game231JuiceMechanism.ps1
```

原生重录使用隐藏、静音的隔离工作实例：

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -OriginalJuiceUpgrades -JuiceMechanismBatch -NativeWorker 3 -BinaryCapture -GameSpeed Instant
```

新增类无需复制一套卡牌检查器：继续扩展共用结构契约与批量对照，只有新源码
分支或实际差异才新增适配。日常检查本组与直接受影响的旧回归，阶段再跑全量。
