# 存活受击友军下的普通火炉终局反击

`true-final-boss-triggered-ordinary-pyre.mt2f` 来自
`full-battle-units-and-junk-20261023-044611-0d8ff370`。
明确跳图/解锁原始真终局，向部署手牌加入原始 Avenger；实际支付两能量
召唤 20/40、容量二的单位。第一次正常 EndTurn 完成原始出生与抽牌，再
按记录对三个生命所有者减去 1554/2749/3499，使五主体均一血。未添加
命令或施放法术，未更改敌军阶段、原始卡牌/单位定义或原始火炉生命。

第二次正常 EndTurn 中，休眠前排阻止底层友军出手；三层 Dominion 先
攻击火炉至 77 血，火炉正常反击杀死 Dominion，完成五主体清理及终局
效果。Avenger 原始受击回调保留，20/40 属性不变，本次没有实际受击。
这关闭的是存活回调友军下的物理火炉终局反击，不是底层友军普通攻击
或自然真终局胜局。

首次诊断原生获胜，但旧模型记录一项差异、五项未支持，未登记为验收。
修正仅向三层物理火炉的普通房间/单位回合传递终局来源，包括内部致死
预览；跨层火炉仍使用独立命令校准。原始友军完整签名和五主体一血检查
继续生效，默认终局 API、匿名伤害、其他楼层和变化的受击回调仍拒绝。

新原生进程 36.44 秒完成，正常退出、原文件未变、终局收敛、`DEPTH-PASS`，
采集失败/差异/未支持/待处理均零。三个实际单位回合、付费部署、出生、
两个完整 EndTurn、最终状态/RNG 与 16 条并发独立不可变胜利链匹配。
已定向复核鹰心命令及 Dominion 普通法术的原有反例；未运行全量回归。

采集入口为 `Run-FullBattleProbe.ps1 -OriginalTrueFinalBoss -BattleDistance 8
-TrueFinalBossLethalCombat -TrueFinalBossOrdinaryAlly Avenger`；验收入口为
`Check-TrueFinalBoss.ps1 -Case OrdinaryAlly -SkipBuild`。
