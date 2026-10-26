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

第六战实际施放推进至第8输入后，0.2.121漏掉新副本35的先天基础特性。
原生 `CardState.SetupTraitUpgrades` 会在复制永久升级时安装此特性；模型的
永久特性安装清单未包含先天。0.2.122补齐安装，继续保留费用-1和X值+1。
副本移入手牌，原牌进入消耗区，复制升级被短命替换，未递归复制。

`player-wurmkin-spike-copy-{replay-,}calibration.mt2f` 保留原始错误预测和
独立修复复放：8输入的完整后继／RNG及四个不变父分支全部一致。
既有带冰封的X费不灭复制15输入双进程回归仍通过。
这里尚不宣称该副本后续施放、清除或整场胜利，继续在整场流程中核对。

运行 `scripts/Check-Models.ps1 -Fixture player-wurmkin-spike-copy-deployment-calibration.mt2f`。
施放校准使用 `-Fixture player-wurmkin-spike-copy-calibration.mt2f`。
