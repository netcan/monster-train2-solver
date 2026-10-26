# 导能字典枚举顺序导致自动切层重算

第十六组第二战0.2.172原生复现：第4次结束回合后自动聚焦上层，三个导能单位
各记录六张手牌。手牌顺序17,14,15,5,10,9，原生私有字典枚举顺序则是反向的
9,10,5,15,14,17。旧模型按手牌插入顺序表示成员，严格后继匹配失败而重新搜索。
重算前的完整八步胜路、最新原生根及搜索次数都保存在永久反例中。

原生RoomStateConduitModifier.ModifiedCardTracker中，卡牌身份用于ContainsKey、
Add、Remove。字典唯一的遍历在ClearTrackingAndResetAllMagicPower：逐卡移除
各自的升级并刷新伤害/正文UI，随后Clear；不会按枚举顺序派发角色或CardUpdated回调。
Dictionary删除后重用空闲槽位会改变枚举顺序，不改变成员或上述操作结果。

0.2.173在ConduitUnitState构造时按身份排序Cards，与原有MultipliedCards集合表示
保持一致。保留每个身份、所有升级值、贡献单位及全部RNG；不修改手牌顺序，
不丢弃私有状态，也不把实际卡牌成员或资源差异当成切层。

永久反例验证原生反向顺序，复演首次结束回合及导能切层全部状态/随机数，
完整旧路线与实际根重放的七步后缀均到达同一胜局；会话搜索次数保持1。
能量、RNG、导能成员、贡献数值及实际升级伤害五类变更全部拒绝。
原始导能遗物、物理移动、月相出生及26步自然后继作定向对照。
后续干净构建原生整战继续确认该处不会重算；组件通过不等于整局通关。

原件：full-battle-units-spells-and-junk-20261027-013254-0bd34ff0/
player-advice-research-1.mt2f，SHA256
bd768682f1198362a1ca58fb036bb46fd10f68bdf5d5edbfefe0a29543f1f6fd。
入口：`scripts/Check-Models.ps1 -Fixture player-conduit-view-continuation-calibration.mt2f`。
