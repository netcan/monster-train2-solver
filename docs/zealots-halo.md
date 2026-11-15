# Zealot's Halo 动态攻击与装备生命周期

2026-11-15，本机 2.3.1、build 26650011 原版 `ZealotsHalo`
（`cbfec4a4-00fe-406e-b52a-e35e6456ba7d`）完成受控整场验收。
保留原版 1 费、友军装备目标、无特性和 +10 最大生命的定义；原始升级
`d66f5d82-9174-4212-b85c-e89b56f81be7` 的房间修饰器为
`RoomStateSelfDamagePerMissingHp`，Int=1、Int2=1，不禁止部署阶段。
控制单位、法术、留牌和敌军生命用于稳定触发边界，不计入原版卡覆盖。

原生实现要求修饰器属于查询者，且查询者有当前房间及物理站位；攻击增加量
为缺失生命乘 Int。Int2 虽被读取保存，却未参与运算。它只在角色攻击统计
查询时贡献攻击，不增加法强。模型将规则作为不可变升级载荷，复用装备的
添加、卸除、替换和复制流程；战斗引擎读取即时生命，避免连击中的旧生命
快照导致攻击滞后。规则在数值生命阶段后应用；原生致死提前返回时保持对应
部分应用边界。未校准的系数、缩放升级和半血触发混合仍有保护。

隐藏、静音、Instant 原生 worker 约 167.89 秒获胜，25 次出牌、7 次
EndTurn、71 房间／13 列车阶段，采集失败／差异／未支持／待结算全零，
原玩家文件不变。覆盖以下原生路径：

- 失血增加攻击、治疗降低攻击、满血归零；护甲完全吸收伤害不改变攻击。
- 同单位两件装备叠加：缺 4 血时加 8 攻，缺 7 血时加 14 攻。
- 升层、伤害另一层旁观者和降层保留正确所有者，旁观者不共享加攻。
- 卸掉两件后最大生命归 30、当前生命钳制到 30，规则和装备均清空。
- 重新装备、宿主最终死亡及另一宿主独立状态；容量一时替换最早装备，
  新旧卡实例准确且规则保留一份。

独立验证通过 155 次原生攻击 getter（116 次正数加成）、9 次完整装备
添加／移除后继与父状态隔离、254 份完整上下文、全部房间／列车阶段、
连续整场复放和 16 路不可变分支。25 记录决策、13 根候选的动作前沿及
128 状态串并行搜索／三步计划复放通过，记录和搜索严格零未支持。
旧原版装备 17 次操作、主动攻击原版整场 16 分支、状态加攻及腐蚀升级
独立配对定向回归通过；Probe、ModelChecks、Advisor 的 Release 构建通过。

原生源：
`.probe-runs/full-battle-units-spells-and-junk-20261115-114953-9a31fad1`。
归档 `tests/compatibility/game-2.3.1/mechanics/halo/battle.mt2f`，
59,584 字节，SHA256
`c9f8a31eca0f863f9e5de0cd03fe728cc356529bbea18b666d72a84120b0ee4c`。

随后在未修改原版定义的 no-cards 场景重扫目录，724 个唯一定义中，614 张
图鉴卡投影异常和显式 `ExternalInteractions` 保护均为零。决策、随机状态及
帧号不变；配套 45.12 秒七回合原生胜局四项零，52 房间／14 列车独立
验证通过。原生源：
`.probe-runs/full-battle-no-cards-20261115-115414-4162a68a`。
同目录 `catalog.mt2f` 为 119,404 字节，SHA256
`6cf391b6b184667761eaf02df6cdd8fa4d8f2ac435c32d8b9f2c6e97a1302208`。
两份二进制保留 TSV 哈希清单，无 JSON 依赖。

```powershell
pwsh -NoProfile -File scripts/Check-Game231Halo.ps1
```

重新采样：

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -OriginalHalo -NativeWorker 3 -BinaryCapture -TimeoutSeconds 360
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -CardCatalog -NativeWorker 3 -BinaryCapture -TimeoutSeconds 180 -SkipBuild
```

目录结果仅证明定义投影，不证明 614 张卡的全部动态调用、组合和自然对局。
本次没有专门校准克隆／嫁接／出生升级、完整卡牌悬停、半血／Horde／
LifeLink、致死最大生命卸装和动态规则与其他触发来源的竞争。新版 614 张
卡／236 件遗物整体复验与完整战斗模拟仍未完成，新增遗物整局仍为 12/12。
