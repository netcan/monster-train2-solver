# 腐化版 Penitent's Bloodsuit

第十三组第七战第四回合，带原版TraitCorruptwIcon的Bloodsuit20可对敌人14施放。
0.2.134接受其精确卡牌身份与单个可移除零参数腐化特性，沿用现有前置回调：
楼层腐化0→1，能量保持5，原牌PlayCount加一次、NextCard不变，从手牌进入
Standby并绑定敌人。敌人的生命、攻击和120护甲不因装备动作变化，正确安装
来自装备20的OnShift自身30伤害回调；此组件没有提前触发该回调。

原始拒绝与修复后独立游戏进程各19输入完整前缀／后继／RNG通过，另核对
四个不变父分支和八个签名拒绝控制。原AmbientCharge24输入及Diadem32输入
独立原生配对对照通过。组件不作为后续移位或第七战整场胜利的证据。

运行 `scripts/Check-Models.ps1 -Fixture player-corrupt-bloodsuit-calibration.mt2f`。
