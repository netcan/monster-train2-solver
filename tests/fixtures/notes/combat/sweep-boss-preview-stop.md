# 扫击击杀最后首领时取消预演

第十五组Stygian/Wurmkin，种子494122740。第二战第25输入在第四回合向底层打出
冰矛18，首领15从180降至160血，冠军Tethys有扫击和100%攻击附加法术弱点。
主局面完全一致，旧模型的预演测试随机数比原生多推进一次。

反编译`CombatManager.RunAttack`在预演的每次扫击命中后调用`RemoveDeadCharacters`，
同时保持扫击触发队列暂停。`HeroManager.RemoveCharacter`移除最后首领后调用
`StopRunningBattlePreviewCoroutine`，因此最后一次排队的攻击附加效果不执行。
0.2.166按此顺序移除临时死者，并在终止条件成立时退出，不改变真实战斗的扫击队列。

`--sweep-boss-preview-stop-native-only <source> [replay]`连续核对25个原始输入、
完整后继/预测/RNG、最后六次被动原生概率调用、真实Battle流不变和四个不变父分支。
旧反例完整保留，唯一历史差异须为测试流多推进一次。
源进程`full-battle-units-spells-and-junk-20261026-235753-94b8891a`与独立0.2.166进程
`full-battle-units-spells-and-junk-20261027-000855-908d28f3`配对通过，保留全部71次
原生预演概率调用。旧保证概率/首领预演概率配对、扫击86房间/17列车阶段及八分支、
死亡缓存出生29输入配对、第十五组第一战24输入完整胜局配对均通过定向对照。

此为机制修复证据；第二战完整胜局及后续整局通关另行验收。
