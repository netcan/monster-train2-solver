# Titanskin fully soaked hits still trigger Revenge

Sample24 Banished/Hellhorned1122120878 Ring5 release260 reaches40 valid
inputs, then diverges after EndTurn41 from turn7 to8. Boss_Level5_Titanskin18
starts with40 Titanskin and900 HP. Basher's Trample and three Steward attacks
are fully soaked, but the original boss's OnHit removes one Titanskin each.
Native keeps900 HP and36 layers; the old model keeps900 HP and40 layers.
The first exchange is40->38; the following two exchanges are38->37->36.

CharacterState explicitly runs OnHit when damageToApply, damageBlocked or
titanSkinSoaked is positive. The model counted only the first two. The fix
tracks soak separately and includes it in that condition. Titanskin remains
passive: it does not itself consume stacks. Zero input damage still does not
trigger Revenge, partial soak triggers once, and no-OnHit units retain skin.
Native last-attack damage statistics exclude Titanskin soak, so it must not
be merged into damageBlocked. The full-state historical witness also retains
the newly triggered callback flag and the subsequent preview's49->52
last-attack statistic; every other field must match after this normalization.

Original isolated source221741-85c691ac retains all41 native inputs and the
supported-but-wrong prediction. Its request preserves an early corruption
hypothesis; the native comparison identifies soaked-hit Revenge as the cause.
The focused checker replays all41 complete successors/forecasts/RNG, checks
four immutable parallel branches, four hit controls and the existing passive
Titanskin shield/armor/piercing/magnitude controls. The separate11-input native
floor-transition-piercing pair remains passing. Release261 fixed-build source
222744-4d910749 independently repeats the same41 inputs and passes the full
native pair. The complete fifth battle remains pending.
