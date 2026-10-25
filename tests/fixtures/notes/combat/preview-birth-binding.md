# Preview-born player enchantment sources after frame destruction

Version0.2.91 completes frame destruction for temporary player actors created
inside preview. MonsterManager removes these births when preview resets;
their destroyed Unity references no longer bind CardEffectEnchant. Real card
kills remain deferred until the manager removal pass. Living retained preview
actors are also kept. The explicit frame-completion list still takes effect.

The previous persistent-enchantment-summons fixture failed at paid action4,
retained TrainStewardSmall12, Enchantment.Bound true instead of native false.
Its complete15-action/5-EndTurn independent policy now passes, including four
nested aura births, six bound sources, status child gold and parent isolation.

Two new native processes repeat the same controlled paid policy. Both win;
all15 paid successors and5 EndTurns, inputs and relevant RNG are identical.
The old forecasts preserve six binding mismatches; the new forecasts match
every paid/EndTurn successor. Controls verify immutable/idempotent preview
destruction, real-death deferral, a living retained actor and explicit removal.

This is a component pair, not a clean full-battle calibration. Both captures
also preserve the same four preexisting intra-combat differences:

| Collection/index | Recorded difference |
| --- | --- |
| Stages36, Room | CardRegistry28.RawPlayedRoomUnitIds.Length |
| Stages37, Exchange | Enchantments.Rooms0.Units1.Triggers4.Effects0.Enchantment.State.PreviewTargets19.IsEnchanted |
| Stages40, Exchange | SpawnPoints.Groups.Length |
| TrainPhases8, Combat | Train room unit states differ |

The original mismatch count is10; the corrected count is4. The focused checker
requires those four open records to remain unchanged, checks every input and
EndTurn against both processes, and reports the remaining stage gaps explicitly.
It does not treat the new captures as zero-mismatch full battles.

```powershell
./scripts/Check-Models.ps1 -Fixture player-preview-birth-binding-calibration.mt2f -SkipBuild
```
