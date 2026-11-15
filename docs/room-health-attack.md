# 原版冠军友军失血加攻整合

2026-11-15，固定游戏 2.3.1 / build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。

复用[38,160 次定义驱动查询校准](room-health-query-batch.md)的算术规则，
将 `RoomStateFriendlyDamagePerOwnMissingHp` 接入实际房间攻击、来源卡升级出生、
伤害／治疗后查询、跨层和死亡生命周期。一次场景自动发现并安装三个原版
`Punished1Path1a/b/c` 升级，使用原版冠军卡；不修改冠军或升级定义。

## 共享规则

原生 `RoomState.GetRoomStateModifiedDamage` 收集攻击者当前楼层、同阵营、
可被 Room 目标模式选中的存活来源。每个 FriendlyOwn 来源独立贡献
`(攻击者最大生命 - 攻击者当前生命) / max(1, 除数) * 系数`，最后叠加。
它取受益者自己的缺失生命，不取光环来源的缺失生命；只作用于 Monsters。
沉默不移除该房间修饰器。死亡或移层会改变该层收集到的来源集合。

模型复用已有升级记录与不可变世界，无新增卡牌专用效果分支。现有序列化
`SelfAttackRules` 承载单位拥有的原生修饰器，FriendlyOwn 项由房间统一结算，
避免自身查询和房间查询重复加算。战斗内部每次查询使用实时生命及来源集合；
攻击覆盖状态仍先于普通增量。

## 原生证据

隐藏、静音 NativeWorker 3 的 Instant 胜局耗时 **95.51 秒**：37 次出牌、
七次 EndTurn，48 个房间和 13 个列车阶段，捕获失败／差异／未支持／待完成
全部为零，终局效果结清，原玩家文件不变。原版定义在场景前后完全一致。

同场覆盖三个独立系数、来源和友军分别受伤、部分／完全治疗、三来源叠加、
来源与受益者分别进出楼层、沉默／解除、两个来源死亡、无来源控制及晚出生
受益者。两个零攻击控制单位在完成断言后移除，避免无关的长回合。

独立检查 1,092 次原生攻击 getter（379 次正增益友军）、明确数值断言、232 个
完整上下文、完整连续复放与 16 条不可变分支。全部 37 个录制决策的完整候选
和后继、128 状态串并行搜索及三步返回计划复放通过，记录与搜索零未支持。

检查器随后将录制候选核对从深搜中拆出，不再为诊断重复遍历所有录制前沿。
日常可单独运行生命周期或从三来源叠加／受益者满血的原生状态做一次完整展开，
串并行均选择得到 30 攻击的动作，完整返回路径及父状态不变检查通过。
默认阶段检查组合生命周期、全部录制候选与这项最小搜索；128 状态压力检查
由 `-DeepSearch` 显式加入，节点预算不代表任意深度最优性已证明。
本次同时运行两个快速入口时，生命周期为 29.025 秒，搜索为 14.034 秒。

二进制证据与大小／SHA-256 清单位于
`tests/compatibility/game-2.3.1/mechanics/room-health-attack/`，无 JSON 依赖。
来源为 `.probe-runs/full-battle-units-spells-and-junk-20261115-172305-1d8c6852`。

```powershell
# 日常：原生 getter、生命周期和完整复放，不重复搜索压力检查
pwsh -NoProfile -File scripts/Check-Game231RoomHealthAttack.ps1 -SkipBuild -LifecycleOnly
# 日常：只检查三来源状态的串并行搜索与返回计划
pwsh -NoProfile -File scripts/Check-Game231RoomHealthAttack.ps1 -SkipBuild -SearchOnly
# 阶段：生命周期、所有录制候选与一次完整展开的搜索
pwsh -NoProfile -File scripts/Check-Game231RoomHealthAttack.ps1 -SkipBuild
# 需要深搜压力验收时
pwsh -NoProfile -File scripts/Check-Game231RoomHealthAttack.ps1 -SkipBuild -DeepSearch
# 重新采样
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 `
    -OriginalRoomHealthAttack -GameSpeed Instant -BinaryCapture
```

## 验收边界

本批完成三个原版升级的普通战斗场景，不将其计成三张新卡，也不更新全部
614 张卡／236 件遗物的完成率。普通伤害、治疗和最大生命手牌的
[CardHoverPreview 另已完成 49 次原生验收](room-health-card-hover.md)，技能和复杂
特性／升级预演仍有保护；普通出牌战斗预测参与完整连续复放。缩放升级已有独立保护。
不可触碰来源的过滤沿用公共目标规则，特殊保留引用、任意升级增删／克隆／
复活、房间附件来源及更多跨机制交互仍需相应原生证据。
Self 类型 TitanSavagery 系数二的升级生命周期不由本批验收。

原版 Halo 定向回归通过，包含移动、装备增删、整链／16 分支、128 状态
串并行搜索。旧 `full-battle-original-movement-spells.mt2f` 的连续复放因缺少
月相掩码拒绝，在改动前 HEAD `08fdbefc` 的独立构建上复现同一错误，属于已有
归档缺口，未伪造默认值或重写证据。本批没有运行全部归档回归。
