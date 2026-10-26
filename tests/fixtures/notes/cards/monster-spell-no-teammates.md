# 没有队友时不校验额外复制签名

第十三组 Banished/Wurmkin 第七战第五回合，对房间内唯一友方单位
Carver7 使用带 TraitCorruptwIcon／ReduceCostAddConsume 的 Challenge15。
原生只执行原牌：腐化1→2，目标获得护甲3／近战弱点1，出牌计数增加一次，
消耗堆只有一个原牌条目，NextCard不变。SingleToTeamMonsterSpellTargeting
没有额外队友目标，因此本回合触发标记和计数均保持false／0。

旧模型在筛选队友前先检查额外副本的效果，错误拒绝原牌。0.2.129仅将
现有队友筛选及空集合返回前移，保留实际复制时原有的效果／特性限制。
本观察不证明主动复制 Challenge 或任意升级效果已支持。

原始拒绝与修复后独立原生复放共28输入，完整状态／RNG、原牌单次腐化、
消耗和未使用的遗物条件均一致；四个并行分支保持父状态不变。
真正复制队友的LiquidCourage19输入、JustCause13输入，以及遗物次数已用完
的19输入双进程对照均通过。

运行 `scripts/Check-Models.ps1 -Fixture player-monster-spell-no-teammates-calibration.mt2f`。
