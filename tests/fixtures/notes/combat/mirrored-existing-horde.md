# Hall of Mirrors when spawn-or-status selects an existing Horde actor

Sample23 default Ring6 attempt201845-f99215bb strictly matches its first13
inputs, including the previously corrected cardless birth. Input14 plays
EagerConscript30 into room0 with Funguy10 at two Horde stacks. The original
CardEffectSpawnMonsterOrAddStatusEffect explicitly invokes both sets of
room spawn modifiers even when it adds status to an existing target.
HallOfMirrors therefore adds another stack after the card's own stack.

Isolated source202235-d1386191 preserves the complete historical difference:
release253 predicts three Horde/9 attack/13 current HP/16 maximum HP/four
troop-aura upgrades; native has four Horde/12 attack/18 HP/21 maximum HP/five
aura upgrades. No extra unit identity is allocated. Release254 routes this
existing-target callback through the already calibrated Horde clone path
in both spell and triggered effect dispatch. It requires the original
Funguy3/4 definition and exact original unrestricted HallOfMirrors signature;
ordinary actor copying and unknown modifiers remain guarded. Rooms without
active copying keep their original path.

The14-input complete native successor/preview/RNG chain, four immutable
branches and four altered-signature guards pass. The previous seven-input
troop-aura birth independent pair also passes. Independent frozen replay
is required before accepting the new pair. The current boundary exercises
the spell path; later natural triggered uses require their own full-state
checks. This is not a Ring6 victory or a default eight-ring clear.
