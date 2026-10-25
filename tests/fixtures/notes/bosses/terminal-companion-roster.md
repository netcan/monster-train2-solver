# 终局保留对象的伴生首领配对

自然Covenant3、Banished/Pyreborne、种子424242的第六战，35输入离线改进路线
在原生获胜、薪火30，但0.2.107的最后一次结束回合留下两处阶段偏差。
两个AngelDuo先后死于薪火层；已死亡Savage仍保留Entropic的配对ID，终局标记也未更新。

`player-terminal-companion-roster-counterexample.mt2f`保留原始失败计数和旧预测，
来源`full-battle-units-spells-and-junk-20261026-062158-6a9b2faa`，
SHA256 `3c88e5671d32aed84a5a4eb79d3f3798bd69522b41512e4ad7ccd3355165ae61`。
原生`FullBattleTrace`通过`HeroManager.FindPairedCompanionBoss`查询当前存活配对，
死亡对象也动态捕获该结果。最终Savage 6的`PairedBossUnitId`应从9变0，
`EndsBattleOnDeath`应从false变true，其他字段不变。

0.2.108在共享世界同步时，对保留对象应用现有存活配对清理。定向检查验证原始
35个后继、91房间阶段、22列车阶段的完整状态与RNG，以及八个不变父状态的并行
胜局分支；逐字段证明旧预测只差上述两项。该历史反例仍不是零偏差胜局验收，
需要另取新进程源局及独立重放，且离线路线不计默认预算胜局。
