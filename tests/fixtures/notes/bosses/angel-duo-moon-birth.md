# 满月双天使出生与独立遗物回调

第十四组Underlegion/LunaCoven，种子934692550，第六战正常入口。原生部署冠军8到中层，
第一次结束回合由新月变满月并创建外部首领
`Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth`。0.2.151在出生白名单拒绝该组合。
原件保存在`.probe-runs/full-battle-units-spells-and-junk-20261026-192335-136a87c7`，
同时包含两输入拒绝观察和`outer-boss-birth-calibration.mt2f`出生组件。

0.2.152只增加这个原版首领的满月出生签名。原生CreateHeroState先完成角色月相初始化，
保留原有mask高位4并设置满月位2，得到6；状态注册顺序为titanskin、fullmoon。
随后绑定现存内侧首领3，外侧首领5的EndsBattleOnDeath为false。没有月相角色回调的
限制、出生修改器、Covenant和物理槽位检查均保留。

同一结束回合还自然抽到月相升级法术，暴露三个已建模遗物的刷新误拒绝。
原生sealed RelicEffectAddTrigger只在OnCharacterAdded安装角色回调，无CardUpdated／
月相手牌刷新接口，故统一接受该效果类型并保留有序回调定义校验。真实Resolve／Loop
角色触发仍由原有模型执行。StartOfTurnPropagate使用单独的回合开始前抽牌接口；
GainBonusOnThreeCardsPlayed在回合开始清空列表、出牌时推进类型列表。这两件均复用
已有的原版严格校验，手牌刷新不重放效果或清空计数。

定向入口`--angel-duo-moon-birth-only <source> <birth> [replay replayBirth]`。
源局两输入、完整出生组件、状态／控制器／RNG通过；六个错误月相、变体、月相触发、
回调顺序、传播数值和序列能量控制均保持拒绝。独立修正进程
`.probe-runs/full-battle-units-spells-and-junk-20261026-193118-ecdf7022`的两输入和出生
组件均通过，完整状态、控制器、物理槽位和RNG匹配。四份原件已归档到
`tests/fixtures/bosses/outer/`，可用`Check-Models.ps1 -Fixture player-angel-duo-moon-birth-calibration.mt2f`
定向执行。旧满月Fleshweaver出生及月光束复制对照通过。第六战整场仍待验收。
