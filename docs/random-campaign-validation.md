# 随机组合整局验收

`scripts/Run-RandomCampaign.ps1` 从原生第一战开始，执行实际玩家建议，
每场获胜后在独立游戏进程冻结复放全部输入，再经原生奖励、商店和地图进入下一战。
只有游戏自身 EndGame 给出八战全胜的 `Standard` 结算、完整档案打包和恢复校验
通过，才记为整局通关。模型预测胜利和单场获胜均不算整局通关。

使用发布版默认配置：整场搜索、两个后台线程、3000ms、256 步上限、上层优先，
额外战术种子关闭。3000ms 是搜索软预算；每场原生进程另有超时。
你的 Windows 配置目前为 10000ms，本验收不修改该配置。

```powershell
pwsh -NoProfile -File scripts/Run-RandomCampaign.ps1 `
  -PrimaryClan Banished -SecondaryClan Umbra -Seed 1970936330 -NativeWorker 2
```

开始前顺序构建 Probe、Advisor、ModelChecks；已经构建时可加 `-SkipBuild`。
指定 `-InputProfile .probe-runs/<checkpoint>` 可继续同一种子、难度和组合的
未完成隔离检查点。失败记录和各战接受记录写入独立的
`.probe-runs/random-campaign-*/campaign-result.json` 与 `accepted-rings.jsonl`。
失败保留具体战次和输入，不将后续战次记为通过。胜局的模型检查还要求源局和复放
完整初始局面、每步后继、预测及 RNG 相同；复放必须零搜索。

采样范围为已支持的十一种基础／旧版氏族，排除需要单独 DLC 可用性检查的 Railforged。
只在隔离档案解锁被抽中的氏族及旧族所需 Crew，原生起始牌组、冠军、遗物、
首领生成和后续奖励均不改写。因此这是这些氏族的隔离自然普通通关验收，
不代表档案自然解锁，也不代表真终局或高 Covenant 保证获胜。

2026-10-25 首批用 `.NET Random(20261025)` 依序抽取主族、不同副族、游戏种子，
按下列顺序运行 Covenant0。氏族池顺序为 Banished、Pyreborne、LunaCoven、
Underlegion、LazarusLeague、Hellhorned、Awoken、Stygian、Umbra、Remnant、Wurmkin。

| 样本 | 主族／副族 | 原生种子 | 本次状态 |
| --- | --- | ---: | --- |
| 1 | Banished / Umbra | 1970936330 | 前六战源局及独立复放通过，第七战实际败北；37个输入完整验证 |
| 2 | Pyreborne / Banished | 1862997127 | 第三战实际败北；38个输入完整验证，采集／模型失败计数均为零 |

两组首战各自的源局／独立复放已归档在 `tests/fixtures/campaign/random/`，
以 `full-battle-random-campaign-01-ring1-*` 与 `02-ring1-*` 命名。
定向选择任意一侧归档都会校验整对，重复选择不会重复执行。
首批同时确认错误种子、难度和交换的主／副族均被拒绝；未运行全量回归。

第二组第二战使用正常商店购买的 `TraitPermafrost`
(`e0af4fe5-75c0-4017-ae5e-5a87099aa6c5`) 后，旧采集仅接受遗物版本，
因此搜索立即拒绝所有路线并显示 `Upgrade traits`。修复只接受这个原生商店
升级的 ID、资源名和相应 Permafrost 特性；模型原有冻结逻辑不变。
新源局与独立原生复放均获胜，16 输入／7 回合结束、薪火33，四项失败计数为零。
修复前原生部署观察也已归档，专门检查证明初始完整状态和 RNG 只有该保护标记不同；
新源局有4次冻结手牌观察和1次实际打出／解冻。玩家路线没有跨回合保留该牌，
保留及再次抽牌的通用机制仍由既有原生 Permafrost 专项验证，未虚报为本路线操作。

第一组第四战出现跨楼层触发时，旧采集只因附魔效果而保留完整角色世界，
漏掉了 `Tower` / `FrontInAllRooms` 的需求。现在按敌方未来波次及已知玩家
角色的原生触发目标开启采集；第四战源局和独立回放均胜，22输入、薪火69，
完整后继、预测与 RNG 相同。第二至四战的源局／回放也已持久归档。

第二组第三战的盒子生成牌绕过 `AddCardImpl`，连续插入手牌前端使可见顺序
与创建顺序相反。新增共享的缓存牌入手前身份观察，并在 Probe 和 Advisor
两边安装，替代只在受控盒子场景启用的旧钩子。
此后暴露冠军跨战永久成长的重复记录问题：本场新增的 `Plus1AttackScalable`
与前两战同 ID 记录共存，原生缩放实际修改本场最后一条，模型此前修改第一条。
修复限定到这个已观测的原生升级 ID 和资源名，其他升级选择不变。
`player-random-campaign-02-ring3-upgrade-observed-prefix.mt2f` 保留失败原件，
专门检查重新执行12个实际输入的完整后继和 RNG，并验证成长序列
`[1,1,1] -> [1,1,2]`。该原件没有完整战斗结算，不能算胜局。

再推进到第18个输入，开盒首次复制到商店 Permafrost 牌，暴露复制永久升级时
遗漏 `SetupTraitUpgrades -> OnTraitAdded` 的回调。永久特性安装现在也处理
Permafrost，并立即、幂等添加独立临时 Freeze；不改变临时升级的安装规则。
`player-random-campaign-02-ring3-permafrost-copy-observed-prefix.mt2f` 保留原生失败件，
定向检查重放18个后继及 RNG，并确认新牌同时有一个永久 Permafrost 和一个临时 Freeze。
既有原生 Permafrost 单卡和盒子复制／并行不可变分支专项也通过。

整局执行器的失败收据现在在原生进程开始时即记录实际源档案路径；
即使战斗异常退出，也不会误指向上一战，并再次校验败局输入保存文件未变。

第一组在原生商店买到 `TraitJuice`
(`0fc9f6e7-df71-4045-9867-67453769adf8`) 后，第七战原先也因 `Upgrade traits`
立即不可计算。现只开放已观测的原始 Lifesteal/Scorch 法术和该商店升级签名，
其他卡牌或混合缩放顺序继续保留保护。六组隔离原生回调涵盖负数、零、正数和
整数边界，并对实际持有升级的卡牌验证12次模型状态叠加；这条玩家路线未打出
升级后的牌，故没有冒称其实际施法覆盖。修复后第七战完成自然败局，30次出牌、
7次结束回合、39次发布，薪火38降至0，四项失败计数为零；已归档为
`full-battle-random-campaign-01-ring7-loss.mt2f`。

首批两个随机样本在固定默认配置下均未通关。这证明当前策略质量仍不足，
不证明组合或种子无解。整局中的奖励、商店选择也由原生驱动的合法选择策略执行，
本验收并没有实现跨整局的构筑优化。

第二组第三战另以固定楼层策略比较：下层与中层模型判败，上层预测48步获胜、
薪火5；原生复放在第44步结束回合发现房间引用缓存偏差，不能接受为胜局。
原生失败件位于本地 `.probe-runs/full-battle-units-spells-and-junk-20261025-030940-cbd93f3c/`，
其 `full-battle.mt2f` SHA-256 为
`83b1fa8dd098df0d5c4ce2b834f7eeaad4660b83362444405906c2740e101f87`。
该步实际记录冠军的房间角色为 `[2,8,24]`，模型为 `[2,24]`；首领8此时进入上层。
这条未接受路线属于后续缓存／触发时序调查，不计为默认胜利或完整原生胜利。

后续隔离原生观察已定位并修复该偏差：`RoomManager.RoomIsInRelentlessPhase`
在 `MonsterTurn` 阶段固定返回 false，UI 战斗预览也沿用这一阶段。
此前模型只依据敌方无情状态而禁止递减冷却，漏掉预览中的技能就绪回调，
使冠军源牌少缓存升上来的首领。模型现在同时依据已捕获的战斗阶段。
`player-random-campaign-02-ring3-relentless-preview-observed.mt2f` 保留新的原生观察，
包含44个实际输入及直接缓存写入；完整后继、RNG、16次整车阶段和两个阶段对照
全部通过，原始父局面不变。该档案仍是未结算的失败原件，不冒充新的胜局。

第二组的第三战修复后完成原生结算，29次出牌、9次结束回合、40次建议发布，
薪火57降至0；源局完整后继、预测、RNG 和实际面板均通过，
已归档为 `full-battle-random-campaign-02-ring3-loss.mt2f`。
这是默认策略的真实失败样本，不能据此宣称组合不可通关或该局无解；
也未将完整败局计作独立原生双胜验收。

Windows0.2.26 的最终玩家载荷另复验第一组第一战：实际建议与独立冻结回放均获胜，
9个输入、3次出牌、6次结束回合，源局11次发布；完整状态、预测及 RNG 一致，
四项失败计数为零，原始玩家文件未变。两个原件以
`full-battle-random-campaign-01-ring1-release026-search.mt2f` 与
`full-battle-random-campaign-01-ring1-release026-replay.mt2f` 归档。
这只是最终构建的单战双进程复验，不改变本批两个整局都失败的结论。

定向检查入口：

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --random-campaign-pair-only <search.mt2f> <replay.mt2f> <seed> <rank> <distance> <primary> <secondary>
```

完整败局使用 `--random-campaign-source-only <source.mt2f> <seed> <rank> <distance>
<primary> <secondary>`，仍检查原生逐步状态、预测、RNG 与面板证据。
机制不支持或不完整档案不能通过此入口冒充一个已验证的策略败局。
