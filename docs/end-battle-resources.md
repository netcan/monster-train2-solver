# 战后资源遗物与战斗边界

游戏 2.3.1、build 26650011 的自然 Banished/Pyreborne、种子 20261115、
Covenant 0 第一战在任何输入前被 `ModifyDragonsHoardBattleEnd` 拦截。
源码显示两个 sealed 效果 `RelicEffectModifyGoldEndOfBattle` 和
`RelicEffectModifyDragonsHoardEndOfBattle` 的战中 Test 只记录私有的薪火受伤标志，
总是返回 false；Apply 协程为空。只有 EndOfBattle 消费者修改金币或宝藏。
因此战斗验证识别这两个效果，仍逐项拒绝同一遗物上的未知伴随效果。

现有独立战后资源模型复用至新版。2.3.1 将正伤害的 `OnPunish` 也记为受伤；
2.2.1 保留仅 `OnHit` 的旧规则。检查器以版本和 MVID 区分两种原生证据，
不推断后续版本行为，也不以原生预演充当搜索分支沙箱。

一次隐藏、静音的 Worker 3 原生组件批次完成 864 次实际战后管理器调用：
432 次金币增加、144 次宝藏增加、108 次受伤阻断和 24 次战斗开始复位后恢复。
包含原始两个遗物、六种安装／移除顺序、三种容量、三种初值、重复执行和
八种事件／控制。伤害输入是受控的原生回调，不是实际扣血。Before/After
记录状态、受检查 RNG 与帧号一致，16 路不可变模型分支通过。

批次所在原生进程 44.356 秒正常退出、胜局，采集／差异／未支持／待定计数
均为零，玩家原文件未变。组件在 finally 中移除原始遗物并恢复资源，因此该
胜局只确认采样宿主成功；不能作为自然战局携带这两个遗物的整场整合证据。

原先被阻断的同一自然检查点现已独立复验成功：输入存档 SHA-256 未变，
Banished/Pyreborne、种子 20261115、Covenant 0、
自然首战 `Level1BattleHorde`，保留原始牌组、遗物与关卡。使用默认整场规划、
两工作线程／3000 ms，配置保留 Horizon=3、MaxSteps=256；16 次出牌与五次
EndTurn 完成，薪火 80 血。整场模式的终局预测不受三回合窗口截断。
23 次发布、180 次捕获零纯度失败，完整预测／状态／RNG、九个列车阶段及
42 份根／后继中的原始宝藏遗物身份均通过独立检查。

该隐藏静音 Worker 3 原生进程 75.343 秒、退出码 0、四项计数全零，玩家原文件
未变。原生实际加载和运行后的源检查点哈希与原始输入一致。
自然归档 `natural-ring1.mt2f` 为 45,531 字节；这是携带原始宝藏遗物的单次完整
原生源局及独立模型检查，不认证金币遗物同场、独立冻结原生配对或后续战役。
日志为 `results/game231-natural-ring1-resource-fixed.log` 与
`results/game231-end-battle-resource-final-check.log`，不加入 Git。

二进制证据位于 `tests/compatibility/game-2.3.1/mechanics/end-battle-resources/`，
清单固定字节数和 SHA-256。日常定向入口：

```powershell
pwsh -NoProfile -File scripts/Check-Game231EndBattleResources.ps1
```

同时比较新版 864 和旧版 756 条，并检查战斗分类、未知伴随效果保护及自然
源局完整后继。只复验消费者时可加 `-ConsumerOnly`，不能将该子集算作自然局
通过。这是一组共享战后机制与战斗准入修复，未重算新版全部遗物的整体覆盖率。
