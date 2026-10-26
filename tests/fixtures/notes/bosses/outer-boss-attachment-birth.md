# 外部首领出生与开场法术圣殿

第十五组Stygian/Wurmkin第三战，第5输入结束部署。Fleshweaver Spike首领8出生于
中层，原生附加`SpellSanctum_Upgrade`；旧模型只给普通敌人处理建筑出生升级，
漏掉外部首领。唯一差异为该首领和自动光环世界中的升级列表缺少一项。

0.2.167在外部首领的Covenant出生状态之后、出生通知和OnSpawn之前调用已有建筑
出生同步，再发布完整共享世界。四个并行分支、5步完整状态/预测/RNG及独立原生
配对通过。历史模型少一项升级的预测原样保留；校验仅允许这一历史差异。旧
Fleshweaver Spike、双天使出生和开场建筑完整胜局对照通过。

开场法术圣殿使用原生`Guid.NewGuid`生成独立于游戏RNG的对象身份。开发探针沿用
已验证FightClub的构造时身份绑定，增加这个确切原版升级签名，核对楼层、资产、
阵营、类型、目标开关与GUID。所有战斗值及RNG仍逐项核对，玩家Mod不包含该驱动。
原生源进程`full-battle-units-spells-and-junk-20261027-002711-a6ef4e33`与修复后的
`full-battle-units-spells-and-junk-20261027-002848-0b00a55b`保存完整观测。

定向入口：`--outer-boss-attachment-birth-only <source> [replay]`。
此组件配对不代替第三战完整胜局或整局通关验收。
