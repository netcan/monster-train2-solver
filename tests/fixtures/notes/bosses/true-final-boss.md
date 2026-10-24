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
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --true-final-boss-birth-only tests/fixtures/bosses/true-final/calibration/true-final-boss-birth-calibration.mt2f
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

## 持续战条件与双方回合前阶段

`true-final-boss-continuation-calibration.mt2f` 与 `true-final-boss-passive-calibration.mt2f`
来自 `full-battle-no-cards-20261022-204301-671d4b18`，56.78 秒，原生正常退出，
受保护文件未变；原始五实体、牌组和火炉保持原值。

前者在原生 Dominion/火炉物理房间，用隔离状态存储控制双方生命、lifelink、
untouchable 和持续战开关，直接调用原生续战判定，共 288 次。结束后原始对象、
帧号、16 条游戏 RNG 与 Unity RNG 不变；128 条并发查询匹配。生命链接单位
仍参加首轮，但不计入继续持续战所需的双方存活单位；普通敌军仍可维持持续战。
该证据仅校准判定，不是实际多轮攻击或完整战斗的验收。

后者保留完整来源轨迹，定向验收 12 个双方回合前阶段：部署时两次，五实体
出生后十次。原生顺序、五实体、后续普通波次、完整列车/上下文/RNG 与模型一致，
64 条并发分支匹配且不修改输入。仅在回合前与弃手牌前阶段接纳已捕获的生命链接
状态，Dominion 回合后效果保持独立保护；攻击与预览继续拒绝。

来源整局原生败北，采集失败/模型差异/待完成项为零，unsupported 为 49，
封装器 `DEPTH-FAIL`。没有把整局作为完整模型验收，也未开放玩家真终局入口。

```powershell
./scripts/Check-TrueFinalBoss.ps1 -Case Continuation,Passive -SkipBuild
```

## 原生单位攻击回合

`true-final-boss-unit-turn-calibration.mt2f` 来自
`full-battle-no-cards-20261022-204947-fc7fd2f1`，57.59 秒，原生正常退出，受保护文件未变。
原始五实体、牌组和火炉保持原值。29 个实际单位回合包括部署期的空火炉回合，以及
出生后的 28 个回合；五个真终局实体均实际进入该原生调度边界。

六次火炉攻击、六次 Dominion 攻击、六次生命链接写入的完整房间、共享上下文、
远端生命快照、RNG 和实际目标序列均匹配；64 条并发分支保持输入不变。
只在单个单位攻击边界接纳已捕获的生命链接状态，回合后效果保持独立保护。
一血 Savagery 的攻击选目标/践踏还需实际攻击校准，仍拒绝该场面。

来源整场原生败北，采集失败/模型差异/待完成项为零，unsupported 为 49，
封装器 `DEPTH-FAIL`。单位攻击组件通过不代表完整房间、预览、任意攻击者来源
伤害 API 或玩家真终局入口已开放。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case UnitTurns -SkipBuild`。

## 没有友军单位时的回合后效果

`true-final-boss-post-combat-calibration.mt2f` 来自
`full-battle-no-cards-20261022-205650-20ee072e`，57.89 秒，原生正常退出，受保护文件未变。
原始五实体、牌组和火炉保持原值。18 个原生阶段包含出生后的 17 个阶段，
完整房间、上下文、卡牌实例/牌堆和 RNG 与模型一致，64 条并发分支不改输入。

该来源全程没有友军单位，只有火炉。实际生成 12 张天灾牌，Savagery 累计增加
八层强化并造成 20 点远端火炉伤害。回合后入口从捕获的整个列车取得外部目标，
Dominion 的跨层前排查询正确排除火炉并得到空目标。不能据此推断有友军单位时
的腐化状态写入与回调已校准；该场面仍拒绝，预览也继续拒绝。

来源整场原生败北，采集失败/模型差异/待完成项为零，unsupported 为 49，
封装器 `DEPTH-FAIL`。该组件不表示完整房间战斗或玩家入口已接入。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case PostCombat -SkipBuild`。

## 没有友军单位时的完整列车战斗与升层

复用 `true-final-boss-post-combat-calibration.mt2f` 中已有的 10 个原生列车边界，
不重复启动游戏生成相同样本。五次完整战斗、五次升层包含部署、五实体与后续
普通波次；单位、移动规则、物理占位、共享上下文、牌堆和 RNG 均匹配，64 条并发
分支不修改输入。房间内重复的采集时上下文统一以列车共享上下文比较；沿用既有
升层边界规则，排除原生协程返回前下一次抽牌刷新的 StoredCards 统计。

原生 `IsCombatInRoomDisabledForFinalBoss` 在火炉层只剩生命链接敌军时跳过
升层追加战斗；常规整列车战斗仍执行。循环单位预留槽位后，升层会收紧敌方物理
站位；不能把稍后空出的前排槽位一直留空。这两处已按原生修正。

完整战斗只接纳原始施法期、非预览、五实体共享生命、完整原始锚点和没有友军
单位的已校准场面。显式验证预览和新增友军单位仍拒绝；一血 Savagery、无情期、
终局死亡调度和完整 EndTurn 尚未关闭。普通战斗另以 9 个原生列车边界定向回归，
无差异或跳过项。该证据不表示玩家真终局入口可用。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Train -SkipBuild`。

随后原生整局复验 `full-battle-no-cards-20261022-211145-074e0548` 正常退出且受保护
文件未变，采集失败/待完成项为零；10 个列车边界均匹配。整局未支持项由 49 降到
五个预览边界，同时暴露最后一次空过的终止手牌清理差异。终止 EndTurn 已增加
显式保护，不能把该次运行计为整局通过或自然胜局。

## 原生默认 UI 预览与五回合连续推演

`true-final-boss-preview-calibration.mt2f` 来自
`full-battle-no-cards-20261022-213500-1284c5bc`。五次默认预览保留原生开启设置、
原始五实体、牌组和火炉；只读观察，不改预览开关或时序。预览返回并完成主状态
房间顺序刷新后捕获结果，不能延迟到下一次结束回合切成 HeroTurn 后再比较。

16 个实际执行的楼层战斗和最后一个被取消的火炉房间均有前后快照。未使用的
灾厄负面效果先于房间战斗运行；第五次预览中灾厄已杀死火炉，挂起房间保持不变，
后续楼层不执行。预览只修改独立生命副本，结束后恢复主状态；仅有生成牌效果的
触发器在预览中测试不通过，不能错误设置 HasTriggered。

五次预览的完整列车、共享上下文、牌堆与 RNG 匹配，64 条并发分支不改输入。
部署根状态的连续五次 EndTurn 逐步匹配原生，没有注入后续原生 Before 快照。
最新整局原生正常退出、败北且受保护文件未变；采集失败/模型差异/待完成均为零，
唯一 unsupported 是最后一次终止回合的清理保护，仍为 `DEPTH-FAIL`。

有友军单位、一血 Savagery、外层身体的预览伤害/累积伤害、无情期与真终局死亡
调度仍拒绝；没有开放玩家真终局入口，也不算完整整局或自然胜局验收。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Preview -SkipBuild`。

## 原始牌组全程空过整场校准

`true-final-boss-full-battle-calibration.mt2f` 来自
`full-battle-no-cards-20261022-214143-15622e29`。原生整局正常退出、败北，受保护
文件与原始资产/牌组/火炉未变；采集失败、模型差异、unsupported、待完成均为零，
首次通过该路线的整局验收。默认 UI 预览仍开启。

六次 EndTurn 全部匹配；模型只使用部署根状态独立推进到相同败北，完整状态/
牌堆/RNG 与原生逐步一致，16 条并发整局结果一致且根状态不变。出生、普通波次、
施法期、房间战斗、升层和预览都由模型自身状态串联。

原生七次有卡牌来源的火炉伤害中，前六次正常返回；最后一次未使用灾厄从三血
造成三点伤害，在 `AfterStopCombatLoop` 捕获时 ApplyDamage 尚未返回，也未进入
火炉 CheckForDeath/ShowKillCam。该边界火炉已不在房间，七张手牌仍保留，
KillCamActivated 仍为 false。模型只在该已校准的真终局 HeroTurn 卡牌伤害边界
延后清牌，不能把死亡镜头的更晚清理提前应用。

此路线没有友军单位，也没有撑到无情期或获胜；不关闭有友军单位、外层预览
伤害、一血 Savagery、无情期、真终局 Boss 死亡调度与玩家入口的剩余验收。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Battle -SkipBuild`。

## 单乘务员付费整场校准

`true-final-boss-allied-battle-calibration.mt2f` 来自
`full-battle-steward-once-20261022-220416-f60c06b3`。原始场景、五 Boss、牌组和
火炉定义未改；只打出一张原生 `TrainStewardBig`（8/25、三容量），其余空过。
原生正常退出、败北，受保护文件未变，采集失败/差异/unsupported/待完成全零。

支配者的 PostCombat 使第一层乘务员获得五层腐化，保持其所属楼层；采集器
同时记录所有楼层前排的外部目标。乘务员在第一次实际战斗死亡，回到耗尽牌堆。
预览死亡仅加入忽略名单，保留主物理站位；待命牌谓词在预览内报告死亡，
预览结束后恢复主状态。完整房间/整列上下文、牌堆、物理站位与 RNG 均匹配。

一个付费动作、六次 EndTurn、五次完整预览、十个列车阶段及 64 条阶段并发
分支通过。另从出牌前根状态执行原生付费动作，再独立推演整场，16 条并发
整局逐步一致，不注入后续原生 Before。其他友军、外层预览伤害、一血 Savagery、
无情期、Boss 死亡与玩家入口仍需独立验收。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Allied -SkipBuild`。

## 多友军与外层预览伤害整场校准

`true-final-boss-multi-allied-calibration.mt2f` 来自
`full-battle-units-and-junk-20261022-221718-b0754306`。只使用未改的起始牌组：
部署两个大乘务员与 `MonsterBanishedChampion1`，其余空过。原生正常退出、
败北、受保护文件未变，采集失败/差异/unsupported/待完成全零。

两层前排获得支配者腐化，远程状态更新保留原前后排顺序。中层外壳预览受伤
八点后，三个外壳生命分别为 3500/3492/3500，共享预览伤害为八；预览结束
恢复主生命。实际战斗则同步三个身体至 3492 血。Life Link 只在已捕获的真终局
外壳预览中允许这类独立生命；普通实战的主从生命不一致仍被拒绝。

三次原生出牌、六次 EndTurn、五轮预览、19 个房间边界、六次列车战斗和五次
升层全部匹配。64 条阶段/预览分支与 16 条从出牌前根状态推进的完整付费链
不改父状态，逐步完整状态/牌堆/RNG 一致。该路线最终由战斗伤害致死火炉，
与单乘务员路线的未使用灾厄停止边界分别校验；未进入无情期或真终局胜局。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case MultiAllied -SkipBuild`。

## 一血 Savagery 实际攻击与践踏

`true-final-boss-dormant-calibration.mt2f` 与
`true-final-boss-dormant-trample-calibration.mt2f` 分别来自
`full-battle-units-and-junk-20261022-222917-f78bc810` 和
`full-battle-units-and-junk-20261022-222705-876d60dd`。第一战斗前在隔离存档用
原生 DebuffHP 把 Savagery 降到一血；第二组另把第一层前排乘务员降至五血。
前后完整快照证明只有记录中的 HP 减少，RNG 不变；资产/起始牌组不改。
两进程正常退出、败北、受保护文件未变，四项失败计数全零。

第一组包含 73 次原生单位回合，其中 69 次发生在受控边界后，Savagery 五次
以一血进行攻击。原生友军的两次攻击跳过它，直接攻击后方外壳。第二组 63 次
单位回合中，59 次发生在边界后，Savagery 四次一血攻击；第一次十点攻击杀死
五血乘务员并造成五点践踏，后排原始流放者英雄从十血降至五血。普通选取目标
过滤 Pyre Bane，践踏沿用原生的后继查找；不能把前者过滤规则错误套到践踏。

两组完整列车阶段、五轮 UI 预览、卡牌和 RNG 一致；64 条不可变单位攻击分支
及 16 条从受控第一战斗边界开始、不回填原生快照的独立败局链均通过。
该项证明受控边界后的实际战斗，不把人工减血混入部署根状态的自然整场证明。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Dormant,DormantTrample -SkipBuild`。
原生采集入口增加 `-TrueFinalBossDormantSavagery`，践踏场景另加
`-TrueFinalBossFrontHealth 5`；均要求 `-OriginalTrueFinalBoss` 和距离 8。

## Hellforged 受控无情期与三层摧毁

`true-final-boss-relentless-calibration.mt2f` 来自
`full-battle-no-cards-20261022-225849-da644ac9`。第一战斗前在隔离进程克隆
运行时出怪模式、停止循环出怪并把队列指针移至末尾，再调用 Top 的原生
`AdvanceToAttackPhase(Relentless)`。前后完整快照独立验证这三项明确控制及
RNG 不变；持久资源、原始角色、牌组和火炉 80 血均未修改。该项不能当作
自然阶段切换；原生 Hellforged 仍不会因普通出怪队列耗尽而自行切换。

原生先置等待标记，下一次回合后动作才依次摧毁三层。Savagery 此时处于
第三层，献祭它会移除五个真终局实体，生命链接和存活全局控制器同时消失。
终局在外壳进入火炉战斗前中断动作协程，记录保留 `NativeReturned=false`
和 `TerminalWon=true`，不能伪装为正常返回。终局月相已经推进而回合号仍旧；
模型在相同边界停下。最终获胜、火炉 61 血，21 个房间阶段、六个列车阶段、
三个 EndTurn 与完整状态/牌堆/RNG 匹配；四项失败计数全零且原生 `DEPTH-PASS`。

四个阶段边界和 64 条不可变并发分支通过；终局沿用通用比较规则忽略已不可用的
UI 卡牌规则，仍比较楼层启用状态、完整战斗持久状态和残留控制器。16 条独立
整场分支从部署根状态出发，在第一战斗前应用一次明确控制，之后不回填原生
中间快照。销毁入口仍拒绝额外单位、装备、升级、死亡触发器和活动遗物效果。
此证据不覆盖自然胜局、自然致死伤害、额外友军或存活外壳的火炉战斗。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Relentless -SkipBuild`。
原生采集入口为 `-OriginalTrueFinalBoss -TrueFinalBossRelentless -BattleDistance 8`，
另带 `-Policy no-cards -PhysicalSpawnPoints -BranchCardMasks`。

## 普通攻击杀死 Dominion 的整场终局

`true-final-boss-lethal-calibration.mt2f` 来自
`full-battle-no-cards-20261022-231416-9cbf94a5`。只在第一战斗前用原生
`DebuffHP` 将 Dominion、Savagery 和 Entropy Bot 三个独立主体降至一血，
三个外壳依旧共享 Bot 的一血。完整前后快照和模型逐项验证 1554/2749/3499
三次减血；阶段仍为 Casting，循环出怪、队列、原始资产/牌组及火炉未改。

Dominion 先攻击火炉三点；火炉的普通 45 点攻击随后杀死一血 Dominion。
攻击前临时死亡预览会清理卡牌，实际死亡再调用五实体组移除。未被命中的
四个实体仍是一血，随后以销毁状态离场，不能将全组写成零血。跨层原生采集
现在能读取已经离场的对象；不会因它不在活动楼层列表中而漏报或抛出异常。

新进程原生获胜、火炉 77 血、受保护文件未变，`DEPTH-PASS` 且四项失败计数
全零。七个房间阶段、三个单位回合、三个列车阶段与两个 EndTurn 匹配；
16 条从部署根状态出发、明确应用一次减血控制后独立推演的整场分支一致。
普通终局死亡与已有无情期、生命更新、带友军真终局定向回归分别验证。
当前入口仅接受完整原始五实体、一血主体、无额外单位/装备/升级/死亡回调
和无活动遗物效果的 Dominion 致死；其他主体和自然胜局仍保留保护。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case Lethal -SkipBuild`。
原生采集为 `-OriginalTrueFinalBoss -TrueFinalBossLethal -BattleDistance 8`，
另带 `-Policy no-cards -PhysicalSpawnPoints -BranchCardMasks`。

## 致死 UI 预览与实战恢复

`true-final-boss-lethal-preview-calibration.mt2f` 来自
`full-battle-no-cards-20261022-231941-cac0f384`，在上述三次减血后显式标记
原生 UI 预览需刷新。两次完整预览分别覆盖健康状态四个房间和一血状态的
单个终局房间；后一预览预测 Dominion 死亡、火炉 77 血，并停止后续房间。
预览返回后五个实体及生命链接的实战/预览生命全部恢复为一血，预测的死亡
不会污染下一个实际 EndTurn。完整恢复后状态与原生战斗输入逐项一致。

新进程原生获胜、四项失败计数全零，70 个分支掩码上下文完整；两个 EndTurn、
三个实际单位回合、两次 UI 预览和五个预览房间均匹配。16 条独立分支从部署
根状态推演出生、三次明确减血、UI 刷新和实战获胜，完整终局状态/RNG 一致。
此项扩展未放宽生产模型范围，仍不表示自然胜局或其他主体的致死已覆盖。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case LethalPreview -SkipBuild`。
原生采集在 Lethal 参数上追加 `-TrueFinalBossLethalPreview`。

## 五主体致死伤害的完整房间与列车边界

`true-final-boss-terminal-damage-{dominion,savagery,entropybot,entropymid,entropytop}.mt2f`
复用原生五实体死亡探针，另记录完整房间/列车、外壳锚点、五实体组移除返回、
随后停止战斗循环四个边界。五个新进程都保留原始资产、显式将生命降为一血，
再分别执行一次无来源的原生一伤害。它们补充旧死亡组件，旧 fixture 继续保留。

五种命中主体的完整房间/列车结果均独立匹配，每种包含 64 条不可变并发分支；
包括卡牌清理、统计、物理锚点、RNG、生命链接清除和全部五实体离场。
命中外壳时三个共享生命均为零，其余两主体仍为一血；命中独立主体时只有它
归零，其余保留一血。此前离场的跨层外壳重建时漏用了共享生命结果，现已修正。

组移除返回时战斗仍运行；随后外层停止循环才推进月相并进入 EndOfCombat。
两种快照分别保留，完整伤害模型对照组移除边界，不将后续循环效果混入伤害。
原生组件最终均获胜；仍不代表自然整场胜局、带来源法术/攻击者的所有致死路径，
或其他主体的 UI 致死预览。该批证据为原始五实体、一血、无额外友军/装备/
升级/活动遗物效果的实战致死；后续付费施法补齐了普通友军存活的终局清理。
独立 UI 预览仍只开放已校准的 Dominion 范围。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case TerminalDamage -SkipBuild`。
原生采集仍用 `-TrueFinalBossDeath <主体>`，现在包含完整状态证据。

## 五主体的普通法术来源致死

`true-final-boss-terminal-spell-{dominion,savagery,entropybot,entropymid,entropytop}.mt2f`
来自 2026-10-23 五个全新进程，在同样明确的一血控制后，以原始无特征/升级的
伤害法术作为 `playedCard`，执行原生 `ApplyDamageToTarget`。Dominion 连续五次
一伤害：前四次消耗护盾，第五次死亡；其余主体分别一次致死。模型从各自首个
输入连续推进全部命中，不注入后续原生 Before。完整房间、列车、跨层共享生命、
卡牌/物理清理、统计和 RNG 均匹配，各 64 条不可变并发链通过。

该来源暴露了原生一次性死亡监听器的实际执行规则：`CreateHeroState` 先挂接
`CardStatistics.OnCharacterDeath`，真终局出生再挂接
`HeroManager.RemoveBossActionQueuedVFX`。`CoroutineSignal<T>.Invoke` 只推进
多播委托最后返回的协程，因此统计协程未执行；虽仍可观察到统计监听器存在，
不能据此增加击杀计数或刷新缓存。模型只在已校准的原始真终局组移除中复现
此行为，未改普通死亡统计。fixture 通过只读反射保留上述两个回调及顺序。

附加死亡监听器会改变这个原生顺序，`full-battle-no-cards-20261022-235914-e930f210`
仅供诊断，不登记或计入验收。最终五组均未附加监听器，原始文件未变，原生组件
获胜、正常退出。独立无来源致死、普通攻击整场与致死 UI 定向回归通过。

此项验收的是带法术来源的伤害协程，不包含支付、选中/打出卡牌的完整动作，
也不放宽任意特征、升级或攻击者签名。其他主体的 UI 致死仍未单独校准。

定向入口：`./scripts/Check-TrueFinalBoss.ps1 -Case TerminalSpell -SkipBuild`。
原生采集在 `-TrueFinalBossDeath <主体>` 上追加 `-TrueFinalBossDeathSpellSource`。

## 五主体完整付费施法致死

`true-final-boss-paid-spell-{dominion,savagery,entropybot,entropymid,entropytop}.mt2f`
各自从原始部署状态开始，结束部署并生成原始五主体，再显式减少三个独立生命
所有者的 HP 至一血。Dominion 另通过原生 API 明确移除四层法术护盾；没有把
这个准备动作当作自然护盾消耗。原始场景、实体、牌组和火炉均未修改。

五个新进程均实际支付 1 点能量，打出原始牌组中的伤害/附加状态法术，立即
获胜且火炉保留 80 血。每局一张牌、一次部署 EndTurn，捕获、差异、未支持和
待完成计数全部为零。完整卡牌去向、统计、跨层移除、终局状态和 RNG 一致。
模型曾只替换受击房间，遗留其他楼层的 Boss，现按组移除后的完整世界同步。

定向检查从部署根独立完成出生、显式准备和付费致死，不注入后续原生 Before；
每主体 16 条并发不可变链通过，并确认获胜动作在合法搜索前沿。

| 主体 | 原生来源（`.probe-runs/` 下） |
| --- | --- |
| Dominion | `full-battle-units-spells-and-junk-20261023-002250-c9c6fd6f` |
| Savagery | `full-battle-units-spells-and-junk-20261023-002300-bed36983` |
| EntropyBot | `full-battle-units-spells-and-junk-20261023-002423-24e1bf4a` |
| EntropyMid | `full-battle-units-spells-and-junk-20261023-002423-19d998fb` |
| EntropyTop | `full-battle-units-spells-and-junk-20261023-002523-a4c30fb1` |

入口：`./scripts/Check-TrueFinalBoss.ps1 -Case PaidSpell -SkipBuild`。
原生采集：`-OriginalTrueFinalBoss -BattleDistance 8 -TrueFinalBossLethalSpellTarget <主体>`。
仍为显式临界状态验收；自然真终局胜局、其他友军及升级/特征组合保持原有边界。

## 原生永久数值升级的友军

`true-final-boss-ally-upgrade-{health,damage}.mt2f` 保留原始真终局场景、敌军、
生命/阶段、火炉和抽牌，只在部署期给一张原始乘务员显式附加一个原生永久升级。
`StartingDeckUnchanged=false`，完整记录准备前后状态、原始升级名字、ID 和语义。
随后实际扣费召唤该卡，其他牌全部不打，不人工修改任何单位定义。

| 原生升级 | 召唤后签名 | 原生来源（`.probe-runs/` 下） |
| --- | --- | --- |
| `BlessingBonusHealth` | 8 攻击/45 生命，三容量 | `full-battle-units-and-junk-20261023-003719-710eff64` |
| `EnhanceAttackOnConsume` | 18 攻击/25 生命，三容量 | `full-battle-units-and-junk-20261023-003719-520725df` |

两局均自然进行六次结束回合后败北，原生四项失败计数全零、正常退出。
各五次 UI 预览完整恢复与实际整场状态/RNG 一致。定向检查从升级前部署根开始，
独立应用永久升级、扣费召唤并跑完整场；每种 16 条并发不可变链通过。
原有乘务员、双友军/基础英雄和出生定向检查继续通过。

生产保护只扩展到这两个原生数值升级的签名，并核对原始召唤卡的永久升级来源。
只有相同面板数值、没有相应升级来源时仍拒绝；其他友军、装备、升级组合和
自然真终局胜局未据此验收。

入口：`./scripts/Check-TrueFinalBoss.ps1 -Case AllyUpgrade -SkipBuild`。
原生采集：`-OriginalTrueFinalBoss -BattleDistance 8 -TrueFinalBossAllyUpgrade Health|Damage`。

## 按普通召唤/攻击签名开放友军

`true-final-boss-ordinary-ally-{steward,warrior,animus}.mt2f` 从原生目录筛选
仅有召唤效果、无单位触发器/技能/装备/升级的原始卡。部署前明确添加一张
对应单位卡，保留原始定义、敌军生命/阶段、火炉和能量；牌组准备已显式标记。
实际扣费在大乘务员后方召唤所选单位，其他卡不打。

| 单位 | 原始签名 | 对 Savagery 的第一轮实战伤害 | 原生来源（`.probe-runs/` 下） |
| --- | --- | --- | --- |
| 小乘务员 | 14/5，二容量，普通攻击 | 14 | `full-battle-units-and-junk-20261023-005911-0248d680` |
| HornedWarrior | 15/8，二容量，Multistrike 1 | 30 | `full-battle-units-and-junk-20261023-005710-973031fb` |
| AnimusOfWill | 3/8，二容量，Multistrike 2 | 9 | `full-battle-units-and-junk-20261023-005710-354bbd95` |

三个新进程均两次付费出牌、六次结束回合后败北，四项失败计数全零。
各五次 UI 预览/恢复、完整状态和 RNG 匹配；从准备后的根独立付费出牌并
推进整场，各 16 条并发不可变链一致，不注入后续原生 Before。
该项未把人工添加卡牌的准备动作或败局当作自然牌组/自然胜局验收。

生产保护按原始召唤卡定义和运行时结构判断，接受上述普通攻击签名；只读增加
原始出生生命上限，连同攻击/容量/初始状态共同核对，旧采集缺少生命元数据时拒绝。
不依赖单位名称。额外触发器、技能、装备、其他初始状态、克隆和
无法核对的召唤来源仍受保护；带未知升级的相同单位也明确拒绝。

入口：`./scripts/Check-TrueFinalBoss.ps1 -Case OrdinaryAlly -SkipBuild`。
原生采集：`-OriginalTrueFinalBoss -BattleDistance 8 -TrueFinalBossOrdinaryAlly Steward|Warrior|Animus`。

## 普通友军的受击回调签名

原始 Avenger 的 `OnHit → LastAttackerCharacter → melee weakness 1` 现通过
完整原生整场校准；原始五主体属性/阶段未改，明确添加单位卡。两次付费召唤、
六次 EndTurn、五次 UI 预览及 16 条独立完整链一致，四项计数为零；路线败北。
带回调友军的终局组死亡、勇气组合和十种触发签名变化仍拒绝。
详见 [Avenger 独立证据](true-final-boss-avenger.md)。

## 普通友军的永久数值强化签名

同一普通召唤/攻击签名现可携带至多两个纯正数值永久升级，按原始攻击/生命、
有序升级和原生统计计算规则核对实际单位；不按升级或单位名称逐个放开。
升级必须只增加攻击或生命，且保留完整来源、掩码和生命周期元数据。
额外状态、卡牌/单位回调、技能、装备、偏移、临时升级、克隆和无法核对的
原始生命仍拒绝。攻击强化的原始伤害缓存只在与当前数值一致时接受。

| fixture 后缀 | 明确准备的原生升级 | 实际单位 / 首轮伤害 | 原生来源（`.probe-runs/` 下） |
| --- | --- | --- | --- |
| `warrior-health` | `BlessingBonusHealth`，生命 +20 | 15/28，Multistrike 1 / 30 | `full-battle-units-and-junk-20261023-012003-4e0f2389` |
| `animus-damage` | `EnhanceAttackOnConsume`，攻击 +10 | 13/8，Multistrike 2 / 39 | `full-battle-units-and-junk-20261023-012003-df432cab` |
| `warrior-both` | 上述两个升级依次叠加 | 25/28，Multistrike 1 / 50 | `full-battle-units-and-junk-20261023-012200-4ac0ed53` |

文件为 `true-final-boss-ordinary-ally-<后缀>.mt2f`。三组均两次付费召唤、
六次 EndTurn、五次原生 UI 预览与恢复；各 16 条从准备后的根出发的完整
独立不可变链匹配所有状态/RNG。三个新进程均正常退出、四项失败计数全零。
这是显式添加原始单位卡并应用原生升级的受控牌组；敌军生命/阶段与原始
单位定义未改，三局均败北，不计自然牌组或自然胜局。

缺少升级来源、附加卡牌回调、临时升级、未知空升级及缺少原始生命元数据的
负例继续拒绝。旧三组普通友军、两种乘务员强化及普通狱魔玩家冻结重放通过。
最初缺少分支掩码/物理站位采集的来源仅作诊断，不登记验收。

入口仍为 `./scripts/Check-TrueFinalBoss.ps1 -Case OrdinaryAlly -SkipBuild`。
原生采集增加 `-TrueFinalBossOrdinaryAllyUpgrade Health|Damage|Both`，
该入口自动开启分支卡牌掩码和物理站位采集。未运行全量回归或构建 Mac 包。

## 健康独立主体下的付费生命链接保护

`true-final-boss-protected-spell-{dominion,savagery,entropybot,entropymid,entropytop}.mt2f`
分别命中五个原始主体。只将命中所属的独立生命所有者明确降至一血，其余
独立所有者保留原始生命；命中外壳时只调整底层所有者，原生链接会同步三层。
Dominion 另明确移除四层法术护盾，以检验生命链接的伤害拦截。
没有修改原始单位定义、牌组、火炉或阶段。

| 命中主体 | 独立所有者减血向量（Dominion/Savagery/外壳） | 原生来源（`.probe-runs/` 下） |
| --- | --- | --- |
| Dominion | 1554/0/0 | `full-battle-units-spells-and-junk-20261023-013602-3c2b1fce` |
| Savagery | 0/2749/0 | `full-battle-units-spells-and-junk-20261023-013602-164774d1` |
| 底层外壳 | 0/0/3499 | `full-battle-units-spells-and-junk-20261023-013811-ffc2a4b1` |
| 中层外壳 | 0/0/3499 | `full-battle-units-spells-and-junk-20261023-013811-86e5f818` |
| 顶层外壳 | 0/0/3499 | `full-battle-units-spells-and-junk-20261023-014020-65f3f4ba` |

每组实际支付一能量，原始 `StarterFireStarter` 的一伤被生命链接完全阻止，
随后仍正常添加两层 Pyregel；五个主体均存活、生命链接状态不变。
原生 `ProtectedByLifeLink` 和模型均要求其他独立所有者不高于一血，
才允许当前主体死亡。因此生产死亡保护保持原样，不能据此开放提前致死。

各一张付费法术、六次 EndTurn 后正常结束为败局，原文件未变、四项失败
计数全零。独立从部署结束回合出生，复现唯一已声明的 HP/护盾控制，付费
施法后连续推进全部余下回合；各 16 条并发不可变链的完整状态/RNG 一致。
独立链不注入任何后续原生 Before。搜索前沿保留实际合法命中，并明确验证
不会错误地把受保护的目标判为胜局；正常死亡入口在这些根状态上继续拒绝。

旧五个全临界血量的付费胜局、正常火炉致死/UI 预览和双强化友军完整链通过。
最初以提前致死为目标的两个诊断进程因生命链接阻止死亡而未完成整场，
未作为验收归档。新入口只打一次明确付费法术，再推进完整余下回合。

入口：`./scripts/Check-TrueFinalBoss.ps1 -Case ProtectedSpell -SkipBuild`。
原生采集：`-OriginalTrueFinalBoss -BattleDistance 8 -TrueFinalBossProtectedSpellTarget Dominion|Savagery|EntropyBot|EntropyMid|EntropyTop`。
这是明确控制生命的机制验收，不计自然受伤或自然胜局；未运行全量回归或构建 Mac 包。

## 存活普通友军下的付费终局清理

`true-final-boss-allied-paid-spell-{dominion,savagery,entropybot,entropymid,entropytop}.mt2f`
先明确添加一张原始小乘务员，实际花费一能量在底层召唤 14/5、二容量单位。
正常结束部署回合出生五主体，再按已声明的三个生命所有者减血控制准备临界
状态；Dominion 另移除四层护盾。随后原始 `StarterFireStarter` 实际付费命中
选定主体，移除全部五个 Boss，同时保留小乘务员 14/5 与火炉 80 血。

五个原生来源（`.probe-runs/` 下）依次为：

- Dominion：`full-battle-units-spells-and-junk-20261023-014457-35fb65ff`。
- Savagery：`full-battle-units-spells-and-junk-20261023-014457-e974c0e7`。
- 底层外壳：`full-battle-units-spells-and-junk-20261023-014628-afd22c12`。
- 中层外壳：`full-battle-units-spells-and-junk-20261023-014628-ff143ddc`。
- 顶层外壳：`full-battle-units-spells-and-junk-20261023-014740-594d92b6`。

五个进程均正常退出、原文件未变、四项失败计数全零。各两次实际付费出牌、
一次部署 EndTurn，16 条从添加卡牌后的准备根出发的独立不可变链通过。
召唤、出生、明确 HP/护盾控制、施法、全组移除和完整胜利状态/RNG 连续匹配，
不注入后续原生 Before；比对包含存活友军及原始召唤卡身份、牌堆、统计和锚点。
搜索前沿也包含实际付费胜局。

生产终局保护现在识别已校准的普通友军签名，保持五主体均一血、原始 Boss
布局和无活动遗物等约束；给友军添加未知空升级仍明确拒绝。独立主体健康时的
生命链接保护继续保留，其他主体致死 UI、额外回调/装备和自然胜局另行验收。
本批明确添加单位并调整所有者生命，不能作为自然对局胜利。

入口：`./scripts/Check-TrueFinalBoss.ps1 -Case PaidSpell -SkipBuild`，同时验证
旧五个无友军基线及新增五个有友军场景。
原生采集增加 `-TrueFinalBossLethalAlly`，与原始致死法术目标一起使用。
未运行全量回归或构建 Mac 包。

## 原始鹰心命令普通友军致死

`true-final-boss-commanded-attack.mt2f` 来自
`full-battle-units-spells-and-junk-20261023-020805-1ed9c9f4`。
明确添加原始小乘务员与 `Eagleheart`（`1737fa0b-7f37-41e8-9183-ed9e6a391556`），
保留卡牌定义；根据捕获的洗牌 RNG 交换初始抽牌堆的两个位置，RNG 本身不变。
实际支付一能量召唤小乘务员，正常结束部署、洗牌、出生并抽到鹰心，再按既有
三所有者减血控制准备五主体一血。实际支付两能量的鹰心执行前移、五层勇气、
命令攻击的完整效果链。普通攻击绕过一血休眠的 Savagery，击杀底层外壳，
清除五主体并保留小乘务员 14/5、五层勇气与火炉 80 血；实际攻击为 19。

原生进程正常退出、原文件未变、`DEPTH-PASS`、四项失败计数全零。只读记录
确认实际攻击者与底层外壳目标，完整单位回合状态/RNG 一致。16 条并发不可变
分支从添加卡牌及安排牌序后的根出发，连续复现召唤、部署出生、正常洗牌抽牌、
已声明的减血控制和最终付费命令，不注入后续原生 Before；搜索前沿包含胜局。
五层勇气签名和底层外壳的内部命中预览现已校准，六层勇气仍被明确拒绝。

最初的抽牌、预览保护诊断记录不计验收。普通友军六个基线、生命链接保护
五个反例、既有致死 UI 预览及十个付费伤害胜局继续通过定向检查。
入口仍为 `./scripts/Check-TrueFinalBoss.ps1 -Case PaidSpell -SkipBuild`；
原生采集使用 `-OriginalTrueFinalBoss -BattleDistance 8 -TrueFinalBossCommandedAttack`。
此项不代表其他勇气数量、攻击者回调/装备、全部致死 UI 或自然胜局已验收。
未运行全量回归或构建 Mac 包。

## 原始 Pyreshot 命令火炉跨层致死

`true-final-boss-pyre-command.mt2f` 来自
`full-battle-units-spells-and-junk-20261023-021609-527762fb`。使用同一明确添加
小乘务员和原始命令牌、安排初始牌序、正常部署洗牌抽牌及三所有者减血控制。
保留 `Pyreshot`（`c30c479c-e7aa-4628-9091-dd5d2fb8e5a6`）三费、火炉目标、
选定楼层攻击与抑制火炉层聚焦的原始定义；实际支付三能量，火炉攻击底层
外壳并清除五主体。小乘务员 14/5 和火炉 80 血保留，火炉物理位置仍为三层。

原生正常退出、原文件未变、`DEPTH-PASS` 且四项失败计数全零。原生单位回合
捕获包含完整远程火炉，实际命中目标、全部状态/RNG 与独立模型匹配。16 条
从初始准备根连续推进至胜利的不可变链匹配；合法搜索前沿包含该付费胜局。
修复共享光环同步把远程攻击者写进攻击楼层的问题，并在火炉命令导致组死亡
后使用已更新的全局房间，避免从旧房间重新带回其他楼层的 Boss。

原有 Pyreshot 整场原生夹具、受命攻击纯模型检查和鹰心/付费伤害/生命链接
保护定向用例通过。入口 `./scripts/Check-TrueFinalBoss.ps1 -Case PaidSpell -SkipBuild`
现包含十个伤害法术场景及两张命令牌；采集增加 `-TrueFinalBossPyreCommand`。
此项为明确减血的受控胜利，不计自然胜局；未构建 Mac 包。

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
