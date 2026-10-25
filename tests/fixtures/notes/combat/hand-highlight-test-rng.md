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
干净提交的独立原生双进程组件校准继续进行；
不能据此记作终战胜利或新Windows安装验收，当前安装仍为0.2.110。
