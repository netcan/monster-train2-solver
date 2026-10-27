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
remaining birth callbacks. The supported path requires the original
cardless Funguy's Horde3/4 definition, a positive Horde birth status and
the exact original HallOfMirrors owner/asset/unrestricted subtype. Unit
copy modifiers and other generated births retain their rejection.

The focused checker replays all11 complete native successors, forecasts
and RNG, four immutable branches and seven altered signature controls.
The independent frozen native replay is required before fixture acceptance.
This component capture alone does not establish a Ring6 victory.
