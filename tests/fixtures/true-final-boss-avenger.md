# 真终局原始 Avenger 受击回调

`true-final-boss-ordinary-ally-avenger.mt2f` 来自
`full-battle-units-and-junk-20261023-035802-acc6a4d3`。使用明确跳图/解锁及
添加原始 Avenger（`69fcbe85-a5b7-4a83-bf6d-21888604d99e`）的控制，原始
场景、五主体生命/阶段、单位定义和火炉 80 血未变；没有升级、装备或嫁接。
实际支付召唤前排小乘务员和后排 Avenger，后者保留 20/40、容量二和原始
`OnHit → LastAttackerCharacter → melee weakness 1`。普通攻击造成 20 伤害，
链接攻击者命中后实际获得一层近战脆弱。

原生正常退出、原文件未变、`DEPTH-PASS`，采集失败、模型差异、未支持及
待完成四项计数均为零。两个付费召唤、六次 EndTurn、五次原生 UI 战斗预览
以及 16 条独立不可变整场链的完整状态、上下文和 RNG 一致。另直接重算
原生链接攻击者触发受击回调的完整单位回合。

生产保护依据原始卡牌来源和完整触发签名，不以 Avenger 名称放行。十个
签名反例检查 Once、触发类型、次数、无视沉默、次数修正、装备来源、目标、
状态数量、法强缩放及额外效果；五层勇气与该触发器的组合仍拒绝。把五主体
改为一血的反例仍满足普通战斗结构，但单位来源终局组死亡保护明确拒绝带回调友军。
尚未校准的升级、技能、装备和其他回调继续受保护。

入口：`./scripts/Check-TrueFinalBoss.ps1 -Case OrdinaryAlly -SkipBuild`。
原生采集使用 `-OriginalTrueFinalBoss -BattleDistance 8 -TrueFinalBossOrdinaryAlly Avenger`，
并启用分支卡牌掩码、物理站位和死亡/卡牌动画收敛。
该受控路线败北，只证明上述完整战斗机制。真实玩家建议及新进程九输入重放
随后也通过，实际召唤 Avenger 并有三次受击回调见证，见
[玩家配对证据](player-true-final-boss-avenger.md)。任意回调致死签名和自然
真终局胜利仍未验收。具有完整来源的付费普通法术随后另完成五主体临界胜局
校准，详见 [存活 Avenger 的终局法术](true-final-boss-avenger-terminal.md)；该结果
不开放单位来源终局攻击。未运行全量回归或构建 Mac 包。
