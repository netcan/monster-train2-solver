# 薪火受伤加成共用机制

固定游戏 2.3.1 / build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。
按 `RelicEffectApplyExtraPyreDamage` 的初始化参数和原生调用顺序适配，
模型不依赖卡牌、遗物、灵魂或变异器的身份白名单。

## 规则与入口

`CharacterState.DoGetDamageToTarget` 先计算伤害并结算护甲、伤害护盾等防御，
再对薪火调用 `RelicManager.ApplyTowerDamageTakenModifiedRelicEffects`，
随后扣血、判断复活并记录伤害。同步的 `GetDamageToTarget` 查询没有这一阶段。
直接支付薪火生命也不因本机制改成伤害。

管理器按拥有者、效果原有顺序执行，并保留部署禁用、预演和条件判断。
每个效果在输入大于零时通知并执行 unchecked 整数加法，否则返回零。
不能先把所有额外值求和：负值或整数溢出可能改变下一效果是否执行加法。
没有效果时管理器原样返回输入，包含负输入。

房间通知重载在预演中仍调用条件触发记录；Covenant 拥有者跳过该记录。
本组用纯检查验证部署、预演、零伤害、一次性条件和父状态不变，
原生整局没有混入 Covenant。其他薪火伤害接口未因此自动获准。

采集保存原生初始化后的 `extraDamage`、原始参数、条件与拥有者元数据。
缺失或不一致的字段继续拒绝。已完整表示的无条件薪火加伤变异器可以通过
原有变异器保护；未知伴随效果及 Covenant 混用继续保留保护。

## 原生批处理与整局

八类目录自动发现四个定义：`DaemosT1`、`DaemosT2`、`DaemosT3`（各 +1）
和 `Echoes_PyreTakesMoreDamage`（+3）。定义字段保持不变；独立初始化效果
复用其数值参数，完成 **40 次回调对照**。另以八个正负／极值参数构造有序
效果列表，完成 **650 组管理器对照**，包含无效果和全部双效果组合。
共执行 1,320 次效果回调，原生矩阵约 **0.015 秒**。

矩阵使用独立通知状态，仅在同步测试期间屏蔽弹窗显示；游戏通知与 OnTrigger
仍由原生执行。finally 恢复管理器效果引用与顺序。完整决策状态、原版字段、
所检查 RNG、帧和空队列均保持一致；离线检查另做 16 路并行求值。
负数、溢出及极值是数值控制，不主张全部属于正常战斗可达输入。

整局携带不改定义的原版 +3 变异器，七张明确编写的控制法术依次验证零伤害、
正伤害、全额／部分护甲和全额护盾，随后由自然敌人攻击薪火。
控制法术不计入原版卡牌覆盖，三个灵魂的其他伴随效果也不计作整局认证。

原生获胜，7 次出牌、7 次 EndTurn，四项失败计数为零；启动与连续战斗
53.75 秒。独立检查匹配 13 个牌循环、52 个房间阶段、14 个列车阶段、
182 份完整上下文及 16 条不可变连续分支。七个录制决策的完整候选前沿
零未支持，根搜索串并行结果及返回计划复放一致。

二进制证据与 SHA-256 清单位于
`tests/compatibility/game-2.3.1/mechanics/pyre-damage-taken/manifest.tsv`，
两份归档合计 54,921 字节，无 JSON 依赖。

```powershell
# 离线组件与整局检查；不启动游戏
pwsh -NoProfile -File scripts/Check-Game231PyreDamageTaken.ps1 -SkipBuild
# 日常只检查参数矩阵
pwsh -NoProfile -File scripts/Check-Game231PyreDamageTaken.ps1 -SkipBuild -ComponentOnly
# 需要重新录制时使用隐藏、静音的隔离实例
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -PyreDamageTakenMechanism -NativeWorker 3 -BinaryCapture -GameSpeed Instant
```

定向回归通过三份薪火反伤终局与完整复放／分支、原版 Bone Dog 致死复活
原生归档，以及既有薪火资源和复活纯检查。本组没有重跑八项阶段全集或全部
历史归档，也不增加全游戏完成率。
