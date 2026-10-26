# 恢复自然档案时同步月相采集

固定随机序列第十四组Underlegion／LunaCoven，种子934692550、Covenant0。
从自然第一战检查点恢复，0.2.138实际10000ms玩家建议23输入零损获胜，但旁路
Probe把18次出牌和5次结束回合全部标记为Moon phase unit/card effects。
玩家建议自身保留25次发布及完整胜路，问题不在月相效果模型。

Probe与Advisor编译了各自的AdvisorRuntime静态字段。恢复档案没有开局氏族覆盖
环境变量，Probe的PlayerSession仍为false，因而未启用自然月相采集。现额外读取
明确的MT2_PROBE_PLAYER_ADVISOR=1；仍执行同一原生签名和模型校验，不去除拒绝。
真实游玩已有PlayerSession=true，该修复针对旁路验收进程。

修复后源局及正常奖励独立复放均23输入／薪火80零损获胜，9列车阶段，完整状态、
预测及RNG一致，四项失败计数均0。原始拒绝档案保留；专用检查要求修复前后玩家
每步根、输入、后继、完整预测和评分不变，并检查Probe捕获真实月相mask。
仅第一战通过，后续自然战斗继续推进，不计整局通关。

针对性入口：`scripts/Check-Models.ps1 -Fixture player-resumed-moon-capture-counterexample.mt2f`。
