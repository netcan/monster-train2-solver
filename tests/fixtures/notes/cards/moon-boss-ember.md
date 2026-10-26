# 月相手牌刷新与原版BossEmber

第十四组Underlegion/LunaCoven正常第三战奖励取得BossEmber，第四战默认建议因
`Uncalibrated moon hand-refresh relic: BossEmber.`无法发布完整方案。
隔离请求从原生部署局面开始，部署原冠军6到中层，连续结束两次回合，第二次触发原拒绝。

原版四个接口分别处理回合能量、薪火最大生命、薪火攻击和部署能量，均没有月相或
CardUpdated回调。原生生命／攻击读保存档案的升级索引0，而不是未使用的ParamInt30／10；
此档案捕获到的实际增量为20生命／30攻击。不能在刷新月相时重新应用取得遗物的增量。

0.2.150只认可该原版ID、完整四效果顺序、无额外条件／突变器／预览调用的资源定义；
既有资源验证仍保留。原拒绝与独立修复重放各三输入，完整父状态、后继、缓存与RNG匹配，
自然第二回合手牌包含月相升级卡，能量4，薪火和资源载荷均保留。旧空效果首领遗物
11输入原生配对及真实TitanCurse升级12法术的对照通过。

定向入口：`scripts/Check-Models.ps1 -Fixture player-moon-boss-ember-calibration.mt2f`。
这只验收该组合边界；第四战整场结果另记，不据组件通过宣称通关。
