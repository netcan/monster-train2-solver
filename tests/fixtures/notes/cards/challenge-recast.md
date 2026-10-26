# Challenge 腐化／消耗遗物副本

第十三组第七战第五回合，Challenge15带TraitCorruptwIcon和ReduceCostAddConsume，
消耗降费Cost-2／XCost+2，其余数值为0。0.2.135只接受该精确身份、两项升级和
腐化／消耗特性组合。原卡对前排Avenger3施放，腐化0→1、PlayCount加一次，
原卡消耗一次，NextCard不变，遗物计数0→1。

原生私有副本将移位效果替换为空效果，保留最后目标供随后的状态使用。
3／2／8三个友方单位各获得近战易伤1，站位保持3／2／8，生命、攻击和护甲
均不因副本变化。副本不再增加腐化，也不再次消耗原牌或登记出牌。

原始拒绝与修复后独立原生进程各28输入完整前缀／后继／RNG、四个不变父分支
及升级身份、降费、状态数量拒绝控制通过。JustCause消耗复制18输入及无队友
Challenge28输入原生配对对照通过。本组件不代表对友军施加易伤是更优策略，
也不代替整场获胜验收。

运行 `scripts/Check-Models.ps1 -Fixture player-challenge-recast-calibration.mt2f`。
