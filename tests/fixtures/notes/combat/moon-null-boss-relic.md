# 月相手牌刷新与原版首领空效果遗物

第十四组第三战第三次结束回合，抽到原版月相升级牌时，模型拒绝Fleshweaver Corruption遗物。
该遗物DataId为741546e7-71a9-4817-aab9-066af7c32fcf，效果恰为两个RelicEffectNull，
不参加月相或CardUpdated处理。0.2.147仅添加此原版签名，与现有遗物校验共同保留其它约束。

原0.2.146拒绝观察和0.2.147独立回放各11输入，完整后继／牌组／状态／RNG一致。
二者还证明上一批TitanCurse跳过预览后，真实未打出结算仍给12张合格法术加费。
两进程均从原第三战检查点部署，保留原始空预测、拒绝原因和相同输入。

定向入口：`scripts/Check-Models.ps1 -Fixture player-moon-null-boss-relic-calibration.mt2f`。
