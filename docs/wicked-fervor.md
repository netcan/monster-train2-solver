# 原版 Wicked Fervor · 2026-11-15

游戏 2.3.1 的 `CardEffectDiscardHandAddStatusEffectPerCard` 先固定手牌中的
弃牌名单，排除施放牌、Infusion 和 Persistent；Freeze 不阻止这次弃牌。
它按手牌顺序逐张调用正常弃牌流程，保留回调，再按最初名单数量乘以状态
层数。零张时直接结束，不施加零层状态。预演计算数量但不实际弃牌，
原生重施副本另外保留首次施放的弃牌数量，不能简单重新统计手牌。

新模型复用既有保护牌筛选、弃牌及状态流程。原版 `WickedFervor`
（`2107d396-9427-4bfd-9889-eef36bd5c7b0`）保持定义不变：费用 1、单个我方
目标、每张弃牌施加 5 层狂热。隔离场景放置两名控制单位，第一次施放将
三张冻结的普通牌依次弃掉，获得 15 层狂热，增加攻击／最大生命但不治疗。
第二次没有有效弃牌，完整目标状态不变。另两张陪测牌分别带 Persistent 和
Infusion，始终保留且不计数。第二张原版牌临时添加 Persistent，防止第一次
弃牌将其移除；原版 CardData、费用及效果均未改动。

适配器同时识别临时 Persistent：原生类只有“不可弃牌”的标记行为，无额外
参数或回调。第一次探针被旧适配器的临时特性保护阻断，未计作通过；适配后
重新运行得到原生胜局，未修改或替换失败记录。

原生 Instant 胜局耗时 46.74 秒，4 次出牌、5 次 EndTurn、52 个房间阶段、
9 个列车阶段，采集失败／差异／不支持／待结算全部为零，终局已结算，
启动器确认玩家文件未变。独立检查保留完整卡牌、牌堆、统计、状态和 RNG，
完整连续复放及 16 路不可变分支通过。搜索匹配四个录制决策，根有六个动作，
128/128 状态串／并行最优相同，三步计划复放通过，两个不支持计数均为零。

最终 Probe、ModelChecks、Advisor Release 构建通过。复用保护牌筛选的原版
Devote 整局回归通过（14 次 Devote、15 次出牌、7 次 EndTurn），旧原版弃牌／
抽牌归档的 11 次出牌、5 次 EndTurn 及 16 路连续分支也通过，未运行全量归档集。

归档 `tests/compatibility/game-2.3.1/mechanics/wicked-fervor/battle.mt2f`
共 35,562 字节，SHA256 为
`2aea75a275591200d51a4b9bd85578d004f04d1d16955f6ecac5b394cc9442dd`，
没有 JSON 依赖。独立入口验证 manifest 大小／摘要及全部场景语义：

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -OriginalWickedFervor -NativeWorker 3 -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Game231WickedFervor.ps1
```

这是原版卡的正向／零弃牌整局证据。完整悬停、原生重施共享计数、随机状态
池、状态倍率／额外升级以及会改变角色世界的弃牌回调组合仍需独立原生验收；
当前重施遗物按其既有卡牌边界处理。本项不代表新版全部 614 张卡／236 个
遗物完成重验，新增遗物整局仍为 12/12。
