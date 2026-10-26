# TitanCurse预览与自然第二回合

第十四组Underlegion/LunaCoven第三战，首次部署已经正确，下一次结束回合抽到TitanCurse后，
模型以`Uncalibrated preview unplayed effect`拒绝。原生CardEffectAddTempCardUpgradeToSpellsInDeck
明确`CanApplyInPreviewMode=false`，预览不得升级牌组；真实未打出结算仍须执行。

0.2.146只跳过原版TitanCurse及TitanCurse_Upgrade的此预览效果。原生ApplyEffects入口仍先缓存
所在房间的四个活角色，因此不能直接省略整条调用。状态测试还必须检查是否有可传播状态：
冠军斩杀在没有合法状态目标时不进入效果调用，不应写入冠军的房间缓存。

HeroManager保留在列车外的两个MiniFleshweaver（9、10）仍参加下一次真实月相更新，
得到newmoon和理解掩码5；这一回合新预览出生的11、16保持无月相、掩码4。
模型只校准此原版离列敌人的签名，不推广到其它离列单位回调。

原0.2.145拒绝样本与0.2.146独立原生重放各六输入，部署、回合、完整状态／缓存／RNG匹配。
定向入口：`scripts/Check-Models.ps1 -Fixture player-titan-curse-preview-calibration.mt2f`。

旧`full-battle-unplayed-card-effects`同时含Soul Savior，0.2.145与0.2.146均在另一个预览禁用
效果被拒绝，尚未宣称此旧组合通过。后续11输入自然未打出边界已确认真实TitanCurse结算
给12张合格法术加费，独立原生配对完整状态／RNG通过，见[月相空效果首领遗物](moon-null-boss-relic.md)。
