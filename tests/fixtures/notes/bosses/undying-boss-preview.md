# Undying boss preview and attacker upgrades

`undying-boss-preview-calibration.mt2f` imports turn 7 and the opt-in native
preview attack diagnostics from the frozen replay
`.probe-runs/full-battle-units-spells-and-junk-20261021-105845-1667aa6a`.
No captured states or outcomes were rewritten. This is a component witness
from a rejected rolling battle, not an accepted full battle or victory.

After the boss ascends, the native UI preview resolves eight Undying revivals.
The champion's OnAttacking upgrade runs between lethal damage and revival.
That upgrade's living-unit settlement omits the dying boss; omission must not
mark it physically removed. The final native attacker is Greed Dragon with
34 damage, surviving with 4 HP. Before the fix the model lost the boss during
the champion's upgrade and left LastAttackDamageDealt at 52.

The checker compares the exact native EndTurn state and gameplay RNG,
requires all eight preview revivals and the final dragon hit, and checks that
a live model branch cannot declare victory before all eight revivals. The
live branch is a model control; full native terminal acceptance is separate.
Source immutability is checked for both branches.

Run `--undying-boss-preview-only tests/fixtures/bosses/outer/undying-boss-preview-calibration.mt2f`.
Use `-PreviewAttackTrace` with `Run-FullBattleProbe.ps1` to record native
preview actor rosters from turn 7 onward without changing combat state.
