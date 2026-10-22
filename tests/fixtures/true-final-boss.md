# Level9 原生出生边界

`true-final-boss-birth-calibration.mt2f` 是原生观察证据，尚不表示整场模型支持。
来源为 `full-battle-no-cards-20261022-183857-7869f3c7`，原生进程正常退出，
`DEPTH-PASS`，27.95 秒，受保护游戏文件未变。

隔离新存档调用原生 `MapScreen.Cheat_JumpToTFB`，明确记录解锁覆盖从 false 到
true、距离 0 到 8。原始场景、角色资源、起始牌组和火炉 80 血不改；这不是自然
解锁或自然推进。原生界面消耗 NonDeterministic，其他 15 条 RNG 流未变。
完成 `DoTrueFinalBossSpawning` 后连续两次同步读取状态一致，全部 RNG 流未变。

| 实体 | 攻击/生命 | 楼层索引 | 生命链接 | 原生角色类别 |
| --- | --- | --- | --- | --- |
| Dominion | 3 / 1555 | 3 | 独立 | 真终局，非外层 Boss |
| Savagery | 10 / 2750 | 0 | 独立 | 真终局，非外层 Boss |
| Entropy Bot | 15 / 3500 | 0 | 独立所有者 | 真终局、外层 Boss |
| Entropy Mid | 15 / 3500 | 1 | Bot | 真终局、外层 Boss |
| Entropy Top | 15 / 3500 | 2 | Bot | 真终局、外层 Boss |

五个实体都在上述楼层的原生物理成员列表中。三个 Entropy 分别注册到三层外部
锚点；HeroManager 的 `outerTrainBoss` 最终指向 Top。出生前队列为 `[0,2,1]`，
实际创建顺序仍为 Dominion、Savagery、Entropy Bot/Mid/Top，出生后队列清空。
不能据队列排列推断角色创建顺序，也不能把 Dominion/Savagery 当普通外层 Boss。

定向检查（不运行全量回归）：

```powershell
dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore -p:WarningLevel=0 -v:q
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --true-final-boss-birth-only tests/fixtures/true-final-boss-birth-calibration.mt2f
```

## 共享生命属性与原生预览深拷贝

`true-final-boss-linked-health-calibration.mt2f` 来自
`full-battle-no-cards-20261022-184603-5d1d6576`，原生进程正常退出、
`DEPTH-PASS`，26.92 秒，受保护游戏文件未变。出生后的五个原始实体分别临时绑定
新建、隔离的原生 `StateInformation`；只调用原生 `Hp` / `MaxHp` 属性和
`DeepCopyForPreview`，不调用伤害、治疗、死亡信号，不推进帧或恢复随机状态。
结束后原始状态对象引用全部恢复，五个实体/房间/队列状态、16 条游戏 RNG、
Unity 全局四字随机状态和帧号均未变。

70 个独立写入控制覆盖五个目标、两个属性和 `17/0/-3/99999/100000/Int32`
上下限；另有 10 个连续操作验证共享写入、两次原生深拷贝和副本独立写入。
`LinkedHealthModel` 与每个边界的完整原始存储、读取数值和更新标记匹配，
128 条并发分支匹配且输入不变。未知目标、未知属性、重复身份、悬空/自链接和
循环链接拒绝；多级转发尚未校准。

原生属性只有 99999 上限，负值不在此层归零；改最大生命也不会在此层补血或
截断当前生命。链接目标写入会标记目标和所有者，但保留目标自己的底层存储。
预览深拷贝读取共享生命数值，然后**去掉链接**，因此三个副本之后独立变更。
这些是状态属性边界，不能代替伤害回调、全局战斗预览或死亡行为的校准。

```powershell
./scripts/Check-TrueFinalBoss.ps1 -SkipBuild
```

## 原生 lifelink 防死阶段

`true-final-boss-life-link-calibration.mt2f` 来自
`full-battle-no-cards-20261022-185149-cfb4275a`，原生正常退出、`DEPTH-PASS`，
26.99 秒，受保护文件未变。五个原始实体的原生状态被临时替换为隔离状态：
调整三组生命、保留/移除 lifelink、设置预览生命和共享预览伤害；调用原生
`StatusEffectLifeLinkState.ProtectedByLifeLink` 与 `TestTrigger`，不推进伤害协程。
原始/预览状态引用、预览模式和累计伤害均恢复，实战状态、帧号、游戏与 Unity
RNG 未变；不通过重置 RNG 获得此结论。

1640 个上下文验证 3280 次保护查询与 9840 次伤害阶段输出，`LifeLinkModel`
全部匹配；512 条共享输入的并发分支保持不可变。覆盖三组不同生命、临界 1/2
生命、仅 Entropy 有状态、没有状态、四种预览上下文、死亡目标与整数边界。
死亡目标保留输出哨兵，防死伤害和防止的伤害量遵循原生整数运算。

五个实体的原始状态都有 lifelink，但三个 Entropy 因链接关系属于同一组，不能
彼此提供保护。其他独立组有超过 1 点生命时，致命伤害最多扣到 1 点；预测模式
使用预览生命，正在计算战斗预览时则用 Entropy 当前生命减共享预览伤害。
不能把物理实体数量或同一组的副本数量当作独立保护来源。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case LifeLink -SkipBuild`。

下一步为原生回合流、跨层伤害/治疗及终局移除回调。Level9 仍被玩家入口和
Level8 模型签名保护；上述组件不计作整场可求解或胜局。

## 防死与共享生命更新组合

`true-final-boss-health-updates-calibration.mt2f` 来自
`full-battle-no-cards-20261022-190544-c60a5ac8`，原生正常退出、`DEPTH-PASS`，
26.64 秒，受保护文件未变。五个原始角色临时绑定新建隔离状态，调用原生
`DebuffHP`、私有 `UpdateHp`，或先执行原生 lifelink 伤害阶段再扣血。
只运行非终结更新，原始状态引用、预览控制、终局标志、世界、帧和 RNG 均未变。

180 个矩阵控制和两条各 10 步的依赖链验证原始/有效共享生命、更新标记、
生命信号和共享预览伤害；`TrueFinalBossHealthModel` 全部匹配，256 条并发分支
不改输入。包含五个目标、两种战斗预览上下文、整数边界、不扣血、致命伤害
保留 1 血、三组都剩 1 血后恢复生命、跨层继续扣血以及生命属性的 99999 上限。

更新链接实体会同步三层生命，但 `hpChangedSignal` 只派发给被更新的角色，
不会广播给共享生命所有者或其他链接角色。只有 Entropy 更新会改变全局
`TrueFinalBossBattlePreviewHpChange`；它在非预览战斗时也累计，生命恢复会减小
此值。累计使用请求值，生命属性随后再截断到 99999，二者不能颠倒。

此边界中的 `UpdateHp` 是直接生命更新，**不是完整治疗协程**；没有运行
伤害过滤、受伤/治疗触发、死亡或全体移除。会导致死亡的操作仍显式拒绝。
定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case HealthUpdates -SkipBuild`。

## 原始整场诊断入口

`-OriginalTrueFinalBoss` 使用同一原生跳图入口，随后继续执行整场，不在出生组件
处退出。它与组件捕获互斥，要求全新隔离存档和距离 8；整场轨迹保留跳图控制。

```powershell
./scripts/Run-FullBattleProbe.ps1 -OriginalTrueFinalBoss -Policy no-cards -BattleDistance 8 -PhysicalSpawnPoints -BranchCardMasks -FullRuleCatalog -SkipBuild -TimeoutSeconds 240
```

`full-battle-no-cards-20261022-185533-c8428f7f` 原生正常退出，49.19 秒，原始
资源、牌组和火炉未改，150 次分支掩码捕获通过。六次空过的火炉生命依次为
72、61、44、3，最后归零（第一次结束回合只出生，仍为 80）。该局败北；采集失败、
模型差异和待完成项均为零，但 **unsupported 为 65**，集中在真终局出生、外层
控制器和 lifelink。此运行仅用于定位原生回合流，不登记为模型验收 fixture，
也不计整场覆盖、玩家入口可用或胜局。
