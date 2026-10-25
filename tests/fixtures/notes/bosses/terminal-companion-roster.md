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

后续0.2.108新源局`20261026-063547-9ae639e2`和独立重放
`campaign-ring7-20261026-063927-1da8117e`均35输入／薪火30原生获胜。
24次出牌、11次结束回合，完整后继／预测／RNG及原生累计Covenant01／02／03
一致，四项失败计数全零。两个新档案分别注册为
`full-battle-native-covenant-03-ring6-refined-{search,replay}.mt2f`，
SHA256分别为`8a78d6a86a4d22c664d31b82f12ec0cb29c60f4df4b22ccd6fa5aa65b4a88ab5`
及`e54722d195de1eda1c43a42518f7543b99c301643a182caf4c8251b0028daa3c`。
同一冻结模型路线与两份原生记录分别绑定通过。正常奖励进入第七战，存档SHA256
`e98ddaefff830752a78a2fcd977fbc6e7a9c49404060b2d20339c2a94c4b72b5`；
28文件检查点打包后恢复验证27项载荷。
这是自然第六战的离线改进双胜，不计实际默认预算胜局或整局通关。
