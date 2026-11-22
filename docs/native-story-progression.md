# 真终局自然解锁进度

原生 `StoryProgressionData` 的真终局奖励位于剧情索引 11，条件是
`UnlockedPyreHearts >= 9`，还需实际推进相应剧情对话。这个计数不含默认
`PyreHeartStarter`。标准八战通关本身不能替代这些条件。

2.3.1 隔离实例从原版契约 10 存档读取了三个试炼门槛。第三、六、八环开启前分别需要
0、1、2 次试炼胜利，第八环后由原版 `TrueFinalBossUnlockC` 事件确认挑战。
该存档已有自然解锁的剧情和薪火之心，第 3 环战前记录为两场普通胜利及零场试炼胜利。
原版资格审计运行了两个只读事件，存档哈希校验通过。
报告为 `.probe-runs/campaign-story-20261122-041551-4216e964/native-story-progression.json`。

同一存档在 2.3.1 的 Arkion 顾问与独立固定复放均以 22 点薪火获胜。原版战役续接再次
复放这场战斗并执行 66 个原版结算、奖励与地图事件，保存出第四环战前档。
新档记录前三战全胜，第三战为第一场泰坦试炼胜利，且未选择最终挑战。
检查点为 `.probe-runs/campaign-ring4-20261122-042209-f16d0f66`，主存档 SHA256 为
`e9a7eba59e77c0b916bb91886531e4191c82f688d463ede38e4f114193b8643e`。

契约 10 第四环的 37 个输入在两个原版进程中完整复放并均判负。原版结算后保存了空闲档，
再从其元进度开种子 `424242` 的契约 0 新局。战前事件和存档均确认实际契约为 0，
元进度仍允许挑战真终局，保留自动选择泰坦试炼的偏好。第 1 环战前档为
`.probe-runs/campaign-ring1-20261122-054703-6a8ac9f3`，主存档 SHA256 为
`c06ee6618d4dcf387a505bd092af567bf7e4b9658aac781966bf3daf3e218d33`。

第 1 环原版胜局保留 50 点薪火生命。独立进程复放 18 次出牌或技能和 5 次结束回合，
完整状态、随机流与终局一致。原版战役结算保存第 2 环战前档，已记录一场普通胜利。
归档为 `unlocked-covenant0-ring1.mt2f` 与 `unlocked-covenant0-ring1-frozen.mt2f`，
续接档位于 `.probe-runs/campaign-ring2-20261122-055103-b991b69c`。

第 2 环同样以 50 点薪火生命获胜。独立原版进程复放了 15 次出牌或技能、5 次结束回合，
完整状态和随机流一致。原版第 3 环战前档已记录两场胜利，且首场泰坦试炼已开启。
归档为 `unlocked-covenant0-ring2.mt2f` 与 `unlocked-covenant0-ring2-frozen.mt2f`，
续接档位于 `.probe-runs/campaign-ring3-20261122-055559-21b0e6fb`。

第 3 环 Siphon Rage 泰坦试炼以 35 点薪火生命获胜。前 11 个原版输入固定复放，顾问生成后续方案，
整战共 33 次出牌或技能和 9 次结束回合。独立原版进程的输入、完整状态、预测和随机流一致。
两份归档为 `unlocked-covenant0-ring3-siphon-trial.mt2f` 与 `unlocked-covenant0-ring3-siphon-trial-frozen.mt2f`。
原版写入第 4 环战前档 `.probe-runs/campaign-ring4-20261122-061404-c55c1cc1`，
存档保留前三战胜利，且第 3 战明确记录为泰坦试炼。

第三环的另一条部署路线让 Arkion 在新月登场。原版核对了 8 个部署输入和首个结束回合，
出场时月相掩码为 5，登记 1 层 `newmoon`。顾问复用已有月相初始化。出场、首次行动、
完整回合后继及随机流一致。二进制组件归档位于 `mechanics/siphon-rage-new-moon-birth/`。

第 4 环顾问接续前 37 步后完成原版败局，验证了超时短方案可继续发布。另一路线离线求解后，
在两个独立原版进程中复放全部 43 步，均以 10 点薪火生命获胜。输入、完整状态与随机流一致。
归档为 `unlocked-covenant0-ring4-defeat.mt2f` 和 `unlocked-covenant0-ring4-victory.mt2f`。
原版第 5 环战前档为 `.probe-runs/campaign-ring5-20261122-065100-a04d00d7`。

同一第 3 环试炼胜局可在原版选左路续接。隔离实例完整复放 42 步并执行 55 个战役事件，
经过治疗点、遗物商店和原版剧情事件，第 4 环战前薪火生命为 61，已验收胜局所用右路战前为 46。
左路战前档为 `.probe-runs/campaign-ring4-20261122-080904-9ece3bef`，主存档 SHA256 为
`ac7ccb8605560cbada505fcc8096101103fb6763f83b3a3018363a8665127cb4`。
顾问在开场手牌中遇到 `Purified card` 保护。隔离原版从同一存档部署已净化的 `LadyOfTheReformed`，
实际支付 1 点能量并失去原始 `ephemeral` 和 `OnHit`，结算后记录仍有 `newmoon` 和 `OnKill`。
单步原版后继归档为 `mechanics/purified-card-deployment/native-branch.mt2f`，现已通过完整状态与随机流复放。
离线同时验证部署前后可结束回合，以及未验证的特殊效果仍受保护。
左路整战复试通过前 13 个输入。第 14 步原版技能把上一回合打出的 Gravity 复制到手牌，并保留其永久升级。
顾问预测少了这项升级，整场验收在该处停止。原版结算归档为
`mechanics/last-turn-copy-permanent-upgrade/native-partial.mt2f`。
复制后已复用手牌月相更新，第 14 步在独立的原版进程中完整匹配，顾问继续推进至第 31 步。
第 31 步结束回合的原版承伤累计为 153，预测为 156。前 30 个输入已匹配。该分支的原版结算归档为
`mechanics/relentless-monster-damage/native-partial.mt2f`。
修正原版燃命和腐化的结算顺序后，隔离实机复放前 31 步并由顾问接续余下 2 步，
以 41 点薪火生命赢下第 4 环。33 个输入逐步对齐，四项采集计数为零。
胜局归档为 `unlocked-covenant0-ring4-healing-route-victory.mt2f`。同一进程继续执行原版战役结算并保存
第 5 环战前档 `.probe-runs/full-battle-units-spells-and-junk-20261122-090359-6cf37987-before-ring5`，
主存档 SHA256 为 `3d5b46defe3abd0306cdd13f7db89b944b1310c731fb5b9a55f74747ef5fb108`。
从 41 点薪火生命开始的第 5 环已匹配前 14 个原版输入。第 15 步使用上一回合的 Witchweave 副本，
原版结算与预测在牌堆和消耗计数上首次出现差异。原版后继记录为
`mechanics/copied-card-consume-routing/native-partial.mt2f`。
修正短命副本的消耗结算后，原版复放前 15 步，顾问续接后完成全部 36 步。四项采集计数为零，
原版判负，首领剩 681/900 生命。整战归档为 `unlocked-covenant0-ring5-healing-route-defeat.mt2f`。
另一种部署的 41 个输入在独立原版进程中完成。最后一步由 Lunar Echo 复制带原版先天升级的 RapidRaise，
完整原版后继归档为 `mechanics/intrinsic-last-turn-copy/native-branch.mt2f`。修正后 41 个输入的完整状态与随机流均通过复放。
另一条布阵的 Gravity 移动验证了局部月相回调。43 个原版输入的完整状态与随机流通过复放，
归档为 `mechanics/gravity-moon-movement/native-branch.mt2f`。
同一战前档的另一条部署路线走了 47 个原版输入。末步复制带满月升级的 Gravity，副本保留一份该升级和一次移动效果。
原版后继归档为 `mechanics/last-turn-copy-existing-moon-upgrade/native-branch.mt2f`。
原版商店的对照路线购买了薪火治疗升级，用在 Gravity，RapidRaise 保持未升级。第 5 环仍从 41 点生命开始，
战前档为 `.probe-runs/campaign-ring5-20261122-100542-b71c8670-before-ring5`，主存档 SHA256 为
`30870b841f2870edfa995fc902b2b9712282ed6bf68c8538c0f803f950c27f21`。第 23 步的 Gravity 副本
首次在结算效果列表偏离，原版后继归档为 `mechanics/last-turn-copy-pyre-healing/native-partial.mt2f`。
修正永久升级的复制投影后，原版前 23 步的完整状态与随机流通过复放。

从原 41 点薪火生命战前档出发，调整开局部署人数、顺序和站位后，第 5 环以 9 点薪火获胜。
离线方案的 50 个输入在隔离原版进程逐步复放，终局、完整状态、随机流及四项失败计数都通过独立比对。
胜局归档为 `unlocked-covenant0-ring5-roster-victory.mt2f`。同一进程完成战后领取和地图选择，保存了
第 6 环泰坦试炼战前档 `.probe-runs/full-battle-units-spells-and-junk-20261122-101943-8826725e-before-ring6`。
该档记录前五战全胜，主存档 SHA256 为 `3407bd18b9bbbcccdea847b70312a3a5a43c79d9170eab5043c378aa37430a11`。
该胜局的 50 个录制决策和另一条 Follower 路线的 34 个录制决策共枚举 1,568 个出牌或技能候选
与 84 个结束回合后继，原版输入的完整后继与随机流匹配，零未支持、零截断。
同一第 4 环胜局也已在原版奖励界面改选 `RageOnBurnoutStackLoss`，领取结果和其余流程通过原版战役验证。
第 5 环战前档为 `.probe-runs/campaign-ring5-20261122-115007-050548a2-before-ring5`，主存档 SHA256 为
`2e894fc3caa702ecd30f3d4c5e59dffefd244b67661028c35242fdf207df37db`。带该遗物的月相手牌刷新已有
13 步原版记录 `mechanics/burnout-rage-moon-hand/native-branch.mt2f`。
经过治疗点后，第 6 环试炼开场为 28 点薪火生命。顾问首回合遇到敌方攻击增益校验。
独立原版进程已完成同一开场的结束回合，归档为 `mechanics/angel-duo-trial-entry/native-branch.mt2f`。
补齐试炼攻击增益、生命增益和满月出生后，独立原版进程复放了全部 53 个固定输入。完整状态、随机流、
终局及四项失败计数均通过。原版判负，两名首领分别剩 209 和 1100 点生命，归档为
`unlocked-covenant0-ring6-duo-trial-defeat.mt2f`。
该归档的全部 53 个决策根还通过了候选枚举，覆盖 961 个出牌或技能候选及 53 个结束回合，零未支持、零截断。
同一第 5 环胜局的另一组原版战后选牌取得 Hexcalation 和 KillRandomUnit，保留左路治疗点。
第 6 环仍从 28 点薪火开始，战前档为 `.probe-runs/campaign-ring6-20261122-112923-5374f92c-before-ring6`，
主存档 SHA256 为 `3c53ce0916845594cf2a856342eae9b33c4670fee6008bfdb7e8adc0e7c13b12`。

原版复放同一第 5 环的 50 步胜局后走右路，删两张牌、领取金币，在原版商店买到
Bloodthirsty Blade、Eclipse Chamber 和 Zone of Silence。第 6 环战前档为
`.probe-runs/campaign-ring6-20261122-125402-900b5069-before-ring6`，主存档 SHA256 为
`fade21b8c7c61ed1c1c618e06c3c298e3e4634d8e893f5f85aca06bf03b453c6`。
用 `Run-FullBattleProbe.ps1 -CapturePlayerDeploymentOnly` 从该战前档在约 26 秒内录得原版
第 0 回合。顾问纯捕获一次，零搜索、零建议发布、零战斗输入。
快照在 [ring6-right-zone.mt2f](../tests/compatibility/game-2.3.1/mechanics/player-deployment-only/ring6-right-zone.mt2f)。

从该快照离线筛查，三条同策略对照在前四个普通回合均未抽到沉默房，底层设定的薪火维持
9 点，两个首领分别剩 759 和 1237 血。继续用底层和顶层部署优先策略，各自独立复放了
54 与 34 步，均在模型中判负。底层策略终态两首领剩 808 和 1237 血，顶层策略剩 1076
和 1100 血。原版证据覆盖战前路线和部署快照，两条终局属于离线推演。

左路另将原版商店的 `UnitUpgradeEndless` 放在第一优先级。第一次点击后商店停留，
修正后只在同一报价仍可买且尚未扣钱时重试。原版确认 `AddEndless` 的牌组数量由
0 增至 1，并建立了第 6 环战前档 `.probe-runs/campaign-ring6-20261122-131858-3114693d-before-ring6`。
这次恢复保留 28 点薪火，录得 Endless 装在 Lady of the Reformed 上。部署快照约 20 秒完成。
[`ring6-left-endless.mt2f`](../tests/compatibility/game-2.3.1/mechanics/player-deployment-only/ring6-left-endless.mt2f)

同一快照的底层、顶层离线策略分别通过 61 步和 63 步独立复放，均在第 11 回合的
模型终态落败。底层策略击败 Savage，Entropic 剩 969 血。顶层策略两首领剩 669
和 1102 血。离线后继及随机流通过复放。

从第 5 环胜利后、原版地图尚未分岔时保存的一份存档，还直接续到同样的左路
商店强化和下一战入口。原版继续过程没有重新执行第 5 环的 50 个输入。
源档为 `.probe-runs/native-post-ring5-before-map-20261122-132525`，保存时已记录五胜，
主档 SHA256 为 `e907018194db2c60fed1bb6717aada6e57ab68cd3d1f778cd6890484fbe2a871`。
`Run-CampaignProgress.ps1 -CaptureBeforeMap` 已在该档实测。它复制相同哈希的原版
胜后存档，此时地图分支还没选。之后同一进程完成左路原版购买和下一战入口，
复制件保存在 `.probe-runs/campaign-ring6-20261122-133531-4f888e78-before-map-ring6`。

从该地图前档另选 Hexcalation 与 KillRandomUnit，再购买同一 Endless 强化，
同样通过原版战役检查。新原版部署快照在
`.probe-runs/full-battle-units-spells-and-junk-20261122-133838-1c3a71e4/player-deployment.mt2f`。
两种部署的 70 步与 68 步离线复放均判负，签名状态及随机流验证通过。

第 5 环的首次整场对照从 10 点薪火生命开始，原版判负且四项采集计数为零。另一条部署路线验证了
StealthWaxers 的出生条件与月相手牌更新可同时生效。两次部署与结束回合的后继在原版完全一致，
归档为 `mechanics/stealth-waxers-moon-refresh/native-branch.mt2f`。
从同一第 4 环胜局另选原版左路，生成第 5 环战前档 `.probe-runs/campaign-ring5-20261122-071048-39525a08`。
左右两条路线均已对账至原版终局，均判负。右路记录 10 个输入，左路记录 26 个输入。
整战归档为 `unlocked-covenant0-ring5-right-defeat.mt2f` 和 `unlocked-covenant0-ring5-left-defeat.mt2f`。
左路红冠部署的原版观察记录了 27 个实际可选装备、选中的 Glass Cannon、装备归属与战斗随机流推进。
原始观察和修复后复测分别归档为 `mechanics/red-crown-equipment-observation/native-branch.mt2f` 与
`native-supported-branch.mt2f`。直接部署已与原版对齐完整后继、挂载顺序、新装备出牌规则与随机流，
并接入 2.3.1 兼容检查。结束回合期间触发同类出生仍待原版验证。
同一左路存档的整场补采确认红冠部署与装备挂载。装备中间帧的房间选择刷新将新装备登记到统计缓存，
原版调用栈与缓存前后值均保留。冻结复放通过 22 次出牌、9 次结束回合和全部录制候选，
原版结果为败局。[整战归档](../tests/compatibility/game-2.3.1/mechanics/player-advisor-rolling/unlocked-covenant0-ring5-left-red-crown.mt2f)
左路另选 Follower 和另一组奖励后，原版对账通过 34 个输入，终局仍判负，首领余 798/900 生命。
该局归档为 `unlocked-covenant0-ring5-follower-defeat.mt2f`。

2026-10-28重新检查第25组第七战入口的累计档案，额外薪火之心仍为0/9，
剧情索引0、真终局未解锁。当前值为Pact0/7、Life326/475、Malicka0/1、
Wyngh109/225、Fhyra3605/20000、Aquath460/12000、Bogworm169/3200、
Time0/25、Herzal2105/10000；这些是同一继承分支的原生值，不合并其他分支。
[本次原生快照](../tests/fixtures/evidence/campaign/native-story-progression-random25-ring7.json)
SHA256为`00be69a29c1ce1cff31abe4187a422753323cdc218dfc5c3167268755a0667ca`。
观察进程`campaign-story-20261028-053525-dc6c47e8`只读完成两个审计事件，
原输入及真实玩家文件未变；输入存档SHA256为
`aed7c6ab0bce1a5d4a0757db3c21ef084bf4d3f0662c00c504da63221bcb1610`，
原生元档案SHA256为`3d87a66abd82725714bb45f4e60d515a7b830e5a1f240d0d487a20d85e36c215`。
下面保留早期独立档案的基线，不能将其进度与本次相加。

在已经验收的 seed424242 标准通关存档上，以只读进程加载原生数据，得到
额外薪火之心 0/9、剧情索引 0、真终局功能未解锁。原生条件如下：

| 薪火之心 | 原生条件 | 当前值 | 目标 |
| --- | --- | ---: | ---: |
| Pact | BattlesWonWithTrial | 0 | 7 |
| Life | PyreDamage | 135 | 475 |
| Malicka | NumChallengesWon | 0 | 1 |
| Wyngh | UnitsMoved | 67 | 225 |
| Fhyra | CoinsSpents | 1510 | 20000 |
| Aquath | MagicPowerBonusDamage | 90 | 12000 |
| Bogworm | DebuffsApplied | 142 | 3200 |
| Time | CardsFrozen | 0 | 25 |
| Herzal | DamageDoneByTrainStewards | 719 | 10000 |

挑战模式剧情奖励另要求额外解锁两个薪火之心；六个对应 `UnlockCrew` 奖励。
表中是这个已完成档案的实际进度，不代表用户实机存档或其他隔离分支。
三个击败真终局后解锁的薪火之心不计入这九个前置目标。

证据保留在
[原生快照](../tests/fixtures/evidence/campaign/native-story-progression-after-standard.json)。
源为 `campaign-completed-ring8.zip` 恢复的 48 个已校验成员，观察进程为
`campaign-story-20261023-063754-d268f1e6`：正常退出、原文件未变、只有两个
审计事件、没有开始或恢复战斗。它是原生进度观测，不是新的战役胜局。

```powershell
pwsh -NoProfile -File scripts/Run-CampaignProgress.ps1 -InputProfile <restored-profile> -StoryProgressionOnly -NativeWorker 2 -TimeoutSeconds 120 -SkipBuild
```

该模式在档案加载完成后立即读取原生条件，不点击解锁、不调用作弊命令。
后续自然战役完成时也会自动写出同样的快照；检查点打包可保留该文件。

seed42 的自然腐化者路线现也完成原生八战 Standard 结算。其
`campaign-completed-corrupter.zip` 已包含 `AfterEndGame=true` 的快照，
结果同样为额外薪火之心 0/9、真终局未解锁。该档案的全部 33 个载荷恢复校验
通过；它与表中 seed424242 分支分别保留，不合并两个隔离分支的进度。

seed424244 的自然 Fragile 路线也已完成原生八战 Standard 结算，最终战使用
三回合／3000ms、底层偏好的真实玩家建议及独立回放。完成档案
`campaign-completed-fragile.zip` 的 26 个载荷恢复校验通过；其
[通关后原生快照](../tests/fixtures/evidence/bosses/native-story-progression-after-fragile.json)
仍为额外薪火之心 0/9、剧情索引 0、真终局未解锁。三份标准通关档案独立保留。
