# 满月阿基翁吸取狂怒变体出生

第十六组Awoken/LunaCoven、种子1168994391正常第三关首次结束回合，
月相从新月变为满月后才创建Boss_Level3_Arkion_SiphonRage。
0.2.171拒绝这个月相出生签名，导致整个部署规划不可用。

0.2.172仅增加这个无月相角色回调、无出生升级的原版变体在满月出生的签名。
原生创建分配身份5，攻击7、血量425；隐藏fullmoon1状态与字典/注册表同时存在，
原始mask4加入满月位2得到6。随后初始动作落在上层物理槽位。
独立进程复演同一个首次结束回合，两份创建组件及完整后继/预测/RNG相等；
四个并发分支父状态不变，新月、其他有效首领变体和月相回调控制保持拒绝。
已有Fleshweaver满月出生配对对照通过。整场胜利仍须另行验收。

原始拒绝：full-battle-units-spells-and-junk-20261027-012713-0d25bf68。
独立修正：full-battle-units-spells-and-junk-20261027-013036-602a106c。
四份原件归档于bosses/outer，可执行
`scripts/Check-Models.ps1 -Fixture player-arkion-moon-birth-calibration.mt2f`。

隔离玩家驱动另外保留意外自动新搜索前的完整旧方案与最新原生根，写入
player-advice-research-N.mt2f，供后续重算反例诊断；此记录不进入产品玩家DLL，
不修改原生游戏状态或改变搜索策略。
