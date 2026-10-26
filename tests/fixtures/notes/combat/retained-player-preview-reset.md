# 上一次临时预览出生的玩家单位清理

第十四组Underlegion/LunaCoven，种子934692550，第六战第39输入，结束第八回合进入
第九回合。自然完整前缀的两个临时MonsterFunguy48/50被保留，原生UI预览重置时已
销毁，模型却仍保留存活状态与InfectoryEffect_Upgrade集合。不是队长强化增删差异；
请求文件保留定位初期的诊断标签`Retained troop upgrade cleanup differs after the ninth end turn.`。

原生MonsterManager.SetCharacterPreviewState会移除SpawnedInPreviewMode的玩家单位，
RemoveFromSpawnPoint/HideUnit/ProcessRemovals销毁对象；CharacterState.StateInformation.OnDestroy
清空升级等集合和监听器。不会正常死亡，也不恢复出生前的主状态。0.2.154在完整UI
预览重置后清理上一次临时预览遗留的玩家出生，保留4生命、3攻击、cardless/fullmoon/horde，
清空升级集合和统计监听，RoomIndex变为-1、IsDestroyed为true。敌方预览出生仍走其
已有主状态恢复路径；普通玩家单位与已销毁对象不变。

原始预测不符进程`.probe-runs/full-battle-units-spells-and-junk-20261026-194910-0c0a4c85`
包含39输入原生观察。`--retained-player-preview-only <source> [replay]`逐步比较完整状态
和RNG，并证明历史预测仅这两个保留对象不同；四个不变父分支与普通玩家、敌方和
重复清理控制通过。旧12输入房间死亡预览身份、25输入月相/conduit出生顺序配对通过。
独立修正进程`.probe-runs/full-battle-units-spells-and-junk-20261026-195714-1bc206a3`
已完成同一39输入完整配对；状态／预测／RNG及四个不变父分支均匹配。
两份原件已归档到`combat/spawn/`，定向Check-Models入口和1928项归档哈希／注册检查通过。
整场第六战仍在执行，不据组件通过计为通关。
