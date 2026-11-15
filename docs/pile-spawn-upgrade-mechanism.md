# 按牌堆数量授予出生升级

固定游戏 2.3.1 / build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。
`RelicEffectAddUnitUpgradePerPile` 接入现有出生队列与角色直接升级 API，
按效果参数复用，不依赖拥有者或卡牌 ID 白名单。

## 原生语义与模型

管理器保留 Covenant／非 Covenant、部署禁用和条件判断的原有位置。
效果先判断 `targetTeam.HasFlag(characterTeam)` 与子类型，再查询牌堆数量。
初始化把 None 目标阵营归一为双方。怪物管理器的空子类型排除薪火，
英雄管理器的空子类型允许全部；指定子类型沿用既有出生单位的子类型快照。

`CardPileHelper.GetVisibleCardCount` 对 Deck、DeckTop、DeckRandom 都返回永久
可见牌组数，不能替换为抽牌堆剩余张数。Hand、Discard、Exhausted 取对应
运行时计数；当前 `CardManager.GetCardCount` 的默认分支对 Standby、Eaten
和 None 返回零，即使那些实际容器可能有内容。

阈值大于零时，升级请求数是整数除法结果；非正阈值或负请求数均不执行循环。
数量在进入循环前取得一次。每次请求使用同一份升级状态、直接作用于角色，
不改写来源卡牌，也不先把升级属性乘数量。唯一升级、应用记录、状态、触发器、
能力和容量保护复用已有直接升级管线，保留空 upgradeId 和已有队列运行／延后
策略；结果及待执行回调传回出生队列。此效果本身没有遗物触发通知。

采集保存初始化后的牌堆、阈值、阵营、子类型和升级描述。
原生直接角色升级不检查升级的卡牌筛选；仅在此描述入口去除该无效筛选保护，
其他升级保护继续保留。缺失或未知的参数、未支持升级仍拒绝。
新的不可变描述随全部资源复制路径保留。

## 原生证据与验收

八类目录发现四个原版定义：`Echoes_GainStatsPerDeckSize` 与
`LilCapT1`／`LilCapT2`／`LilCapT3`。它们都是每十张可见牌组重复授予升级，
分别增加 2/2、2/4、4/8、6/12 攻血。

一次进程批处理包含四个原版配置和 48 个参数控制，共 **52 组配置、3,744 条
原生升级请求**，覆盖阈值零／负数／整数极值、团队掩码和子类型匹配／不匹配。
这部分仅同步控制计数输入并枚举原生父协程，不执行其升级子协程，不能把请求
数量对照当作 3,744 次完整升级应用。另以未替换的原生函数取得九类真实牌堆
查询。完整决策状态、定义、所检查 RNG、帧与空队列保持一致；原生矩阵约
**0.027 秒**，离线比较及 16 路并行求值通过。

整局使用不改定义的原版牌组数量变异器。三张明确编写的 80 攻／5,000 血
单位控制分别在三层出生，不计作原版卡牌覆盖；每次出生逐项验证独立升级记录
及最终攻血。原生获胜，三次出牌、五次 EndTurn，四项失败计数全零，启动与
连续战斗 43.08 秒。模型匹配九个牌循环、42 个房间阶段、九个列车阶段、
132 份完整上下文、16 条连续分支和全部录制候选前沿，根搜索串并行及返回
计划复放一致。部署、Covenant 分派、已触发条件、零阈值和替换身份五个模型
控制通过；它们没有扩大上述原生整局范围。

两份二进制共 51,354 字节，使用 `.mt2f` 与哈希清单，无 JSON 依赖：
`tests/compatibility/game-2.3.1/mechanics/pile-spawn-upgrade/manifest.tsv`。

```powershell
pwsh -NoProfile -File scripts/Check-Game231PileSpawnUpgrade.ps1 -SkipBuild
# 日常参数／查询对照，不启动游戏
pwsh -NoProfile -File scripts/Check-Game231PileSpawnUpgrade.ps1 -SkipBuild -ComponentOnly
# 需要更新原生证据时，隐藏、静音的隔离实例
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -PileSpawnUpgradeMechanism -NativeWorker 3 -BinaryCapture -GameSpeed Instant
```

定向回归通过现有百分比出生组件与完整整局、直接角色升级归档的 21 个原生
转换，以及资源和升级触发纯检查。未重跑八项阶段全集或全部历史归档。
三个灵魂的取得／携带流程、复杂升级回调组合及其他拥有者组合未因此全部认证。

同次源码核对确认，`RelicEffectScaleStatusApplicationsOnSpawn` 还会按源定义
追加初始状态，并改写运行时触发器参数；其实际出生与显示预演的 requiredMutator
条件相反，四个来源还带其他效果。该机制继续待实现，不以本组出生升级替代。
