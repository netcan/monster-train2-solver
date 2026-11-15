# 原版 Punished Spike 与两张锁链法术

游戏 2.3.1、build 26650011，Assembly-CSharp MVID
`bf93f060-beda-46b1-b808-95ebf1e9a99d`。
三张原版定义在控制场景改动前保存，费用、效果与特性不修改：

| 原版卡 | 费用 | 原生行为 |
| --- | --- | --- |
| PunishedSpike | X | 一次添加 8X 狂热，再向同一目标执行 X 次 1 点伤害，消耗 |
| Imprison | 2 | 对敌人添加 4 锁链，再对上次目标添加 1 禁移动，消耗 |
| StarterSharedSentence | 1 | 对敌人添加 1 锁链，正常弃牌 |

`CardEffectDamage.GetEffectApplicationCount` 调用
`GameEffectHelper.GetDamageEffectApplicationCount`，由第一条
`CardTraitScalingAddEffectApplications` 读取本次实付能量。
`CardEffectAddStatusEffect` 使用基类的一次执行，不读取伤害的次数 getter。
因此 X=3 的狂热为 24，随后伤害三次，不能把狂热也添加三次。
模型扩展已有次数查询，复用狂热缩放、上次目标、状态、伤害与死亡队列；
原版 Moonbeam 的前排重新选目标路径及保护仍保留。

隔离场景共 22 次输入、7 次 EndTurn，八次原版施放包括六张 Spike、
一次 Sentence 与一次 Imprison。Spike 的实付能量为 0/1/3/3/3/3。
前五张保留默认伤害，最后一张只通过普通永久升级增加 3 点伤害。
控制单位为同层 30/4 生命和远层 30 生命；第三层的 1000 攻/500 血控制单位
完成后续战斗。原版守卫保留 25 护甲和 2 尖刺，生命改为 1000。
这些控制定义不计入原版卡牌覆盖。

三点逐次伤害被友军护甲完全吸收时，仍逐次触发五层锁链的 3 点伤害，
共扣守卫九点护甲。对敌人直接施放只增加其狂热及扣三点护甲。
远层友军受伤不会触发另一层的锁链；它与技能从远层攻击同层受害者的场景不同。
升级 Spike 首击杀死四血友军后，原生仍执行后两次伤害协程并保留死亡目标 ID；
两次均不改变完整列车状态，也不改打其他单位。
自然回合结束后守卫仍在第一层，禁移动保留，锁链 5→4、狂热 24→23。

直接拦截六次原生伤害次数 getter，核对实付能量、原生特性计算及状态不变；
独立模型对每次样本运行 32 条不可变分支，同时检查状态次数为一。
十三次实际伤害协程记录完整前后状态、实际目标及完成标记，要求致死后的
两次调用保持完整状态不变。缺失来源、改变统计周期或未校准目标仍受保护。

独立验收比较全部房间、列车、连续整场后继和 RNG，运行 16 路整场不可变分支，
再枚举全部 22 个记录决策；128 状态的串行/四线程搜索要求路径、分数和最终状态
一致并复放结果，全部记录及搜索候选严格零未支持。

固定原生录制只执行并验证本次计划输入，直接进入专用策略；完整候选枚举与
搜索在独立检查器完成。这样避免先计算通用策略后丢弃结果，再枚举专用输入。
此优化只应用于该固定场景，不改变玩家规划器。

三次隐藏、静音、Instant 原生采样分别为 216.22、158.92、79.85 秒：
第二次取消专用策略的重复前沿枚举，第三次直接路由专用策略。
三次均为相同的 22 输入/7 回合原生胜局，55 房间/13 列车阶段，
采集失败/差异/未支持/待结算四项为零，退出码零、`DEPTH-PASS`，玩家文件未变。
最终采样耗时比首轮减少 63.07%；这是该固定场景的实测，不是全套或玩家搜索基准。
正式归档的独立检查还覆盖 216 份完整上下文、44 根候选、128/128 搜索状态
和三步返回路径复放。旧 Moonbeam 原版次数/整链、现有锁链整场定向回归，
ModelChecks、Probe、Advisor 三个 Release 构建通过。

正式归档为 `tests/compatibility/game-2.3.1/mechanics/punished-spike/battle.mt2f`，
59,601 字节，SHA-256
`c654605e78452aaea06054abbcbb9cf8b54c6f7e445b7bb0483cb30d468cce70`，无 JSON 依赖。
正式来源 `.probe-runs/full-battle-units-spells-and-junk-20261115-140217-673dcd84`；
前两次分别为 `...-20261115-135352-4677e492` 与 `...-20261115-135912-851697a4`。

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -NativeWorker 3 -OriginalPunishedSpike -GameSpeed Instant -BinaryCapture -SkipBuild
pwsh -NoProfile -File scripts/Check-Game231PunishedSpike.ps1
```

未知次数特性组合、X 费房间/遗物倍率、复杂回调在重复之间改换目标、
Dualism、克隆/复活及完整悬停仍需各自证据。新版 614 卡/236 遗物的整体复验
及完整战斗模拟尚未完成，新增遗物原版整局验收仍为 12/12。
