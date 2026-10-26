# Fracture 的精确遗物复制

第十三组 Banished/Wurmkin 第七战离线候选的第9输入，将带单个原版
TraitCorrupt升级的StarterFracture18用于Carver6。原生为同层Carver7执行
私有副本，两者各增加腐毒2／护甲3；房间腐化只增加1，遗物计数0→1，
原牌只增加一次PlayCount并进入弃牌区，NextCard不变。

0.2.130只补齐原版Fracture单个DropTargetCharacter腐毒2效果和精确零数值
TraitCorrupt升级；其OnPreCardPlayed只在原牌执行，副本不重新执行腐化。
私有副本不使用原牌实例编号写统计。原生无位置变化，因此无需替换效果。
修改状态层数、升级身份、升级伤害的对照仍拒绝。

原始拒绝及修复后独立原生复放各9输入，完整前缀／后继／RNG、一次腐化、
弃牌和遗物计数及四个不变父分支均通过。空队友28输入、LiquidCourage19输入、
JustCause13输入双进程对照通过。这是合法分支校准，不代表向友方施加腐毒
是好策略，也不计为第七战胜局。

运行 `scripts/Check-Models.ps1 -Fixture player-fracture-recast-calibration.mt2f`。
