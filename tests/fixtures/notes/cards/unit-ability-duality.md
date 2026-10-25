# 单位技能的双重状态翻倍

自然Covenant3、Banished/Pyreborne、种子424242，第七战第31输入由单位17
SneezyWhelp施放技能38，向单位20附加薪火胶。施法者具有Duality，原生增加24层，
旧模型增加12层。伤害1、冷却、其他单位及RNG一致。

`player-unit-ability-duality-counterexample.mt2f`保留原始未完成战斗和一次偏差，
SHA256 `03409510d80946c3a0c0be42d63bd7d174b6d0ef064b1619f5b47c1366fbb610`。
定向检查核对31个真实操作后继、8个列车阶段、完整状态／RNG，并逐字段证明
旧预测只差该单位的活动／注册状态及共享世界副本，共四个层数值。

原生`CardEffectAddStatusEffect.ApplyEffect`将`selfTarget`传给`AddStatusEffect`，
后者按施法者Duality翻倍正面／负面状态和护甲；`ParamBool2`可禁用该翻倍。
0.2.110采集此参数并在普通技能AddStatus路径应用现有状态翻倍逻辑，保留
范围、装备合并和升级重建中的该参数。普通无施法单位卡牌、状态乘法及
SpawnOrStatus不套用这条翻倍；缺少显示类别的状态仍返回明确未覆盖原因。

八个并行不变父状态分支及IgnoreDualism控制通过。提交1a087498后的干净构建
原生源局`20261026-071456-13ac2b8a`和独立重放`20261026-071652-4f9e98f2`
均完成31输入，完整前缀、状态、预测和RNG一致，正常游戏文件不变。
两份校准注册为`player-unit-ability-duality-{calibration,replay-calibration}.mt2f`。
这是技能组件验收，不计第七战胜利或整局通关；同一自然第七战继续测试默认建议。
