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
`--angel-duo-loop-resolve-birth-only`.

Actual player acceptance now also passes in the same explicitly selected original
scenario. Source `full-battle-units-spells-and-junk-20261023-175319-4a01cfb1` and
fresh frozen replay `full-battle-units-spells-and-junk-20261023-180641-b79a4682`
both exited normally (799.19s/495.95s), retained protected files and finished with
all four failure counters zero. Both are losses. Each has49 inputs,40 card/ability
plays,9 EndTurns,81 room stages and18 train stages. The source published51 player
recommendations with51 mandatory fresh-root checks,428 pure captures and zero
purity/publication failures. The replay executes those frozen inputs; it does not
claim new advice publications. Original checkpoint history, every root/successor,
full forecast/state/RNG and actual player UI pass the dedicated pair checker.
The2-turn/1500ms soft budget includes the atomic first-turn completion fix.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-player-angel-duo-loop-resolve-search.mt2f | 138751 | 20873 | 4342fa60ebd22faca90e79ed31c3089e16a22a9f7f27635d2c912252c075a4f4 |
| full-battle-player-angel-duo-loop-resolve-replay.mt2f | 134480 | 20610 | 3c6fe09f52d3b3021ec185b6353dd31c87a48a0d52a68b5dd3bfe8da034b2d33 |

Focused pair: `--player-angel-duo-loop-resolve-pair-only`.

## Revenge Attack / Harvest Health

The same five-win checkpoint explicitly selects the original
`Level6AngelDuo_RevengeAttackHarvestHealth` scenario without asset/RNG changes.
Source `full-battle-no-cards-20261023-175329-3091e16d` exited normally in72.67s,
protected files unchanged, all four failure counters zero. It is a six-EndTurn
no-card loss with51 room stages and12 train stages. Original paired births,
full state/RNG and sixteen independent immutable no-card chains pass. It has no
positive growth callbacks and is not used as growth evidence.

Unmodified fixed-policy source
`full-battle-units-spells-and-junk-20261023-175953-16a6ea54` exited normally in
342.32s, protected files unchanged and all four counters zero. Native Revenge
grants the Entropic companion ten +1 attack increments, four across floors;
the Savage source is excluded. Full native actions/turns/rooms/train state/RNG
are checked separately from the no-card full-chain branches. This source has no
Harvest witness. Its own actual advice/replay pair remains separately required.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-angel-duo-revenge-baseline.mt2f | 57889 | 7573 | 5f54d63ad1dc17d4b2389fca9401a0016da7f7d56d684f667d56eae61f3a33df |
| outer-boss-birth-calibration-angel-duo-revenge-harvest.mt2f | 33587 | 3574 | 82902be98b33ed0f9a16e8d3ec627fa4825ac6d45db8e92df15359e0c3443d73 |
| full-battle-angel-duo-revenge-native-spells.mt2f | 101757 | 16413 | 464a03ca70d8e29d4d3773131bc3e6ac602f7b5ca6eede3977e91aad7bb14a2b |

Focused checks: `--angel-duo-revenge-baseline-only`,
`--angel-duo-revenge-harvest-birth-only`, `--angel-duo-revenge-only`.

Positive cross-floor Harvest is now calibrated in source
`full-battle-no-cards-20261023-182347-a72acb2f`. A disclosed native10000-damage
call kills one original wave minion on the outer boss's floor while its Savage
companion is on another floor. No target health setup, boss stat, deck or authored
asset is edited. Native removal grants only the remote companion +50 current/max
health. Complete damage/removal state/RNG and sixteen immutable branches pass.
The same control is explicitly simulated inside six original no-card EndTurns;
all51 room stages,12 train stages, full battle/RNG and sixteen complete branches
match. The process exited normally in62.39s, protected files unchanged and all
four counters zero. It is a controlled loss; ordinary player advice is separate.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-angel-duo-controlled-harvest.mt2f | 58756 | 7634 | e680b94b8dcdc50d329f1902a305a9202958fa158b9063571cbeff59117589c5 |
| angel-duo-harvest-calibration.mt2f | 33618 | 3622 | 1fed2fb1c548c3675f1381b8eaa4f0717c05106bbf0218e9379d16eaea4e00d1 |

Focused checks: `--angel-duo-harvest-death-only` and
`--angel-duo-harvest-death-battle-only`. All three exact original Angel Duo
definitions now reach the player gate and have separately accepted player pairs.

Actual Revenge/Harvest player source
`full-battle-units-spells-and-junk-20261023-182706-66704cdd` and fresh frozen replay
`full-battle-units-spells-and-junk-20261023-184156-2a4b4969` both pass. Each executes
56 inputs,45 card/ability plays and11 EndTurns, with93 room stages and22 train
stages. The source publishes58 recommendations, each with a fresh full-root
check;484 captures have zero purity/publication failures. UI and every full
forecast, root, successor and RNG match the independent replay. Both processes
exit normally (887.75s/603.77s), preserve protected originals and report all four
failure counters zero. Both are losses at the disclosed2-turn/1500ms soft budget.
These actual player inputs include no controlled Harvest damage or boss HP edits;
the separate positive Harvest control above retains its own boundary.

| Archive | Bytes | Nodes | SHA256 |
| --- | ---: | ---: | --- |
| full-battle-player-angel-duo-revenge-harvest-search.mt2f | 147866 | 22325 | 13d3c64cf1b20701ee9c9f45d62dcb19840a3b3f13976da27fefc3acf4a2c5ae |
| full-battle-player-angel-duo-revenge-harvest-replay.mt2f | 143214 | 22026 | 2759e0cc2d4e1b3dd092344b4da04101ede89e40633e8725ebd1a0cda87cc29e |

Focused pair: `--player-angel-duo-revenge-harvest-pair-only`.
