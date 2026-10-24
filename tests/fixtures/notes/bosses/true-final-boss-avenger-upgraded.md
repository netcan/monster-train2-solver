# 原始受击友军的永久数值强化

三个 `true-final-boss-ordinary-ally-avenger-{health,damage,both}.mt2f` 分别
验证原始 Avenger 的生命强化、攻击强化与两者组合。明确跳图/解锁原始
真终局并添加单位卡，在部署前通过原生 ApplyPermanentUpgrade 应用原始
`BlessingBonusHealth`（生命 +20）及 `EnhanceAttackOnConsume`（攻击 +10）。
敌军生命、阶段、原始卡牌/单位定义、能量及火炉 80 血不变。

实际召唤前排原始小乘务员与后排 Avenger，后者分别为 20/60、30/40 和
30/60，容量均二，完整原始 OnHit→LastAttackerCharacter→一层近战脆弱
保持。原生链接攻击者实际命中该友军并获得近战脆弱；相应完整单位回合、
两个付费召唤、六次 EndTurn、五次 UI 战斗预览与每场 16 条独立不可变
整场链的所有状态/上下文/RNG 一致。三个进程均败北，四项计数全零、
终局收敛、正常退出且原文件不变，不计自然胜局。

生产范围依据完整原始召唤元数据与至多两个纯正数永久升级的来源和内容，
不按单位名称或最终面板数值放行。丢失升级来源、升级中的卡牌回调、临时
升级、未知升级、缺少原始生命元数据，以及原有十种受击签名变化继续拒绝。
强化受击友军的终局来源和与勇气的组合仍单独保护；本批不会继承未强化
友军的付费法术、命令或普通火炉终局验收。

Health 来源 `full-battle-units-and-junk-20261023-045030-0a7a93b3`，
Damage 来源 `full-battle-units-and-junk-20261023-045030-c9e8a8df`；
Both 来源 `full-battle-units-and-junk-20261023-045223-2636c61b`。
使用 `-TrueFinalBossOrdinaryAlly Avenger -TrueFinalBossOrdinaryAllyUpgrade
Health|Damage|Both` 采集；`Check-TrueFinalBoss.ps1 -Case OrdinaryAlly`
登记三项。双强化另外完成真实玩家建议及新进程九输入冻结重放，实际
部署 30/60 的单位并有三次受击见证，见 [玩家配对验收](player-true-final-boss-avenger-upgraded.md)。
未运行全量回归或构建 Mac 包。
