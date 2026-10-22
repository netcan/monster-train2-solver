# Level9 原生出生边界

`true-final-boss-birth-calibration.mt2f` 是原生观察证据，尚不表示整场模型支持。
来源为 `full-battle-no-cards-20261022-183857-7869f3c7`，原生进程正常退出，
`DEPTH-PASS`，27.95 秒，受保护游戏文件未变。

隔离新存档调用原生 `MapScreen.Cheat_JumpToTFB`，明确记录解锁覆盖从 false 到
true、距离 0 到 8。原始场景、角色资源、起始牌组和火炉 80 血不改；这不是自然
解锁或自然推进。原生界面消耗 NonDeterministic，其他 15 条 RNG 流未变。
完成 `DoTrueFinalBossSpawning` 后连续两次同步读取状态一致，全部 RNG 流未变。

| 实体 | 攻击/生命 | 楼层索引 | 生命链接 | 原生角色类别 |
| --- | --- | --- | --- | --- |
| Dominion | 3 / 1555 | 3 | 独立 | 真终局，非外层 Boss |
| Savagery | 10 / 2750 | 0 | 独立 | 真终局，非外层 Boss |
| Entropy Bot | 15 / 3500 | 0 | 独立所有者 | 真终局、外层 Boss |
| Entropy Mid | 15 / 3500 | 1 | Bot | 真终局、外层 Boss |
| Entropy Top | 15 / 3500 | 2 | Bot | 真终局、外层 Boss |

五个实体都在上述楼层的原生物理成员列表中。三个 Entropy 分别注册到三层外部
锚点；HeroManager 的 `outerTrainBoss` 最终指向 Top。出生前队列为 `[0,2,1]`，
实际创建顺序仍为 Dominion、Savagery、Entropy Bot/Mid/Top，出生后队列清空。
不能据队列排列推断角色创建顺序，也不能把 Dominion/Savagery 当普通外层 Boss。

定向检查（不运行全量回归）：

```powershell
dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore -p:WarningLevel=0 -v:q
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --true-final-boss-birth-only tests/fixtures/true-final-boss-birth-calibration.mt2f
```

## 共享生命属性与原生预览深拷贝

`true-final-boss-linked-health-calibration.mt2f` 来自
`full-battle-no-cards-20261022-184603-5d1d6576`，原生进程正常退出、
`DEPTH-PASS`，26.92 秒，受保护游戏文件未变。出生后的五个原始实体分别临时绑定
新建、隔离的原生 `StateInformation`；只调用原生 `Hp` / `MaxHp` 属性和
`DeepCopyForPreview`，不调用伤害、治疗、死亡信号，不推进帧或恢复随机状态。
结束后原始状态对象引用全部恢复，五个实体/房间/队列状态、16 条游戏 RNG、
Unity 全局四字随机状态和帧号均未变。

70 个独立写入控制覆盖五个目标、两个属性和 `17/0/-3/99999/100000/Int32`
上下限；另有 10 个连续操作验证共享写入、两次原生深拷贝和副本独立写入。
`LinkedHealthModel` 与每个边界的完整原始存储、读取数值和更新标记匹配，
128 条并发分支匹配且输入不变。未知目标、未知属性、重复身份、悬空/自链接和
循环链接拒绝；多级转发尚未校准。

原生属性只有 99999 上限，负值不在此层归零；改最大生命也不会在此层补血或
截断当前生命。链接目标写入会标记目标和所有者，但保留目标自己的底层存储。
预览深拷贝读取共享生命数值，然后**去掉链接**，因此三个副本之后独立变更。
这些是状态属性边界，不能代替伤害回调、全局战斗预览或死亡行为的校准。

```powershell
./scripts/Check-TrueFinalBoss.ps1 -SkipBuild
```

下一步为跨层伤害/治疗及终局移除回调。Level9 仍被玩家入口和 Level8 模型签名
保护；出生观察与共享生命组件不计作整场可求解或胜局。
