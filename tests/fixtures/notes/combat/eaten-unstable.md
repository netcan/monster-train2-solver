# Final meal suppresses Unstable before OnEaten self-damage

Natural LazarusLeague/LunaCoven seed68586509 Ring6, Pyre40 input. The
release209 source092206-4b22e16b reaches turn5 and47 inputs, then loses.
Room49/top floor first consumes Heavy16's shield with Steward22, then
Pincushion7 deals12 damage. Snack17 has12HP, Eatmany1 and Unstable5;
its OnEaten grants a fresh shield and then deals20 damage to itself.

Native CombatFeederRules marks WillDieFromEaten before enqueueing OnEaten.
StatusEffectUnstableState.Explode skips a character killed from eating.
The model omitted that transient marker, incorrectly exploded the snack,
spent the new shield and cleared the heavy's last attacker. It predicted
Pyre4/turn6; the actual extra shield causes a turn5 defeat.

Release210 marks the final meal before the local trigger queue and skips
only its Unstable death explosion. Full native room/context and four
immutable terminal EndTurn/state/RNG branches now match. A synthetic
Eatmany2/self-damage control still explodes; the existing31-input Undying
revival native pair and58 ordinary explosion room stages remain unchanged.
The independent original47-input native observation is preserved; repaired
native replay and clean actual-budget campaign continuation are pending.

This fixes a false survival forecast. It does not turn the loss into a win
or establish that every feeding/Unstable combination is covered.
