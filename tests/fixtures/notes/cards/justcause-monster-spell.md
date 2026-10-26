# JustCause 的队友法术复制

第十三组 Banished / Wurmkin，种子1184964217，第三战第二回合的离线候选在
第13输入把带商店腐化的 JustCause19 施放于第一层单位6，被原模型拒绝。
原生遗物 `SingleToTeamMonsterSpellTargeting` 生成私有卡牌副本，
把位置变化效果换成 `CardEffectNULL`，保留后续 Valor2；复制对象11、13
获得增益，队伍仍为6、11、13，不能再次把两个队友移到最前。

0.2.118按这个签名处理复制，并保留转换后的效果，防止拥有者卡牌的效果刷新
将其恢复为原始移位效果。私有副本没有玩家牌组实例编号，其状态统计不应
累计到原卡19。当前同时支持精确的原始商店 `TraitCorrupt` 升级，其回调只在
首次施放前运行；其他升级和复制卡牌回调仍会拒绝。

`player-justcause-monster-spell-{replay-,}calibration.mt2f` 是两个独立原生进程
的13输入观察，原件24102字节，修正后23926字节。完整前缀、后继、卡牌统计、
RNG和四个并行不变父分支均通过；保留原拒绝记录。额外修改Valor层数、升级
身份或伤害的控制分支继续拒绝。这是机制校准，没有计作默认预算的整场胜利。

新增合法动作后，旧 `full-battle-original-monster-spell-targeting.mt2f` 的普通
策略会在第4个后续动作选择另一张牌。因此旧档保留为原生路线，20次动作、
10次后续输入和4次结束回合均按原顺序核对，包含16个并行不变分支。
另以0.2.119原生重新执行当前策略，独立归档
`full-battle-original-monster-spell-targeting-expanded.mt2f`：22次动作、12次
后续输入、3次结束回合，原生获胜且四项零失败；完整动作和16个并行路线
重放通过。此处不要求不同支持范围的策略选择同一张牌，也未省略状态比较。

运行 `scripts/Check-Models.ps1 -Fixture player-justcause-monster-spell-calibration.mt2f`。
