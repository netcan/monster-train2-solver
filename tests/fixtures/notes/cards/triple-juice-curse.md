# TitanCurse cost upgrade on triple Juice Spike

Sample18 Ring3 release184 fails its turn4 search after22 native inputs when
a future TitanCurse left in hand adds its temporary spell-cost upgrade.
Release185 keeps unsupported signatures as rejected branches; release186
admits the independently captured native cost payload on this specific owner.

The two30-input native observations preserve29 natural prefix inputs and then
end turn5 with TitanCurse unplayed. At turn6 Spike18 returns to hand with one
temporary TitanCurse_Upgrade (f37fb903-28d6-4d49-b50e-9226f4854816), cost+1,
zero other stats and no traits, triggers, ability or external interactions.
The historical source has no model prediction at the last boundary. The fresh
release186 replay has one; both match full state, forecast and RNG across all
30 inputs. Four detached parallel EndTurn branches match and preserve their
parent. Unknown upgrade ID/asset, cost+2 and damage+1 remain unsupported.

This calibrates the cost-upgrade boundary and resulting hand. Actual casting
of the upgraded Spike and a complete battle win still require campaign evidence.
