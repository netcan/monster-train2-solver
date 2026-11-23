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
验收核对了输入和原版存档哈希。区域首领尚待实机验证。
