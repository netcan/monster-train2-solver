# 六张原版单位的受击、攻击与战后触发

2026-11-15，在 2.3.1、build 26650011 中，使用同一受控整场场景批量校准
六张原版单位。原卡费用、特性、单位基础数值与触发定义均保留，临时冻结
用于留牌；控制法术、楼层容量、训练兵及敌军生命用于稳定边界，不计作原版
卡牌覆盖。没有给每张卡单独实现一套模拟器。

| 原版卡 | 费用／攻／血／体积 | 本次路径 |
| --- | --- | --- |
| PenitentSentinel | 2／10／45／3 | 治疗免疫、受击永久 +1 生命，同步写回来源卡 |
| TheCondemned | 1／0／10／1 | 1 层 Tenacious，惩戒按护甲前受伤量的四倍伤害最前方敌军 |
| BrandedApostate | 2／5／15／1 | Bloodshed 为本次受伤的友军 +5 狂热 |
| Lasher | 0／9／9／1 | 攻击时对自己前方的全部友军造成 1 点伤害 |
| Cantor | 1／8／8／1 | 战后只给同层受伤友军 +1 吸血 |
| ScarredJusticar | 1／30／20／2 | 惩戒获得本场战斗 +15 攻击 |

此次新增两个共享机制。`CardEffectDamagePerOwnerDamageTaken` 的测试要求
所有者的护甲前伤害缓存为正且目标非空；执行时再次读取缓存，乘原版未修正
系数 4，只对正数结果执行伤害。它不使用攻击力或法强。原版目标是敌军
`FrontInRoom`；模型保留这个已校准的受限签名，复用伤害队列、来源归属和
单位伤害缓存。完整护甲吸收的 5 点伤害仍反击 20；直接伤害 3 反击 12；
伤害盾和零伤害对照不反击。

`InFrontOfSelf` 使用角色实际出生点的槽位索引，选中同层索引小于自身的
所有合格单位，不是只选紧邻的一个。Lasher 对三名前排友军逐一造成伤害，
其间嵌套的惩戒和 Bloodshed 仍走共享队列；另一层最前方的 Lasher 提供
空目标对照。缺失物理站位时返回未支持，不能猜测目标。

其余四种触发复用已有的永久／临时升级、来源卡写回、状态应用和健康过滤。
完全护甲抵挡可触发 Justicar 成长，零伤害不触发；治疗免疫不阻止 Sentinel
通过生命升级取得生命。Bloodshed 中的 `LastAttackedCharacter` 覆盖目标是
受伤者，狂热增加攻击和最大生命但不治疗；Cantor 不给满血单位加吸血。

UI 战斗预演会留下部分非主状态缓存写入：动作结束后的 Condemned 缓存
可能已被 Lasher 预演改成 1。因此采样直接记录反击执行时的缓存、原版系数、
目标和实际伤害请求，并分别校验即时反击与之后的预演写入。不能用动作结束
后的缓存倒推先前伤害，也没有为通过比较而忽略该字段。

隐藏、静音、Instant 原生 worker 302.36 秒获胜，25 次出牌、5 次 EndTurn、
47 房间／9 列车阶段，采集失败／差异／未支持／待结算全零，原玩家文件
不变。直接采到 6 次反击执行及 13 次效果测试：受伤缓存分别为 1／3／5
时系数均为 4，请求伤害分别为 4／12／20；伤害盾清零后的测试返回 false。
44 次 Bloodshed 受伤者增益和 13 次受伤目标战后吸血授予均通过独立断言。

独立检查通过 186 份完整上下文、全部房间／列车阶段、整场连续复放和
16 路不可变分支。25 记录决策、31 根候选、128 状态串并行搜索及三步计划
复放通过，候选和搜索严格零未支持。专用检查直接复用搜索检查内的全决策
前沿验证，避免再枚举一遍全部候选。旧原版 Scourged Zealot 的完整链／
16 分支／串并行搜索、攻击触发纯检查与 Penitent Sanctuary 的 76 房间／
13 列车定向回归通过；Probe、ModelChecks、Advisor 的 Release 构建通过。

原生源：
`.probe-runs/full-battle-units-spells-and-junk-20261115-122913-f117e0eb`。
二进制归档 `tests/compatibility/game-2.3.1/mechanics/punished-units/battle.mt2f`
为 62,917 字节，SHA256
`6125920445d9b00e18b981f068cf402b90b2e7dc91b4c1d20b5d1a839c0914ce`。
保留 TSV 大小／哈希清单，无 JSON 依赖。

```powershell
pwsh -NoProfile -File scripts/Check-Game231PunishedUnits.ps1
```

重新采样：

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -OriginalPunishedUnits -NativeWorker 3 -BinaryCapture -TimeoutSeconds 420
```

本次没有专门验收致死反击链、复杂链接、克隆／复活／嫁接、多个同名触发
来源竞争、其他系数／随机范围、完整悬停或所有楼层移动组合。新版 614 张
图鉴卡／236 件遗物整体复验与完整战斗模拟仍未完成；新增遗物整局仍为 12/12。
