# 零余能、具有 X 加成的战术候选

第十三组 Banished/Wurmkin 第六战第一回合，WurmkinSpike22 用完余能并复制
出35。副本保留先天升级的 X+1，零余能仍会施加腐化毒素；旧战术种子却按
余能为零一律跳过 X 费牌。0.2.126 保留永久／临时修正带正 X 加成的候选。

`player-zero-energy-x-policy-{replay-,}calibration.mt2f` 为相同8输入前缀加
这次出牌的独立原生双进程：9输入完整状态／RNG、四个不变父分支及仅去掉
X 加成的拒选控制通过。模型原先已能执行此牌，这次修复的是候选遗漏。

相同部署根的上层战术种子从60输入、剩余首领253生命的模型败局变为61输入、
剩余109生命；这不是胜局，也不能替代默认搜索和原生整场验收。

运行 `scripts/Check-Models.ps1 -Fixture player-zero-energy-x-policy-calibration.mt2f`。
