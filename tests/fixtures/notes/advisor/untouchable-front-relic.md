# Front-room health relic skips Untouchable

Sample16 Ring6, turn6, input39 ends the turn with Untouchable Sentient(unit2)
in front of FaeSunsetter(unit8). Native
RelicEffectModifyFrontInRoomCharacterMaxHealthEachTurn uses TargetHelper's
FrontInRoom collector, which skips Untouchable. Its EmblemOfExiles_Upgrade
therefore adds15HP to the Fae and its spawner card, instead of the Sentient.
The old forecast incorrectly produced205/109HP rather than190/124HP.

The original39-input counterexample, an exact38-input prefix plus EndTurn,
and an independent repaired replay match full state, forecasts and RNG.
Four concurrent detached branches preserve their parent. The earlier two-floor
native front-only/repeated-upgrade/owner-ablation fixture also passes.
This does not establish a complete Ring6 victory.
