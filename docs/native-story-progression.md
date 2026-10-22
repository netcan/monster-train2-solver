# 真终局自然解锁进度

原生 `StoryProgressionData` 的真终局奖励位于剧情索引 11，条件是
`UnlockedPyreHearts >= 9`，还需实际推进相应剧情对话。这个计数不含默认
`PyreHeartStarter`。标准八战通关本身不能替代这些条件。

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
[原生快照](../tests/fixtures/native-story-progression-after-standard.json)。
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
