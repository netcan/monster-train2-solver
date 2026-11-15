# 原版受罚单位与技能

游戏 2.3.1、build 26650011，Assembly-CSharp MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。场景准备时先保存六个原版定义，
保留费用、基础属性、出生状态、效果和冷却：

| 原版卡/技能 | 费用 | 行为 |
| --- | --- | --- |
| GrimDisciple | 1 | 60 攻/25 生命/2 体型，横扫及禁攻击，受击减少一层冷却 |
| GrimDiscipleAbility | 0 | 治疗 9999、移除禁攻击，将自身替换为 AngelFlightAbility；原冷却及出生冷却均为 4 |
| AngelFlightAbility | 0 | 选择目标层与物理站位传送，激活后冷却 2、出生冷却 0 |
| ChainBearer | 1 | 20 攻/30 生命/2 体型，攻击时给本次目标两层锁链 |
| ChainBearerAbility | 0 | 给同层全部敌人两层锁链，激活后冷却 3、出生冷却 0 |
| ForsakenBerserker | 2 | 20 攻/20 生命/2 体型，出生时每两级虔诚获得一次多重攻击 |

模型复用既有伤害、治疗、状态、技能缓存、可用性回调及传送机制，本批补齐
两个共用缺口。`CardEffectAddStatusEffectPerDevotionLevel` 现可用于单位
OnSpawn 的固定单项自身状态，按当前等级除以原版除数；采样器同时修正
继承类被错误描述为普通状态倍率的问题。其他触发时机、随机状态池及范围
继续受保护，不由本批扩展。

原版 `CharacterState.SetUnitAbility` 在非装备技能替换且正在激活时设置
`unitAbilityReplacedWhileResolving`。`CardEffectResetCooldown` 的激活后
重置会消耗它，并跳过这一次冷却重置；出生冷却模式不消耗，Horde 出生门槛
先执行。OnOwnAbilityActivated 仍正常派发。不可变模型、状态复制和搜索
键保留该标志；原生采样直接读取字段，不调用会消耗它的原生 getter。
2.2.1 没有该原生标志和消费方法。采样器按方法是否存在捕获
`ReplacementSkipsCooldown` 能力，模型仅在该能力为真时设置替换标志。
两项为 false 时不增加二进制归档字段，旧归档继续沿用旧版规则。

控制场景只修改锚点及守卫、手牌上限，并另建能量、容量、受伤、冷却清理及
虔诚控制卡。六个受验原版定义不改；保留卡使用临时冻结。首回合 Grim
保持禁攻击，ChainBearer 经自然攻击在守卫上再加两层锁链，随后锁链按
原生回合流程减一。三个独立 5 点伤害将 Grim 从 25 生命降到 10，同时
清除其剩余三层冷却；原版技能再将其治疗至满并替换为 Flight。

第一技能结算后 Flight 不带冷却。随后两次跨层传送检查物理站位和
2→1→0→2 冷却链，零伤害不触发减冷却。ChainBearer 原版技能施放两次，
每次对同层至少两个活敌人加二层锁链且不造成直接伤害。五次 Berserker
出生分别位于虔诚 0/1/2/3/4 级，多重攻击为 0/0/1/1/2；后来提高虔诚
不追溯修改已出生的单位。

独立检查比较所有记录后继、完整房间/列车、RNG、整场连续复放及 16 路
不可变分支，要求全部记录决策与串行/四线程 128 状态搜索严格零未支持，
并复放返回的计划。五次实际 OnOwnAbilityActivated 冷却效果另逐项验证
完整前后状态和 32 路分支：只有第一次技能替换的标志为 true，结束时
被清除，其余四次激活照常重置冷却。

正式采样隐藏、静音、Instant，耗时 92.79 秒；26 输入/7 EndTurn、
61 房间/13 列车阶段原生胜局，采集失败/差异/未支持/待结算四项为零。
五次直接冷却效果各 32 分支、236 完整上下文、整场复放/16 分支、
26 个记录决策、47 根候选、128/128 串并行状态及三步计划复放通过，
记录和搜索严格零未支持。既有 2.2.1 技能生命周期、冷却和效果三份完整
定向回归通过；旧虔诚出生、六个受罚单位和上一批装备整链定向检查通过。
ModelChecks、Probe、Advisor 三个 Release 构建通过。

正式二进制归档为
`tests/compatibility/game-2.3.1/mechanics/punished-skills/battle.mt2f`，
63,407 字节，SHA-256
`746929cfaabad9bf0d89347130ebf3b07ebcd23a299a6c728856fe37e473a52d`，
不依赖 JSON。来源为隔离采样
`.probe-runs/full-battle-units-spells-and-junk-20261115-145906-b017fe9a`。

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -OriginalPunishedSkills -GameSpeed Instant -BinaryCapture -SkipBuild
pwsh -NoProfile -File scripts/Check-Game231PunishedSkills.ps1
```

复杂触发竞争、装备与结算中替换的交互、克隆/复活和完整悬停组合仍需
独立证据。新版 614 张卡/236 件遗物的整体复验和完整模拟尚未完成；
新增遗物原生整局验收仍为 12/12。
