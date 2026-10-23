# Original Angel Duo variants

The native `tests/catalogs/boss-rule-catalog.mt2f` contains all three original
scenarios and their six growth callbacks. Role birth/death definitions are
identical across variants. Runtime and player acceptance are separate.

## Loop Attack / Resolve Health

The input is the immutable five-win native rank-two checkpoint recorded in
`angel-duo.md`. A disclosed isolated route selection changes only Ring6's
scenario index and generated wave-boss identity to the original
`Level6AngelDuo_LoopAttackResolveHealth`. It runs after native save loading,
before battle-intro assets load. Native RNG remains unchanged. The authored
scenario, bosses, health, deck and original prior-win history remain unchanged;
this selected encounter is not claimed as the checkpoint's natural route.

The original Entropic PostCombat grants its Savage companion +60 current/max
health through a temporary battle upgrade. Savage's OnTrainRoomLoop grants
only its Entropic companion +8 attack. Reciprocal boss identities and original
death/Relentless routing are retained. The native rank-two births are outer
attack14/HP1500 and inner attack16/HP1350, from authored11/1250 and13/1100.

Source `full-battle-no-cards-20261023-173826-5c5c80b8` exited normally in
62.16 seconds with protected files unchanged and all four final failure counters
zero. It is a no-card loss. Five health60 phases (three cross-floor), two attack8
loops, six EndTurns,49 room stages,12 train stages, complete state/RNG and sixteen
independent immutable full-battle branches pass. Original births and battle-relic
trigger installation also pass their dedicated native boundary.

The capture waits for scheduled Unity destruction at the frame boundary, as the
player input does. The model refreshes this boss's movement speed from its current
room and statuses: native Haste speed is `max(1, topFloor - currentFloor)`, not
the speed observed before the previous casting move or aura change.

Archives:

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-angel-duo-loop-resolve.mt2f | 59589 | 7929 | 282c6bc4c4f2a2e951a36137bd7c7fc9479f4db5d046ffb3d7e8061333e2af20 |
| outer-boss-birth-calibration-angel-duo-loop-resolve.mt2f | 33422 | 3580 | c9da87ea4254b86b2e0941ee227400863b3078732ab02dde878460117eceeab5 |

Focused checks: `--angel-duo-loop-resolve-only` and
`--angel-duo-loop-resolve-birth-only`. Actual player advice and independent frozen
replay remain separate; the third Revenge/Harvest variant remains guarded.
