# Punished 冠军升级树与 Darkshard 定义审计

2026-11-15，游戏 2.3.1 / Steam build `26650011`，程序集 MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。

原生 `AllGameData` 的卡牌目录不足以判断冠军升级后的支持程度。本次沿原版
`ClassData.FindUpgradeTreeForChampion` 读取两名 Punished 冠军的三条路径，
保留每条路径的原生顺序、三个等级的身份、数值、替换列表、触发器、
能力升级及房间修饰器。该采样不修改原版卡牌或升级定义。

## 已验收的审计结果

- 724 个唯一原版卡定义，614 张图鉴卡的投影无错误或显式保护。
- 两名原版冠军、六条路径、18 项升级；所有升级身份、等级、攻击／生命和技能链接匹配。
- `Punished1Path1a/b/c` 的房间修饰器是 `RoomStateFriendlyDamagePerOwnMissingHp`，
  参数分别为 1/2/3，除数均为 1，仍明确保留 `Upgrade room modifiers` 保护。
- `Punished1Path3a/b/c` 分别提供三个原版 GrimFlight 技能。每个技能先传送，再执行
  `CardEffectChooseMonsterToSpawnInBack`，其池为对应等级的 Blinding／Looming／Shattering，
  按此顺序提供三个选项。同层限制为 `SubtypesData_Darkshard`，不是目标子类型过滤。
- 九张 Darkshard 原版卡均为 `NonPlayable`，未归档，单位生命 10、容量 1，属于该子类型。
  基础卡的定义投影通过，不代表它们的原版生成链路已经支持。

目录检查前后决策、全部受检查的 RNG 状态和帧号不变。原生隐藏静音 Instant
采样耗时 46.30 秒，随后基础战斗胜利，四项采样计数均为零。独立检查匹配
52 个房间阶段、14 个列车阶段和七次 EndTurn，并从初始状态连续复放全场，
通过 16 个不可变分支。基础战斗没有执行这些冠军升级或选择召唤。

## 仍需实现和原生校准的机制

候选生成阶段现另完成[原生校准](summon-choice-offers.md)。完整的动作／搜索／出生尚未接入，
三项冠军房间修饰器升级，以及三个技能里的选择召唤效果，仍有显式保护。
不把其余升级的“无投影保护”计为完整运行时验收，也不增加原版卡牌或遗物的整场覆盖数。

原生 `CardEffectChooseMonsterToSpawnInBack` 的顺序是：前一个传送效果完成后，
检查目标房间的同类单位；过滤无单位定义或无物理空槽的选项；提供选项；
玩家选定后再次检查物理空槽；以无卡单位在后排出生，然后结算站位变化。
后续直接代码校准确认，其空出生位置参数走不检查容量点数的分支，不能套用普通出牌的容量限制。
原版三个候选的池大小等于展示数，因此不消耗选择 RNG。通用效果在展示数小于
可用池时会使用 Battle RNG 不放回抽样，这和玩家选择是两个不同的分支来源。

后续需要把选择结果纳入动作、搜索枚举和原生 UI 执行，并校准传送后的容量、
同层限制、出生、效果、离场及连续多回合状态；不能用可打出的副本替代原版生成入口。

## 复验

```powershell
pwsh -NoProfile -File scripts/Check-Game231PunishedChampionDefinitions.ps1
```

两个无 JSON 依赖的二进制归档和大小／SHA-256 清单在
`tests/compatibility/game-2.3.1/projection-audits/punished-champions/`。
源采样为 `.probe-runs/full-battle-no-cards-20261115-152021-d1f34388`；
采样输出与检查日志分别为 `results/punished-champion-catalog-capture.log`、
`results/punished-champion-definition-check.log`，均不进入 Git。
