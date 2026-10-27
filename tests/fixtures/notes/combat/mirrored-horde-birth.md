# Hall of Mirrors after a cardless original Funguy birth

Sample23 Ring6 source195438-0832563c records ten supported inputs followed
by EndTurn from turn2. The old model refused the floor2 UI preview when
the original MonsterGenerousHost death effect created a cardless Funguy
under HallOfMirrors. The complete native successor retains that temporary
Funguy as identity16 after preview restoration, with NextUnitId17.

Native CardEffectSpawnMonsterOrAddStatusEffect supplies the initial Horde
stacks in CreateMonsterState's afterCharacterCreated callback, before the
room duplication phase. MonsterManager.CloneMonsterState takes its Horde
branch: add one stack, return without another identity or the successful
ordinary-clone CardMonsterPlayed notification. Release252 reuses the
existing Horde clone implementation after initialization and before the
first troop-aura room-order binding. The supported path requires the original
cardless Funguy's Horde3/4 definition, a positive Horde birth status and
the exact original HallOfMirrors owner/asset/unrestricted subtype. Unit
copy modifiers and other generated births retain their rejection.

The focused checker replays all11 complete native successors, forecasts
and RNG, four immutable branches and seven altered signature controls.
Frozen release252 at4595df36 independently replays the unchanged request in
process200824-c309fb4b. Both complete chains and the original rejection versus
the repaired forecast pass the focused pair checker. Both captures are
registered under combat/units and have dedicated Check-Models dispatch.
This component capture alone does not establish a Ring6 victory.

The subsequent Witchweave/slay capture includes a live troop aura and pins
the duplication phase before the first room-order binding (see
mirrored-troop-aura-birth.md). Its release253 ordering retains this entire
11-input independent pair and all seven scope controls.
