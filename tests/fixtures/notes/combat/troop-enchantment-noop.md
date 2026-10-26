# 部队强化缓存不变与附魔并存

第十四组Underlegion/LunaCoven，种子934692550，第六战正常入口。原生八输入前缀的
第三次结束回合出现EnchanterT1_Speed；它只给敌方附加haste。中层MonsterSquadLeader
仍没有Fun Guy目标，缓存为空。0.2.152因为存在附魔注册项一律拒绝部队强化刷新。

0.2.153只在强化目标不需要增删任何实际修饰时允许同时存在附魔。不存在或已死亡的
旧缓存可以正常清理；新目标或失去范围的存活目标仍拒绝，未扩大到未知顺序的强化
增删组合。原生RoomStateTroopUnitUpgradeModifier.OnUnitOrderPossiblyChanged在目标
与缓存一致时也不执行AddAllModifiersToTarget/RemoveAllModifiersFromTarget。

原始拒绝进程`.probe-runs/full-battle-units-spells-and-junk-20261026-193424-67a4de8d`
与独立修正进程`.probe-runs/full-battle-units-spells-and-junk-20261026-194114-63c93842`
均完成八输入完整状态/RNG检查。四个并行不变父分支一致；新增目标、离开目标的两个
拒绝控制与死亡缓存清理控制通过。原件归档于`tests/fixtures/combat/core/`。

定向入口`--troop-enchantment-noop-only <source> [replay]`；也可运行
`Check-Models.ps1 -Fixture player-troop-enchantment-noop-calibration.mt2f`。
旧队长出生、增员、减员、移层及最后一个部队移除对照通过；41项附魔原生转换、
135项完整回调及32个独立分支对照通过。旧死亡归档完整链的RetainedUnits差异在
改动前0.2.152也复现，未计为本轮通过，也未作为本次新增回归。第六战整场继续验收。
