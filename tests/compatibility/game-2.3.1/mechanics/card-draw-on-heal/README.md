# CardDrawOnHeal 原版回调

本录制来自 2.3.1，游戏模块 MVID 为 `bf93f060-beda-46b1-b808-95ebf1e9a99d`。
源运行是 `.probe-runs/full-battle-units-spells-and-junk-20261123-052256-4fcb2572`，通过
`-OriginalCardDrawOnHeal -BinaryCapture` 采集。原版遗物 ID 为
`9a38370b-150c-45bd-9e3e-93a688a04850`，探针在战前通过原版 API 获取它。
[录制](callbacks.mt2f)与源运行记录的字节数和 SHA-256 一致。[归档校验值](callback-receipt.tsv)

原版战斗胜利，采集失败、差异、拒绝和未完成的计数均为零。专项重放比较了 602 次管理器回调的完整上下文，
包括 4 次友军治疗抽牌、1 次同回合抑制、1 次敌方治疗对照和 3 次回合计数重置。
26 次出牌和已录制的结束回合分别按各自的原版前后状态验证。

测试场景使用受控训练单位和治疗、伤害对照牌。探针还在以下三个连续出牌边界调用
`CardManager.DrawSpecificCard`，将已有测试牌移到手牌首位，并由原版更新抽牌统计。
这份录制归入遗物回调验收。

| 回合 | `Actions` 索引（从 0 开始） | 测试牌 | 来源 |
| --- | --- | --- | --- |
| 1 | 4 | #11 敌方伤害对照 | 抽牌堆 |
| 2 | 9 | #8 友军治疗对照 | 弃牌堆 |
| 3 | 16 | #8 友军治疗对照 | 弃牌堆 |

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-card-draw-on-heal-only tests/compatibility/game-2.3.1/mechanics/card-draw-on-heal/callbacks.mt2f
```
