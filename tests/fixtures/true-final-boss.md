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

下一步为共享生命写入、预览隔离、跨层战斗及终局移除回调校准。Level9 仍被玩家
入口和 Level8 模型签名保护，不把观察归档计作已可求解或胜局。
