# Soul Savior 原版开局检查点

`Run-CampaignProgress.ps1 -RegionRun` 从已安装的 Railforged 界面进入 Soul Savior，沿用原版选魂、套牌和地图流程。
续接时读取隔离目录中的 `saves/save-regionRun.json`，通过原版接口恢复战前状态。
`-StopBeforeRing` 在这个模式下使用本局战斗序号，因为选择区域时原版地图距离可以跳跃。

已验证的隔离输入为 `.probe-runs/campaign-ring6-20261122-133710-1e721a5d`。
输入的 RegionRun 存档为空闲状态，SHA-256 为 `9b4215257f0a37c29a45cd05ed068ae840f29c4ff088d9d57d138dae52291e73`。
元进度存档 SHA-256 为 `af94977f2cdf817781b9ba071a1a5d0d8dc91070dbf8684a8a6a72efc56aa0bd`。

```powershell
pwsh -File scripts/Run-CampaignProgress.ps1 -RegionRun -StopBeforeRing 1 -Seed 424242 -NativeWorker 3 -TimeoutSeconds 240 -NewRunFromProfile .probe-runs/campaign-ring6-20261122-133710-1e721a5d
```

2.3.1 原版（模块 `bf93f060-beda-46b1-b808-95ebf1e9a99d`）保存了 `SoulSavior_R0_Battle_Morsels` 的 `BattleIntro`。
检查点位于 `.probe-runs/campaign-region-battle1-20261124-062358-90ad43e9-before-ring1`，
存档记录种子 `424242`、地图距离 0、完成战斗 0 场及 `wasDevCheatUsed=false`。
检查点存档 SHA-256 为 `c7ecf40d9fdd905d9605d2fb7e243b2eb23646d9642db9bffedc7369b0c5d7cc`，与 `checkpoint` 审计一致。
从该检查点续接原版战役，在 32 次出牌和 6 次结束回合后赢得序章，火车核心仍为 50 生命。
原版区域选择界面选中了默认区域 0，并保存了该区域的首场 `SoulSavior_R1_Battle_HealOnShiftHeavy`。

```powershell
pwsh -File scripts/Run-CampaignProgress.ps1 -RegionRun -StopBeforeRing 2 -NativeWorker 3 -TimeoutSeconds 240 -InputProfile .probe-runs/campaign-region-battle1-20261124-062358-90ad43e9-before-ring1
```

续接检查点位于 `.probe-runs/campaign-region-battle2-20261124-063542-9f9db4ca-before-ring2`，
为种子 `424242`、地图距离 1、完成战斗 1 场及 `wasDevCheatUsed=false` 的 `BattleIntro`。
其存档 SHA-256 为 `e6c19df2cb24d0cea1f303d7f4e9b442cc4d4b986e92ced092328762430926f0`。
从该存档续接，36 次出牌、4 次主动技能及 7 次结束回合后赢得区域首战，火车核心仍为 50 生命。
原版随后进入该区域首领 `SoulSavior_R1_BossBattle_DutifulChild_Heal` 的 `BattleIntro`，原版目录中的车外首领为 `Boss_SoulSavior_R1_Dutiful_Armor`。

```powershell
pwsh -File scripts/Run-CampaignProgress.ps1 -RegionRun -StopBeforeRing 3 -NativeWorker 3 -TimeoutSeconds 300 -InputProfile .probe-runs/campaign-region-battle2-20261124-063542-9f9db4ca-before-ring2
```

首领检查点位于 `.probe-runs/campaign-region-battle3-20261124-064242-7c3bd82f-before-ring3`。
存档记录种子 `424242`、地图距离 2、两场胜利及 `wasDevCheatUsed=false`，SHA-256 为 `9ba5418fe8be8c0a127991bd917c5478bd5d48eeb0cd9d02786ed6a984994bac`。
验收核对了输入和原版存档哈希。

下列命令从首领检查点续接原版战役。原版接受了 35 次出牌、4 次主动技能和 7 次结束回合，并以火车核心 32 生命赢得首领战。奖励结算后，火车核心恢复到 47 生命。

```powershell
pwsh -File scripts/Run-CampaignProgress.ps1 -RegionRun -StopBeforeRing 4 -NativeWorker 3 -TimeoutSeconds 360 -InputProfile .probe-runs/campaign-region-battle3-20261124-064242-7c3bd82f-before-ring3
```

随后原版选择区域 1，保存 `SoulSavior_R2_Battle_TroopBuffFeed` 的战前状态。新存档位于 `.probe-runs/campaign-region-battle4-20261124-064646-0aee0a1e-before-ring4`，SHA-256 为 `f389ae0511b24a64043890bb57f68ad8e53be7d9bf0231882e8dfb887a125981`。
存档记录种子 `424242`、地图距离 3、三场胜利及 `wasDevCheatUsed=false`。验收核对了输入和原版存档哈希。

同一首领战检查点通过两条顾问逐步对照。第一条原版落败记录核对了 34 次出牌或技能、
7 次结束回合及结束回合候选。第二条沿用原版战役测试策略，薪火 32 生命获胜，
39 次出牌或技能、7 次结束回合和两个阶段的候选检查均通过。连续回放根据原版快照
复放楼层切换触发的预览，并比较完整后继状态、随机流与预览副本编号。
[首领败局](../tests/compatibility/game-2.3.1/mechanics/dutiful-armor/natural-region-run-defeat.mt2f) ·
[首领胜局](../tests/compatibility/game-2.3.1/mechanics/dutiful-armor/natural-region-run-win.mt2f)
