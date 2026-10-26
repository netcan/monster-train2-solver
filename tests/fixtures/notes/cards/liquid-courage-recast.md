# LiquidCourage 的原生遗物复制

第十三组 Banished/Wurmkin 第六战第三回合，SingleToTeamMonsterSpellTargeting
本回合尚未使用，LiquidCourage15 对 Avenger7 施放后，原生也为同层 Carver6
施放私有副本：两者均增加护盾1、勇武4。模型此前只接受伤害和 JustCause
的复制签名，拒绝了这一分支。

0.2.127 仅补齐这张原版、未升级牌的精确双状态效果。原生 Consume 的
OnCardDiscarded 在 isRecastCopy 下无操作，因此原目标出牌只消耗一次，
私有副本不进入持有牌编号，也不写回原牌的房间缓存。

原始拒绝和独立修复复放共19输入，完整状态／RNG、双方状态、遗物一次计数、
唯一消耗牌堆条目和原牌一次 PlayCount、四个不变父分支均通过。
既有已用完遗物19输入、JustCause13输入双进程回归通过；改变护盾层数的
主动复制仍拒绝。本证据不扩展到任意升级、混合效果或其他 Consume 回调。

运行 `scripts/Check-Models.ps1 -Fixture player-liquid-courage-recast-calibration.mt2f`。
