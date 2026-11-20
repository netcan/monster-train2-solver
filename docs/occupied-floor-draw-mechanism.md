# 敌方占层抽牌缓存

固定游戏 2.3.1 / build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。
`RelicEffectAdditionalDrawForEnemyOccupiedFloors` 按原生实现复用，
模拟器不按 Warborne 等级或来源 ID 分支。

## 实际语义

该类的初始 Value 为零，注册在 MutableRules.ChangeStartingHandSize。
查询直接使用当前缓存，不能改成每次重新统计敌人；其定义参数不改变计数公式。
EndTurn 和抽牌后的 Test 会刷新并发出下一回合抽牌信号，始终返回 false。
CombatManager 在每层完成战斗后执行 EndTurn 回调，不能挪到整塔战斗结束后。

敌方出生／死亡仅在 MonsterTurn（玩家行动阶段）刷新。出生沿用部署限制，
但原生没有预演过滤；死亡沿用预演过滤，但没有部署过滤。普通计时回调同时
沿用部署、预演和条件过滤。缓存更新不发放额外卡牌，也不通知遗物触发计数。

刷新遍历普通楼层，跳过薪火层。按物理敌方占位统计仍存活且没有发出消失事件
的角色，包含外侧 Boss。原生 TryGetCharacterTriggerEffectStates 只取第一个
PostCombat 触发器；其中存在 CardEffectDespawnCharacter 时不算占层，即使单位
被封印。后面的同类触发器不参与这次查询。该底层筛选按源码转写并有模型控制；
外侧 Boss 与复杂触发组合未在本次整场中穷举。

效果出现时，采集器自动启用完整列车世界。查询可能遇到尚未物理移除的死亡
占位，因此相关角色保留到物理引用移除后；不会把找不到角色的占位当作空位。
出生、死亡、逐层回合末、实际抽牌和后续显示查询共用这份不可变缓存。
按 owner 索引更新，支持相同来源 ID 的不同实例；沿用已有抽牌注册顺序与上限。

## 原生证据与回归

自动发现 WarborneT1/T2/T3 三个原版来源，分离效果实例上共执行 2,304 条
原生回调：四类回调、六个阶段、双方阵营、八种占层组合、两个陈旧缓存值。
原生矩阵约 0.013 秒，不含启动。临时控制阶段、阵营与 HasLivingCharacters
返回值，并截获显示信号；真实回调、查询选项、缓存变化和 Test 返回值独立比较。
完整战斗、原始定义、RNG、帧与队列前后相同。

模型侧 2,304 个不可变后继及 16 路并行数值复验通过，另检查死亡／消失占位、
战后消失与封印、多个 PostCombat 触发器、部署／预演差异、重复来源实例及父状态
不变。批量对照控制了房间查询，不能当作所有底层角色筛选的原生实测。

整场在房间初始化前取得未改定义的 WarborneT1。使用三个零费、80 攻击／
5000 生命的受控 Steward，以及一张零费、200 敌方整层伤害控制法术；
这些控制不计入原版卡牌覆盖。实际 MonsterTurn 击杀使缓存从 1 降到 0，
同时验证抽牌后的正奖励、真实抽牌顺序与陈旧值消费。

隐藏静音 NativeWorker 3、Instant 原生胜局 43.59 秒，4 次出牌、5 次 EndTurn、
42 房间阶段、9 列车阶段，采集失败／差异／未支持／待完成四项为零。
134 份完整上下文、连续整局后继、16 条独立分支、全部录制决策候选、
串并行根搜索及返回计划复放通过。两份二进制合计 51,584 字节，SHA-256 清单
固定，不依赖 JSON。

原版开局／满血抽牌的 25 出牌、5 EndTurn、16 路完整策略回归，以及牌堆抽牌、
旧资源、动态能量和自动光环世界的 32 条状态／76 个回调定向回归通过。
额外尝试的 2.2.1 旧光环整链归档在房间启用标记上失败；隔离编译上个提交
`ef8b6734` 得到相同差异，属于已有兼容边界，未作为本批通过项。

```powershell
pwsh -NoProfile -File scripts/Check-Game231OccupiedFloorDraw.ps1
pwsh -NoProfile -File scripts/Check-Game231OccupiedFloorDraw.ps1 -SkipBuild -ComponentOnly
```

T2/T3 的 Relentless 与 T3 第十次出牌抽两张，已由[后续批次](relentless-draw-mechanism.md)
按原版定义同场验收；完整等级生命周期仍需继续扩展。
实际 MonsterTurn 敌方出生、外侧 Boss 多锚点、复杂出生死亡竞争、混合 Covenant
与完整悬停组合也未在本次整场穷举；本组通过不等于全部 Warborne 等级已完整可用。
