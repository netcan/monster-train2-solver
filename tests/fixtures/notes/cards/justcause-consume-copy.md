# JustCause 腐化／留存／消耗组合的遗物副本

第十三组第七战第三回合，JustCause17同时带原版TraitCorrupt、TraitRetain、
ReduceCostAddConsume三个升级。原生先把冠军2移至前排，再给同层3／8执行
不包含移位的私有副本，三个目标各增加Valor2。原卡仅消耗一次、PlayCount+1，
保留原牌的留存标记，NextCard不变，遗物一次计数0→1。

0.2.132只接受这个精确组合：消耗升级Cost-2／XCost+2，其他数值为0，
组合特性恰好各一个Corrupt／Retain／Exhaust。原牌正常执行自己的回调，
副本既不再次腐化，也不登记消耗或留存。改变升级身份、Cost或XCost仍拒绝。

原始拒绝与修复后独立原生复放各18输入，完整前缀／后继／RNG、队伍顺序、
三个增益、原牌一次消耗和标记、四个不变父分支均通过。旧留存19输入、
Fracture9输入及X费楼层排序9输入双进程对照通过。
这里只验证这次施放，不以该组件宣称后续每回合或整场已通过。

运行 `scripts/Check-Models.ps1 -Fixture player-justcause-consume-copy-calibration.mt2f`。
