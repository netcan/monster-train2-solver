# 带先天升级的 WurmkinSpike 复制

第十三组 Banished / Wurmkin，种子1184964217，第六战0.2.120在零输入处
无法生成建议：牌组中的WurmkinSpike22同时带原始先天升级
`TraitInherentSpellReduceCost`（费用-1、X值+1）与 `CreateSpellCopyWithPurge`。
旧复制校验未识别这张牌的两个原生特性，连无关的部署动作也被拒绝。

0.2.121识别精确卡牌ID、消耗特性、按实际费用缩放的6点腐化毒素特性，
以及先天升级。先天只参与开局抽牌，不改变施放后复制时的可复制性。
其他升级数值或特性签名仍不放行。

`player-wurmkin-spike-copy-deployment-{replay-,}calibration.mt2f` 是两个独立
原生进程对原始部署拒绝的观察，保留零前缀、完整后继／RNG和四个并行不变
父分支。四个原生缩放参数变更及一个先天X值变更控制保持拒绝。
既有X费不灭／冰封复制的15输入独立原生双进程回归通过。

当前证据只验收部署解除拒绝；实际施放、复制继承和清除行为继续在第六战
原生流程中核对，未据部署成功推断法术执行或整场胜利。

运行 `scripts/Check-Models.ps1 -Fixture player-wurmkin-spike-copy-deployment-calibration.mt2f`。
