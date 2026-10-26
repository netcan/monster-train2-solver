# 普通玩家牌组的虫卵子单位定义

第十三组 Banished/Wurmkin 第六战在第三回合拒绝 KinhostVessel9：普通采集
没有收录其原生孵化子单位定义。0.2.124 从牌组单位的 authored 触发效果发现
SpawnMonster，补充 SummonCatalog 和 cardless 状态定义，并按战斗重置目录缓存。

三个归档保留旧采集的拒绝、完整采集和独立原生进程复放。只去除新增目录和
cardless 定义后，20 输入的原生前后状态及 RNG 全部相同；新模型逐输入匹配，
四个并行分支不修改父状态。KinhostVessel9 部署为单位16，生命15、孵化8。
这组证据只证明部署，后续真实孵化仍需独立原生校准。

运行 `scripts/Check-Models.ps1 -Fixture player-egg-catalog-calibration.mt2f`。
