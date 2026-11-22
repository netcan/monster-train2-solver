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
`ac7ccb8605560cbada505fcc8096101103fb6763f83b3a3018363a8665127cb4`。左路第 4 环战斗待验收。
顾问在开场手牌中遇到 `Purified card` 保护。隔离原版从同一存档部署已净化的 `LadyOfTheReformed`，
实际支付 1 点能量并失去原始 `ephemeral` 和 `OnHit`，结算后记录仍有 `newmoon` 和 `OnKill`。
单步原版后继归档为 `mechanics/purified-card-deployment/native-branch.mt2f`，现已通过完整状态与随机流复放。
离线同时验证部署前后可结束回合，以及未验证的特殊效果仍受保护。
左路整战复试通过前 13 个输入。第 14 步原版技能把上一回合打出的 Gravity 复制到手牌，并保留其永久升级。
顾问预测少了这项升级，整场验收在该处停止。原版结算归档为
`mechanics/last-turn-copy-permanent-upgrade/native-partial.mt2f`。

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
