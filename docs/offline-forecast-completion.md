# 离线补全部分预测

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll complete-root-plan `
  <native.mt2f> <verified-root-prediction.mt2f> <new-output.mt2f> 2
```

用于检查有希望但尚未结算的整战预测。先独立核验部分预测的全部输入、终局前
状态、RNG、预算和父状态，再核对原生归档 SHA-256 和预测对应的原版决策根。
从战斗中段开始时，先从部署复放原生输入，逐步比较完整状态和 RNG，到达预测起点后
保留原预测全部步骤，再用现有战术策略补到结算。可用 `--strength`
选择现有启发式策略。不会增加玩家搜索预算或操作游戏。

同一场战斗可在一条已经完整复放的模型路线中再次选择重搜点。用 `plan-root`
读取该路线的零起始决策索引，再把同一文件传给 `complete-root-plan --baseline`。

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-root `
  <baseline.mt2f> <new-root-prediction.mt2f> 10000 58
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll complete-root-plan `
  <native.mt2f> <new-root-prediction.mt2f> <new-output.mt2f> 2 --baseline <baseline.mt2f>
```

工具先独立复放整条基线，核对原生归档的哈希、部署根和新预测记录的基线哈希，
再从原生部署根逐步重放至指定决策，比较完整输入后继和 RNG。新预测若已到终局，
直接保存拼接后的整条路线。输出继续引用原生归档，可作为下一次重搜的基线。

全程最多256输入，每回合最多18次后续出牌。完整路线导出前从原生部署根重放，
独立比较终局完整状态和RNG。超出步骤限制不导出；后续结束回合遇到未支持机制
时，另存绑定原生归档及部分预测哈希的`.unsupported.mt2f`，供隔离原生校准。
已有输出及已有失败边界均拒绝覆盖。完成的模型败局仍是败局；模型胜局仍须
原生源局、独立正常重放和存档推进验收。

第十八组第七战的34步部分预测完整重放通过；补全到第六回合36输入时发现新的
Juice升级组合保护，已提取精确失败前缀，没有把薪火97的中间状态标为胜利。
