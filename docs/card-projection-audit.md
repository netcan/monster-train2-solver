# 原版目录定义投影复核

2026-11-15 补齐 `ZealotsHalo` 后再次使用未修改原版定义的目录重扫：
724 个唯一定义、614 张图鉴卡，图鉴投影异常和显式
`ExternalInteractions` 保护均为零，决策／RNG／帧号不变。
新快照在 `tests/compatibility/game-2.3.1/mechanics/halo/catalog.mt2f`，
配套独立检查为 `scripts/Check-Game231Halo.ps1`，见
[装备原生证据及覆盖边界](zealots-halo.md)。这仍只是定义投影，完整卡牌
组合及 614／236 整体复验没有完成。

2026-11-21 增加新版出牌证据清单。`scripts/Get-CardCoverageInventory.ps1 -Current`
按 SHA-256 校验并读取 178 个登记归档，纳入 115 场原版 2.3.1 已结算且四项计数为零的战斗，
其中 111 场胜局，包含固定阶段验收的八场。614 张图鉴卡中有 601 张可直接出牌。
现有归档为 46 张提供了定义吻合的出牌证据，为另 21 张记录了出牌身份，
其余 534 张尚无符合条件的出牌记录。清单按卡牌 ID 列出证据归档及动作索引，
输出至 `results/card-evidence-2.3.1.tsv`。运行时升级与受控卡牌可记录身份，
须匹配目录投影、采集到的原始参数及击杀触发器才计入定义吻合。旧版可单独使用
不带 `-Current` 的历史清单命令。下文为补齐前的目录对照记录。

2026-11-21 三份优先卡牌归档在同一原版首战检查点分别验证三张单位、两张手牌升级法术、
两张带触发的单位。Silver Ink 和 Magic Power Hand 取得定义吻合证据，五张单位取得出牌
身份和完整原版后继。单位使用临时冻结控制，法术的手牌目标由测试场景布置。合计 17 次
出牌、15 次 EndTurn，三场各 16 条分支、全部录制候选、串并行搜索及计划复放通过。
档案为 `tests/compatibility/game-2.3.1/mechanics/priority-card-batch/`。

2.3.1 的旧目录省略了全局技能替换标记及普通单位的默认物理站位字段。按同一 MVID
对账后，15 张已有原版出牌记录的单位得到定义吻合证据。该核对只接受当前版本的全局
标记和精确的五个站位默认值，实际牌效、特性及站位变体继续参与比较。2.2.1 清单
通过既有复核。以两版原版目录的新增身份或 Effects／Traits／Triggers／Rule 差异为优先
范围。Railgun 和 PunishedChamp2 在另一原版胜局取得基础召唤证据，覆盖 5 次出牌、
5 次 EndTurn、16 条分支、录制候选和串并行计划。现有 52 张优先卡中有 38 张定义
吻合、13 张仅有身份，Heavenfall 还缺当前版已通过的出牌记录。

2026-11-15 在 `40bd1378` 模型上重扫本机 2.3.1、build 26650011：
724 个唯一原版定义，614 张图鉴卡。图鉴卡没有投影异常，仍有显式
`ExternalInteractions` 保护的只有 `ZealotsHalo`
（`cbfec4a4-00fe-406e-b52a-e35e6456ba7d`）：`Upgrade room modifiers`。
613 张没有该保护不等于 613 张通过所有出牌、悬停、升级或组合验收。

新增只读 `EquipmentRoomModifierDefinitions`，补充此前缺失的装备升级
原始房间修饰器。本次只观察到一条：`ZealotsHalo_Upgrade`
（`d66f5d82-9174-4212-b85c-e89b56f81be7`）含
`RoomStateSelfDamagePerMissingHp`，Int=1、Int2=1，无状态或嵌套效果，
不禁止部署阶段。该信息与模型投影分开保存，不因模型未实现而丢失原定义。

采集决策、随机状态与帧号均未改变。配套七回合 no-cards 原生胜局约
45.64 秒，采集失败／差异／未支持／待结算四项零，玩家文件未变；独立
52 房间／14 列车阶段复放通过。原生源为
`.probe-runs/full-battle-no-cards-20261115-114029-53a0b700`。

二进制目录归档为
`tests/compatibility/game-2.3.1/projection-audits/before-halo/catalog.mt2f`，
119,786 字节，SHA256
`9b8f000f401fe3afbe47f952398d3c912e8cabf12747e6e1725839c14a958de4`。
保留独立 TSV 清单，无 JSON 依赖。此处记录的是补齐该装备前的定义投影
快照；新版全部 614 张卡／236 件遗物的整体复验及完整战斗模拟仍未完成。

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -CardCatalog -NativeWorker 3 -BinaryCapture -TimeoutSeconds 180
```
