# 已用完的单位法术复制遗物

第十三组Banished/Wurmkin第六战的离线分支在第三回合拒绝LiquidCourage15。
同回合SingleToTeamMonsterSpellTargeting已触发，实际不会再次复制法术，
但0.2.122在判断次数前就检查了尚未校准的复制效果与Consume回调。

0.2.123先应用原生条件门控。次数已用完时，普通法术仍执行其护盾1、勇武4
和消耗；不会执行私有复制或额外队友施法。次数未使用时，未校准复制仍拒绝。
此修正不宣称LiquidCourage主动复制已支持。

`player-spent-monster-spell-relic-{replay-,}calibration.mt2f`保留原始拒绝
和独立修复复放：19输入的完整后继／RNG、四个不变父分支及主动复制拒绝
控制通过。既有JustCause返回牌主动复制19输入原生双进程回归通过。

运行`scripts/Check-Models.ps1 -Fixture player-spent-monster-spell-relic-calibration.mt2f`。
