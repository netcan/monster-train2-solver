# 供奉宝藏的单位与法术间接施放

2.3.1 的 `MonolithHelper.Devote` 先随机选择可弃牌、扣 1 能量、增加 1 点
虔诚，再弃置所选牌。宝藏单位的登场效果因此读取增加后的虔诚等级。
本项保留原版 `DarkInitiate` 与 `GuardianAmulet` 的定义及宝藏特性，补齐
供奉到整列车间接施放的回调；复用现有单位出生、共享触发队列、Rally、
来源卡升级及待命区绑定，没有将免费施放伪装为普通付费出牌。

## 原生规则与模型

`CardTraitTreasure.WillPlayCardOnDiscard` 支持法术和单位；禁用楼层不自动
施放，单位也不能自动施放到薪火层。允许尝试时，原生先把弃置标记为
`wasPlayed` 并增加 `PlayCount`，然后调用 `PlayAnyCard(ignoreCost:true,
discardCard:false,fromDirectPlay:false)`。成功施放另增加 `TimesPlayed`。
因此本地打出次数、实际施放统计及弃牌统计不能互相替代。

`CardEffectSpawnMonster` 没有拖放站位时调用
`MonsterManager.GetCanSpawnMonster(...,dropLocation:null)`，只检查第一个
空物理站位；它不按普通拖放出牌的楼层容量或召唤禁止条件拒绝自动召唤。
模型保留这一差异。成功召唤后，原生把来源卡从弃牌缓冲移到待命区，绑定
具体单位的存活条件。临时冻结从实际 `OnCardPlayed` 移除；拒绝施放不移除。

## 隔离整局验收

worker 3、隐藏窗口、静音、Instant。三张控制牌部署强化 Steward、补充能量、
把虔诚推进到首级门槛前一点；后续五次供奉只在四张原版 `DarkInitiate` 和
一张原版 `GuardianAmulet` 中随机选择。冻结保留后续输入；普通后续抽牌
使用 Persistent 控制牌，不能进入供奉池。

| 原生输入 | 观测结果 |
| --- | --- |
| 首次供奉跨越 0→1 级 | 先升虔诚，再触发宝藏；单位登场为 30 攻／30 血 |
| 三张 DarkInitiate 自动施放 | 各有一条原版 +15／+5 升级，来源卡临时升级与具体待命宿主一致 |
| 第三张单位进入已有满容量楼层 | 容量使用从 5 增至 7，仍成功出生，占用独立空站位 |
| GuardianAmulet 自动施放 | 费用 3 的原版法术免费施放，同层敌方获得 2 层 Sap；只支付供奉的 1 能量 |
| 最后一张单位在薪火层被供奉 | 正常增加虔诚并弃牌，不出生、不增加实际施放或本地打出计数，临时冻结保留 |

五次选择全部匹配原生手牌顺序和 Battle RNG，包括最后一个候选；每次只扣
1 能量。完整胜局为 8 个输入（3 次出牌、5 次供奉）、5 次 EndTurn、32 个
房间阶段、9 个列车阶段。四项失败／差异／未支持／待完成计数为零，终局
效果已结算。最终原生采样 52.23 秒，启动器确认玩家文件未变。

连续整局复放、16 路不可变分支、八个原生决策的完整后继／RNG 对比通过。
27 个根候选、128 状态串并行搜索及三步计划复放通过，录制决策的候选和
搜索都没有未支持尝试。上一个[虔诚升级场景](devotion-unit-upgrade.md)中
原先被拒绝的四个宝藏供奉候选现也已通过，七个决策均无未支持候选。

二进制归档为
`tests/compatibility/game-2.3.1/mechanics/devote-treasure/battle.mt2f`，
34,161 字节，SHA256
`e443fcc7a16fbb575c45e526264600031ee10be8963ea6c7eb3731f51d3cf124`。
没有 JSON 归档依赖。独立验收：

```powershell
pwsh -NoProfile -File scripts/Check-Game231DevoteTreasure.ps1
```

普通供奉 14 次操作／7 次 EndTurn／16 路分支、旧 Deep Offering 的 8 个输入／
35 房间／9 列车阶段，以及原版虔诚升级整局／16 分支／128 状态搜索通过。
Probe、ModelChecks、Advisor Release 构建通过；没有运行全量回归。

## 仍待完成

物理站位全满时的完整弃置后继、禁用楼层及召唤禁止条件的对照、更多单位／
随机与复数召唤、装备与出生中死亡／终局、悬停副本，以及改变焦点或世界的
弃牌回调组合仍需原生验收。带有额外遗物／卡牌回调的间接施放与复杂出生
保留既有明确保护。614 张卡／236 件遗物整体复验及完整模拟未完成；新增
遗物整局验收仍为 12/12。
