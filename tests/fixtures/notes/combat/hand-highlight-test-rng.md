# 手牌高亮消耗测试随机数

第十一组Stygian/Awoken的薪火21终战入口，实际默认建议第21输入结束回合后，
完整局面只有`Enchantments.TestRng`四个字不同，实战`BattleRng`及所有其他字段一致。
原生失败档案和独立被动观察分别保留在
`player-hand-highlight-test-rng-{counterexample,observed}.mt2f`。

模型预测两次预览概率抽样；原生还多40次目标抽样。独立观察的调用栈表明，
新手牌Icestorm的五段`RandomInRoom`由`HandUI.UpdateCardUIAtEndOfFrame`调用
`CardUI.IsPlayableAndAffectsState`反复测试。它们运行在`PreviewMode=false`，
但`TargetHelper.CollectTargets`根据`isTesting`使用`BattleTest`。
原生测试流恰好前进42次；独立检查保留这一反例，未删去随机数比较。

0.2.111玩家插件在该原始高亮查询周围保存、恢复测试流，保留查询执行和返回结果，
不读取或恢复实战Battle流。仅记录调用／恢复／异常累计计数，避免逐帧记录增长。
显式开发双流隔离继续使用原有协议；非活动开发补丁不再覆盖玩家补丁的`__state`。
插件版本常量同时更新为0.2.111，避免启动日志继续显示0.2.89。

原21输入及40次UI调用栈的定向检查已通过。开发构建的21原生后继和完整
状态／预测／RNG、八个不变父分支通过；399次高亮查询中8次恢复测试流，异常0。
干净提交`27fdeed8`的独立原生双进程校准已通过，同一21输入逐步完整后继、
预测和RNG一致，八个不变父分支通过，两份原生件分别为
`player-hand-highlight-test-rng-{calibration,replay-calibration}.mt2f`。
源`20261026-075321-404f790b`、重放`20261026-075321-122598ca`，每个原生进程65.42秒，
原玩家文件未变。这是原生UI测试流保护的验收，未弱化状态比较或关闭原生高亮。
不能据此记作终战胜利或新Windows安装验收，当前安装仍为0.2.110。

继续实际默认终战后，第25输入施放Icestorm时只出现测试流差异。
`player-effect-test-rng-{counterexample,observed}.mt2f`保留完整25输入原失败件和
独立被动观察：`CardManager.PlayAnyCard→CombatManager.TestEffects`消耗5次，
`CombatManager.FireCardEffectInternal→GameEffectHelper.TestEffect`另消耗5次。
高亮保护已经生效，但没有覆盖这两个非UI查询入口；实战Battle流及其他完整状态一致。

开发版将测试流保护移至上述复合查询和单效果查询边界。嵌套单效果查询保留
复合查询内部的抽样顺序，只在最外层返回时恢复；独立单效果查询也恢复测试流。
查询原返回值、异常和实战Battle流保持原样，显式开发双流隔离仍沿用原协议。
25输入定向反例检查及修正后独立原生验收分别记录，不把旧21输入校准当作新修复的验收。

干净提交`d398f7b9`的25输入独立原生双进程校准已通过，
`player-effect-test-rng-{calibration,replay-calibration}.mt2f`逐步完整状态／预测／RNG一致，
每个进程八个不变父分支通过。原5次出牌查询、5次执行查询和75次高亮目标抽样均保留；
测试流不再被查询推进，实战流与模型一致，保护异常0。
源`20261026-080501-33caf89d`、重放`20261026-080501-6c8b5660`，
原生耗时76.25／75.04秒，原玩家文件未变。整场及Windows交付继续单独验收。
