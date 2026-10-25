# Character Slay before dying-enchanter updates

Random10 Banished/Wurmkin Ring8,seed1895012796,Covenant0,continues the
unchanged checkpoint documented in[retained Sentry targets](retained-sentry-target.md).
After0.2.98 closes the input11 target-state difference,the actual default
run reaches31 inputs and stops at the fifth-turn EndTurn.

Firebrand23 has51 base attack,Quick and Silence when it kills the front
EnchanterT3_Silence. The model removed the dying aura before character Slay,
incorrectly enabling BloodthirstyBlade30's one-attack permanent growth.
Native CharacterState.Attacking runs OnKill first; its damage coroutine
runs hit callbacks,then CheckDeathCardTriggers calls TryUpdateEnchantments.
The dying aura therefore still blocks this character Slay. The aura is gone
by the final state,but base attack remains51 rather than52.

The model now updates the dying enchanter at that later boundary,before card
kill triggers. The original125.20s and independent corrected139.83s native
processes match all31 complete states and RNG. The original extra sword and
host upgrade remain preserved as a counterexample. Eight parallel EndTurn
branches preserve the parent. Existing native Sentry,preview binding,and
enchantment world/combat callbacks pass focused regression. This component
pair is not a full Ring8 victory. Actual default0.2.99 advice from the same
checkpoint finishes in defeat after41 inputs (33 plays/eight EndTurns), with
all complete successors/forecasts/RNG matching and all four failure counters
zero. The full loss is archived separately as
`full-battle-random-campaign-10-ring8-release099-budget10000-loss.mt2f`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --slay-enchantment-order-only `
  tests/fixtures/combat/damage/player-slay-enchantment-order-calibration.mt2f `
  tests/fixtures/combat/damage/player-slay-enchantment-order-replay-calibration.mt2f
```
