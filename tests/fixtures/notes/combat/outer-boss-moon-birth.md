# 满月下的Fleshweaver外部首领出生

第十四组Underlegion/LunaCoven第三战在首次结束回合后，月相已切换满月，
Fleshweaver Corruption此时才由原生Setup创建；原模型拒绝任何月相追踪下的外部首领出生。
0.2.145仅放行此原版首领、满月、无月相回调的明确签名。

原生出生边界显示：共享身份分配5，尚未进入任何楼层，fullmoon隐藏状态与mass5均已存在；
注册顺序为mass、fullmoon，理解掩码保留原始位4并加入满月位2，得到6。
随后原生初始动作选择已有友军的楼层，出生状态、整个战场及所有RNG均需相等。

原0.2.144拒绝观察和0.2.145独立重放各两输入：冠军部署、首次结束回合。
两套创建／初始动作组件全状态比较及整个后继/RNG校验通过；旧无月相Fleshweaver出生对照通过。
此边界不推广到新月、其它首领、出生修改器或有月相回调的组合。

定向入口：`scripts/Check-Models.ps1 -Fixture player-outer-boss-moon-calibration.mt2f`。
