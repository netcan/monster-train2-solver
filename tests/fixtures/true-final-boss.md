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

## 整车生命状态接入

整场捕获现保留 `CombatContext.LifeLinks`，记录五个实体的有效生命、链接所有者
和角色类别；所有上下文复制路径保留它。这里是实战投影，预览生命规范为实战
生命、预览累计为零，避免鼠标悬停产生的 UI 副本和累计伤害混入搜索状态。

`LinkedBossHealthModel.Apply` 把已校准的生命更新应用到整车：重建共享上下文并
更新三个物理楼层的所有链接实体，保持位置和无关状态不变。100 个原生实战
操作（含连续链）匹配；另外 100 个正在计算 UI 预览的上下文被明确拒绝。
此接口尚未放行普通房间战斗或真终局控制器。

`true-final-boss-live-world-observation.mt2f` 来自
`full-battle-no-cards-20261022-191115-02aafd2d`。原生正常退出，49.66 秒，完整
空过败局的 11 个出生后回合边界中，共享状态与各物理楼层的生命/角色一致。
采集失败、差异、待完成均为零，unsupported 仍为 65，严格整场检查正确报
`DEPTH-FAIL`；这份文件仅验收共享状态捕获，不计整场模型通过。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case HealthUpdates,World -SkipBuild`。

## 原始实体实际伤害与治疗协程

`true-final-boss-damage-calibration.mt2f` 来自
`full-battle-no-cards-20261022-192108-974989fe`，原生正常退出、`DEPTH-PASS`，
28.80 秒，受保护文件未变。五个角色保持原始出生属性，不替换状态对象、不改
生命控制点；交替对中层/顶层 Entropy 执行 7 次原生伤害和 5 次原生治疗协程。
此处明确排除攻击者、来源卡牌、控制器调度与死亡；伤害类型为 Default。

从一个冻结整车根状态连续执行 12 步，以及逐步重放、64 条共享输入并发分支，
房间/整车/全局光环快照/卡牌与状态/战斗 RNG 全部匹配，父状态不变。覆盖
3500→3481→1、保护时 1 血不再下降、真实治疗 1→101→3500、零伤害/零治疗、
最大整数治疗的缺失生命截断及三层持续同步。修复了共享生命更新没有同步其他
楼层光环快照的问题。16 条原生随机流中仅 NonDeterministic 与 Chatter 有变化；
后者由原生伤害/治疗触发中的台词选择消耗，其余 14 条均未变。

`LinkedBossHealthModel.ApplyDamage/ApplyHeal` 现通过房间伤害/治疗引擎同步整车。
顶层保留原有外层 Boss 未支持标记，仅纯伤害/治疗入口允许在明确捕获的原始
Entropy 目标上执行；普通房间战斗、预览、带攻击者或来源卡牌的链接伤害，
以及终结更新仍拒绝。此组件不是整场 Level9 或玩家入口验收。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Damage -SkipBuild`。

## Savagery 一血时的选目标

`true-final-boss-pyrebane-targets-calibration.mt2f` 来自
`full-battle-no-cards-20261022-200703-401b635d`，26.52 秒，原生正常退出，原始
文件未变。对原生 Savagery 临时使用分离的生命/状态信息，并在每项后还原；
原始身体、房间、帧、16 条随机流及 Unity 随机状态保持不变。

480 组查询覆盖生命 1/2/2750、lifelink 和 pyrebane 的开关、战斗/放置阶段、
Room/FrontInRoom/BackInRoom/Weakest/RandomInRoom 五种模式和四个原生随机种子。
同时携带两个状态且生命恰为 1 时，战斗中各模式均排除 Savagery；放置阶段
只在前排和层内随机模式排除，整层/后排/最低生命查询保持原生结果。

`CardTargetModel` 的房间目标收集与搜索器随机分支候选池均接入该规则；480 组
目标/可选性/随机状态逐项匹配，128 条并发查询不改父状态。该检查没有开放
真终局攻击、死亡或整回合控制器。

```powershell
./scripts/Run-FullBattleProbe.ps1 -TrueFinalBossPyreBaneTargets -SkipBuild
./scripts/Check-TrueFinalBoss.ps1 -Case PyreBaneTargets -SkipBuild
```

## 真实法术来源与法术护盾

`true-final-boss-spell-damage-calibration.mt2f` 来自
`full-battle-no-cards-20261022-195550-c7f2f7ad`，28.69 秒，原生正常退出且原始
文件未改。选择起始牌组中真实存在、没有升级/额外特性的法术实例（编号 5），
将其作为 `ApplyDamageToTarget` 的 `playedCard` 来源；没有伪造卡牌或调整出生生命。
这是实际伤害 API 的校准，不计完整出牌流程/资源消耗验收。

14 次原生调用覆盖五个目标，完整房间/列车/统计/光环/共享生命/RNG 状态一致，
并从同一冻结根状态连续推进，64 条并发分支保持父状态不变。Dominion 前四次
19 点伤害逐层消耗法术护盾、生命不变；第五次才实际扣血，之后致命伤害由
生命链接保护到 1 血。其余身体也覆盖法术致命保护与实际恢复。

共享生命伤害接口只为已捕获、无升级/额外特性的普通法术开放来源编号；
缺失来源、单位攻击者和复杂卡牌签名继续拒绝。整回合、终态与玩家入口仍待接入。

```powershell
./scripts/Run-FullBattleProbe.ps1 -TrueFinalBossSpellDamage -SkipBuild
./scripts/Check-TrueFinalBoss.ps1 -Case SpellDamage -SkipBuild
```

## 五实体实际伤害与治疗

`true-final-boss-all-target-damage-calibration.mt2f` 来自
`full-battle-no-cards-20261022-195102-3e6bff64`，28.10 秒，原生正常退出，原始
资源/牌组/出生属性均未改。沿原先 12 次 Entropy 中/顶层调用继续，对 Dominion、
Savagery 及 Entropy 底部追加 12 次实际伤害/治疗，总计 14 次伤害、10 次治疗。

24 次协程从同一个冻结列车根状态逐步匹配完整房间/整车/光环世界/生命链接和
随机状态，64 条并发分支不改父状态。覆盖 Dominion 与火炉同层、Savagery 与
Entropy 底部同层、保护到 1 血、保护后的无损伤害及恢复至上限。原生 16 条随机
流逐行审计，只有声音/UI 使用的 Chatter 和 NonDeterministic 允许推进。

`pyrebound`/`pyrebane` 仅在带完整原生共享生命上下文的纯伤害/治疗调用中接纳；
Dominion 的回合后跨层腐化定义在该范围内保持不执行。普通战斗验证、选目标、
有来源卡牌/攻击者的伤害、终态死亡和完整回合仍保留 guard。

```powershell
./scripts/Run-FullBattleProbe.ps1 -TrueFinalBossDamageAllTargets -SkipBuild
./scripts/Check-TrueFinalBoss.ps1 -Case Damage,AllTargetDamage -SkipBuild
```

## 五实体完整出生状态转换

`true-final-boss-spawn-calibration.mt2f` 来自
`full-battle-no-cards-20261022-194609-d1087cb9`，26.80 秒，原生正常退出，原始
资源/牌组/火炉未变。通过明确披露的原生跳图入口，在 `DoTrueFinalBossSpawning`
前捕获五份原始定义和完整战斗状态，返回后再独立捕获实际结果。

`TrueFinalBossSpawningModel` 从冻结输入分配五个共享编号，按原生固定组顺序创建，
放置 Dominion/Savagery 与三个 Entropy 外部锚点，运行底部身体的原始单张天灾牌
生成，随后建立共享生命并清空出生队列。完整列车、牌堆、卡牌实例/注册表、
统计、光环世界、物理位置及可模拟随机流均与原生匹配，64 条并发分支不改父状态。
无关的 13 条原生随机流未变；Battle 用于生成卡牌，Chatter 和 NonDeterministic
允许原生声音/UI 调用。未来生成牌的规则在出生前捕获，避免牌已生成但规则缺失。

该组件仅接受原始五实体、全楼层启用、只有火炉的空场及没有出生效果的遗物环境；
Covenant、其他出生回调、特殊场面和真终局整回合控制器仍保留 guard。嵌套普通
房间世界也保留外层 Boss 攻击 guard，不因该组件通过而开放未校准的整场预测。

```powershell
./scripts/Run-FullBattleProbe.ps1 -TrueFinalBossSpawn -SkipBuild
./scripts/Check-TrueFinalBoss.ps1 -Case Spawn -SkipBuild
```

## 致命伤害与全体移除

五份 `true-final-boss-death-{dominion,savagery,entropybot,entropymid,entropytop}.mt2f`
分别来自独立的新原生进程。出生属性、资源和火炉保留原值；随后明确调用原生
`DebuffHP` 把三个独立组都调到 1 血，对所选实体施加 1 点实际伤害，再执行原生
`RemoveDeadCharacters`。这是一血控制场景，不计自然战斗或自然胜局。

五次都正常退出、`DEPTH-PASS`，受保护文件未变，用时 27.35–29.44 秒。伤害后
仍有五个活跃列表成员、没有移除调用，只有直接目标完成死亡信号；Entropy
链接组的三个生命同时为零，其他组仍为 1。清理阶段才调用一次
`RemoveTrueFinalBoss`，并对**全部五个真终局实体**执行 `RemoveCharacterInner`，
不是只移除死亡目标或三个 Entropy。五次均触发原生胜利，火炉仍为 80。

`TrueFinalBossRemovalModel` 与五次原生完整移除顺序、每步活跃列表和转发标志
一致，320 条并发计划不改父状态。楼层按 3/2/1/0/0 降序，同层 Savagery 在
Entropy Bot 前；最后一次死亡标记只给最后一个实体。当前仅校准原始五实体组，
其他数量拒绝。它不代替完整死亡回调、击杀镜头、出牌终结与整场控制器的模型。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Death -SkipBuild`。

## 首回合出生调度接入

`true-final-boss-scheduled-spawn-calibration.mt2f` 来自
`full-battle-no-cards-20261022-203222-86a8609e`，27.17 秒，原生正常退出，受保护文件未变。
原始五实体、牌组和火炉不改；完整出生调度边界及 64 条并发分支与原生匹配。

`EnemySpawnState` 现携带待出生定义/队列及 Hellforged 标志，出牌、技能、抽牌分支、
刷怪和回合状态转换均保留这些信息。首回合在普通首波处理后接入完整五实体出生，
清空队列，安装全局 Top 控制器、三项初始访问记录及零移动速度，并保留三个身体的
原生锚点数组。未来天灾牌的规则在出生前采集，避免首次抽到时缺少定义。

此组件只接受已校准的首回合原始空场。后续生命周期、攻击调度和终局死亡继续拒绝；
没有开放玩家入口，也不代表整场支持。

```powershell
./scripts/Run-FullBattleProbe.ps1 -TrueFinalBossScheduledSpawn -SkipBuild
./scripts/Check-TrueFinalBoss.ps1 -Case ScheduledSpawn -SkipBuild
```

## 原生控制器与回合前后动作阶段

`true-final-boss-controller-calibration.mt2f` 与 `true-final-boss-phase-calibration.mt2f`
来自 `full-battle-no-cards-20261022-201428-384921d9`，53.05 秒，原生正常退出。
原始资源、牌组及火炉保持不变。控制器覆盖 Dominion、Savagery 与全局 Entropy
Top 的 11 次实际调用，以及 1152 次隔离原生转换。Dominion/Top 普通动作组为空，
Savagery 有一个初始动作；不能把五实体控制器都当作空定义。

九个自然回合前后动作阶段（五次前置、四次后置）保持 Hellforged Casting。
完整战斗、普通敌军、牌堆、控制器和 RNG 与原生一致，64 条并发分支不改父状态。
期间 Savagery 在三层循环，Dominion 被火炉实际攻击，不能假设跨回合生命不变。

`true-final-boss-exhausted-phase-calibration.mt2f` 来自
`full-battle-no-cards-20261022-202001-f0c85a57`，34.27 秒，原生正常退出。
此场景明确在第二回合把原始九波刷怪队列推进到末尾，并将火炉生命/上限设为 1。
三个动作阶段（两次前置、一次后置）及 64 条并发分支完整匹配，验证刷怪队列
清空时仍保持 Casting，不进入普通 Level8 的无情期切换。

两个来源整局的采集失败、模型差异、待完成项为零，但 unsupported 分别为
65/23，原生均败北，封装器均 `DEPTH-FAIL`。以上仅关闭控制器与动作阶段组件，
受控一血场景不算自然战斗、自然胜局或整场支持。

```powershell
./scripts/Check-TrueFinalBoss.ps1 -Case Controllers,Phases,ExhaustedPhases -SkipBuild
```

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
