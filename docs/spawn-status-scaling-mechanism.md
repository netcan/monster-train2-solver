# 出生时状态层数缩放

固定游戏 2.3.1 / build 26650011，MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。
`RelicEffectScaleStatusApplicationsOnSpawn` 按初始化后的参数复用，
不为每张卡牌、灵魂或变异器编写身份分支。

## 实际出生语义

沿用原有 Covenant／非 Covenant、部署禁用及条件分派。原生实际回调只处理
敌方 Heroes；排除子类型匹配即跳过。requiredMutator **存在时跳过**，与该类
独立的显示预演查询相反。本组实现实际执行，未认证完整鼠标悬停查询。

状态必须在原生状态数据库中存在且 `ScalesInEndless()` 为真；仅列表开关打开
时还须匹配配置的状态 ID，列表中的数量不参与计算。保留原生 float 乘法后
向上取整，不以整数近似代替。

初始状态遍历来源 CharacterData 的原始列表，计算 `ceil(original * factor)
- original`，只追加正数。使用现有状态应用管线、spawnEffect=true，不直接改
当前层数，也不把后加的升级状态误认作原始定义。重复回调每次仍从原始列表
计算追加量。复用已有 AuthoredStartingStatuses，不新增一套单位数据。

触发器按两个原生遍历顺序处理精确类型 CardEffectAddStatusEffect、CardEffectEnchant，
复制并缩放当前运行时状态数组。重复回调基于已改写参数继续缩放；继承的
NextMonster 类型不参与。保留触发器状态／身份、其他效果参数、目标筛选及
附魔生命周期映射。所有状态追加与参数改写先完成，再由既有出生队列处理
状态回调，不在逐个状态之间提前结算。

附魔采集同时修正为读取 CardEffectState 的运行时参数；继续读取原定义会丢失
上述改写。新 payload 随六条资源复制路径保留；缺失初始化数据继续拒绝。

## 原生证据

自动发现四个原版来源：SoulSavior_DifficultyTier2、SoulSavior_DifficultyTier3、
SoulSavior_RunDistance3、SoulSavior_RunDistance4，保留原始定义。
四个原版配置加 50 个控制，共 **54 组配置、2,592 次实际出生回调**：
六种倍率、八种开关组合、正负／零数量、双方阵营、子类型排除、两次连续调用、
requiredMutator 存在／缺席，以及精确类型与派生类型控制。

批处理只在脱离战场的对象上拦截 AddStatusEffect 请求，验证数量、顺序与
spawnEffect；**不执行这些请求的实际状态回调**。触发器参数改写由原生方法实际
执行，附魔快照与原生数组直接对比。完整决策状态、原定义、所检查 RNG、帧及
空队列保持一致；矩阵约 **0.343 秒**，离线对照与 16 路并行检查通过。

完整战斗携带未改定义的原版 SoulSavior_RunDistance4，在薪火创建前取得它，
保留其两项薪火属性伴随效果。敌人原始 Rage3／PreCombat 状态触发及三张
80 攻／5,000 血守军均为明确编写的控制，不能计作这些卡牌原版覆盖。
原生获胜，三次出牌、五次 EndTurn，42 房间／九列车阶段、九个牌循环、
132 份完整上下文匹配，四项失败计数为零；启动与整场约 47.36 秒。
完整连续后继、16 条不可变分支、三个录制决策的完整候选前沿、串并行根搜索
及返回计划复放通过，搜索零未支持。

两份二进制合计 **56,527 字节**，附 SHA-256 清单，无 JSON 依赖：
`tests/compatibility/game-2.3.1/mechanics/spawn-status-scaling/manifest.tsv`。

```powershell
pwsh -NoProfile -File scripts/Check-Game231SpawnStatusScaling.ps1 -SkipBuild
# 日常只运行批量组件对照
pwsh -NoProfile -File scripts/Check-Game231SpawnStatusScaling.ps1 -SkipBuild -ComponentOnly
# 更新原生证据：隐藏、静音的隔离实例
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -SpawnStatusScalingMechanism -NativeWorker 3 -BinaryCapture -GameSpeed Instant
```

定向回归覆盖上一组按牌堆数量出生升级和原版 Darkshard 升级附魔整局。
其余三个原版来源的伴随效果、灵魂取得／携带流程、复杂状态回调竞争、混合
Covenant、完整显示预演和缩放附魔的实战生命周期仍未全部认证。
本组不重跑八项阶段全集，不按组件数量推算全游戏完成率。
