# EchoInfusion 先天／腐化／消耗组合

第十三组第七战第二回合，EchoInfusion23同时带原版TraitCorrupt、
ReduceCostAddConsume、TraitInherentSpellReduceCost三个永久升级。
0.2.133只接受该精确身份、数值和四项特性组合；改变身份、消耗降费或先天降费
仍拒绝，不扩大到任意腐化／消耗组合。

原卡先执行一次腐化及消耗，遗物随后向同层队友施放不重复原卡回调的私有副本。
每个友方目标生命及最大生命各加15，NextCard不变，PlayCount只加一次，
遗物计数0→1。已有2点腐化时，原卡加1、限制支付1，最终仍为2；
零腐化的另一层同样可施放，原卡产生并支付1，最终仍为0，两个队友各获得3护甲。

两组分别使用原始拒绝与修复后独立游戏进程，各9输入完整前缀、后继和RNG，
单位生命／护甲、一次腐化／消耗和遗物计数、四个不变父分支均通过。
既有JustCause消耗复制18输入、腐化限制11输入、留存返回45输入及返回后施法
62输入配对对照通过。本组件不代替整场胜局或任意升级组合验收。

运行 `scripts/Check-Models.ps1 -Fixture player-intrinsic-infusion-calibration.mt2f`
及 `scripts/Check-Models.ps1 -Fixture player-intrinsic-infusion-zero-calibration.mt2f`。
