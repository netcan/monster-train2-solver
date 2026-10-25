# Champion-preserving Stygian/LazarusLeague Ring6 root search

Seed972508374, Covenant0, unchanged natural sixth-battle checkpoint:
save SHA256 `441c387bc484a97187d5ab50b9950787eb51e5b190776ba19431b424dee5d1f4`.
The old default10000ms route loses after56 inputs. Its historical three combat
mismatches and one card-cycle refusal, and their independent repaired replay,
are preserved in [the native pairing calibration](../bosses/natural-angel-pair-cache.md).

With0.2.58, a30000ms whole-battle root search finds50 inputs,41 card plays and
nine EndTurns, terminal victory at Pyre59. It uses two workers and256 shared
nodes (22.73s search). Independent immutable successor/forecast/RNG replay
passes before export. A separate10000ms root search also finds exactly the
same50 inputs and final complete state (201 nodes,10.09s soft search budget).
Neither offline search is an actual default-player publication.

The exported30000ms candidate is actually executed by the player/probe in a
native game process with the committed0.2.58 payload from `4d67d918`. It wins at
Pyre59 in271.02s, with80 room stages,18 card cycles,17 train phases and187
independent card-mask contexts. All four failure counters are zero, frozen
execution publishes no advice and searches zero times. It preserves the
original save, difficulty, clans, deck and natural encounter.

Native execution was followed by a separate game process replaying the exact
native source inputs before normal rewards/map progression. The pair check
requires both terminal wins and identical roots, successors, complete forecasts
and RNG. This is offline-root-selected evidence, not a default-budget player
search win, a global optimum or an eight-battle campaign-clear claim.

The second process also wins at Pyre59 with all four failure counters zero.
Normal rewards, healing, merchant purchases and map choices reach Ring7,
Pyre79, with save SHA256
`c6be95a6ec102e831d2fa4799b9810f7b733413cbc3ed15dbd78e0931f097182`.
Both native17-phase train chains match. The preserved source/replay archives
contain the same50 frozen inputs and no advice publications.

```powershell
./scripts/Check-Models.ps1 -SkipBuild -Fixture @(
  'full-battle-random-campaign-05-ring6-root30000-keep-champion-search.mt2f')
```
