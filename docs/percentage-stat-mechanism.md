# 出生百分比攻血的共用机制

固定游戏 2.3.1 / build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。按
`RelicEffectModifyCharacterAttackOrHealthPercentage` 实现与参数适配，
不按卡牌或遗物身份新增运行时白名单。

## 规则与接入

`RelicPercentageStat` 捕获效果顺序、原始百分比、原生初始化后的 float 倍率、
攻／血选择、阵营掩码、子类型及所需变异器的实际启用状态。带未适配效果条件、
缺少倍率或未知伴随效果的输入继续拒绝。27 条既有遗物不可变复制路径保留该字段。

- 查询路径要求阵营相等，返回基于 CharacterData 原始属性的差值；None 不是通配。
- 出生路径采用 None 通配／HasFlag，以及原生角色管理器的子类型过滤语义。
- 血量以当前最大生命计算，直接设置当前／最大生命，并保留原生 99,999 上限；
  不调用治疗、受伤或半血回调。
- 攻击以未增益攻击加遗物额外攻击为输入，先应用晚期倍率并向上取整，再应用
  百分比倍率并再次取整，写回原始攻击；不改成 DamageBuff 或永久卡牌升级。

运行时通过共用 `RelicSpawnStatusModel.CharacterAddedWithPending` 按遗物／效果顺序
执行，沿用出生队列的结算位置。玩家、普通敌人和既有其他出生入口复用同一分派。
本版原生元数据中的额外攻击提供者只有 `RelicEffectAdjustAttackByStatusEffect`，
已有状态加攻描述继续复用；唯一晚期提供者 `RelicEffectMultiplyAttackLate` 尚未适配，
仍被全局遗物类型保护拒绝，不能把当前中性晚期倍率说成该提供者整合完成。

极值对照发现 Mono 初始化 float 表达式与离线 .NET 的中间精度存在差异。
因此保存原生 `percentModification` 的实际 float；缺失时拒绝，避免在另一运行时
重新根据整数百分比计算而改变结果。

## 批量原生对照

`PercentageStatsMechanismBatch` 自动扫描八类原生遗物目录，发现六个原版变异器、
56 处效果，保留重复定义。另生成 80 组百分比／攻血／阵营控制和两组所需变异器
启用／未启用控制，共 138 组配置。七组原始攻血、四种阵营、子类型匹配／不匹配
共生成 **7,728 组查询与实际出生回调对**，包含负数、零、奇数、封顶与整数极值。
负数和极值是数值控制，不主张它们均为正常对局可达状态。

回调使用未注册的独立单位、独立 CharacterData 和独立效果实例；不挂载遗物状态，
因此不分发遗物 UI 通知。原版参数只读。采样前后完整战斗决策、捕获的原版定义字段、
受检查 RNG、帧号和空触发队列一致。独立检查同时比较共享不可变单位转换，
另做 16 路并行求值、字段缺失／未知伴随效果／晚期提供者拒绝及 27 条复制路径检查。

原生矩阵实测 **0.240 秒**；该次游戏启动与完整战斗合计 **40.10 秒**，正常退出。
冻结证据只包含二进制与哈希清单：

- `tests/compatibility/game-2.3.1/mechanics/percentage-stats/batch.mt2f`：29,149 字节。
- 同目录 `battle.mt2f`：32,538 字节。

## 连续战斗与边界

原版 `Echoes_DoubleEnemyUnitHealth` 的定义不变，在隔离场景中实际启用。
耐久 Steward 是明确改写的控制单位，不计入原版卡牌覆盖。原始未来波次不预加生命，
实际出生再由上述共享回调加倍；检查器另验证观察到的敌方最大生命确为原始值两倍。

本场原生胜局四项失败计数为零，完整后继／终局／RNG、一次出牌、五次 EndTurn、
40 个房间阶段、九个列车阶段、124 份完整上下文及 16 条独立分支通过；完整录制候选
前沿零未支持，串／并行根搜索与返回计划复放一致。

六个原版定义的查询／独立出生回调通过，不等于六个变异器整局均已验收。
其余变异器的伴随效果、与 Covenant 预计算出生属性的混用、非中性晚期倍率提供者，
以及带其他效果条件的组合仍需处理。当前生产采集只有全为无附加条件的百分比效果、
且没有 Covenant 的变异器集合，才绕过旧的笼统变异器保护；其他保护继续保留。

```powershell
# 日常只跑批量组件，不启动游戏
pwsh -NoProfile -File scripts/Check-Game231PercentageStats.ps1 -SkipBuild -ComponentOnly
# 组件与完整战斗／搜索检查
pwsh -NoProfile -File scripts/Check-Game231PercentageStats.ps1 -SkipBuild
# 需要重新采样时，隐藏、静音的隔离实例
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -PercentageStatsMechanism -NativeWorker 3 -BinaryCapture -GameSpeed Instant
```

原版 Juice 整局及固定阶段中的 punished-units（25 次出牌／五次 EndTurn）定向回归通过。
这是一组共用机制及其整局整合交付；没有重新运行八项阶段集或全部归档，
也不增加全游戏完成率。
