# 新蘑菇出生的月相与conduit注册顺序

第十四组第三战第25输入、第五回合，EagerConscript21在第0层召唤Funguy41。
该层已有Planetarium，新单位应先由CharacterState.Setup注册满月，再由房间挂件给予conduit5。
旧模型先加conduit后加月相；生命、层数和活跃状态相同，但状态注册表及字典槽位顺序错误。

0.2.148把出生月相初始化移到房间挂件之前；已有出生阶段回调的执行位置保持原有次序。
原0.2.147错误预测及原生状态原样保留，独立0.2.148回放25输入全部完整状态／RNG匹配。
原生注册及字典顺序为horde、cardless、fullmoon、conduit；旧错误则最后两项颠倒。
请求的历史标签写作room slay；定向检查明确验证实际新出生单位，未把初步诊断当成机制结论。

旧月相升级15出牌／5结束回合及16不变父分支通过；第二战26输入死亡出生独立配对对照通过。
入口：`scripts/Check-Models.ps1 -Fixture player-moon-conduit-birth-order-calibration.mt2f`。
