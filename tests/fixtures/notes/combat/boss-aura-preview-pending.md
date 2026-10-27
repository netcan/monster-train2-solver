# Ordinary boss aura pending action

Sample25 Underlegion/Pyreborne seed1860103187 Ring5 release267 stops after
seven EndTurns. The preserved component source011608-4f122ea4 starts from the
unchanged native deployment and executes the same six-input prefix.
The seventh input moves live Boss_Level5_Reanimate15 and Titanskin enchanter16
from bottom to middle. Boss HP500,Pyre13,primary enchanted target15/action0
and preview enchanted target15/action1 remain distinct; preview sync is true.
The historical model differs only by clearing that preview action to zero.

Read-only native trace011308-5b84ef87 explains the cause: during upper-room
temporary Pyre previews,SavePreview is true but target15 has Preview=false
and Temporary=false. CardEffectEnchant plans AddStatusEffect then skips this
actor without clearing nextStateAction. Subsequent primary updates preserve
the shared preview map. The later UI refresh performs no aura update that
would clear this action. Pyre does not die at this boundary.

Release268 removes the obsolete unconditional ordinary-boss action reset.
Primitive lifecycle target planning/mode checks and shared object restoration
retain the actual map. The focused checker replays all seven native successors,
full state and RNG, witnesses the one historical difference and checks four
immutable final branches. Existing plain/random persistent aura controls pass
their complete EndTurn/card transitions. Both old aura-summon/fresh-summon
fixtures also pass all five EndTurns and15 card actions each.

Fixed revisionac1216e7 independently repeats the exact request as native
012005-9d5b77b0. All seven complete successors/forecasts/RNG match, the original
wrong prediction remains preserved and the corrected forecast matches native.
The focused independent pair passes. Actual10000ms/two-worker Ring5 play now
continues from the unchanged Ring5 checkpoint; whole-battle acceptance and
an eight-battle campaign clear remain separate and unclaimed.
