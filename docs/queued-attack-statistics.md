# 排队攻击回调的伤害统计 · 2026-11-15

游戏 2.3.1 的薪火攻击在运行中的触发队列内结算。伤害先把
`CardStatistics.lastAttackDamageDealt` 写为 45，荆棘反伤再写为 2；
随后执行的 `OnAttackingBeforeDamage`、`OnAttacking` 回调根据各自
`FireTriggersData.attackDamageDealt` 恢复为 45。普通单位直接攻击时，
攻击回调在反伤前运行，最终统计仍为 2。

模型现在将命中时确定的伤害值随攻击回调保存，保留本地队列、跨房间
派发、回调快照及回调复制中的值，在真正执行回调时恢复共享统计。
该值包含被护甲抵挡的伤害；2.3.1 另包含 LifeLink 防止的伤害，
保留旧版的计算方式。反伤本身仍更新统计。

原生证据取自新版隔离胜局的十个完整房间阶段：八个薪火排队阶段、
两个普通单位反伤对照，含整个房间战斗和逐次交换。源胜局的采集失败、
差异、不支持、待结算四项全部为零；二进制保留源归档 SHA256。
原生统计写入顺序另经临时 Harmony 诊断确认，诊断补丁已移除。

`tests/compatibility/game-2.3.1/mechanics/queued-attack-statistics/native-stages.mt2f`
共 11,661 字节，SHA256 为
`8acc93d8d1edb507366b5b3d817374a33477f568efde63364ae613529415c2dc`。
没有 JSON 依赖。运行 `scripts/Check-Game231QueuedAttackStatistics.ps1`
可校验归档大小和哈希，以及十个原生完整单位／上下文后继与每项 32 路
不可变并发分支。此项是共享战斗统计规则的修复，不增加原版目录覆盖数。

受影响的旧版攻击触发、荆棘首领及整层荆棘回调三份整局归档定向复验，
157 个房间阶段及 37 个列车阶段全部匹配，零不支持；未跑全量回归。
