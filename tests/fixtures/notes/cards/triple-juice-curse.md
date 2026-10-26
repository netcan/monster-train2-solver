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

Two additional31-input observations preserve the exact30-input prefix and
cast the upgraded Spike at turn6 with3 energy. The native cast pays3, scales
to12 then triples to36 Valor and exhausts the card. Both complete native
trajectories and four detached branches match full state/forecast/RNG.

The separate full Ring3 default-budget acceptance has45 inputs, Pyre80 and an
independent normal replay into Ring4. That route casts Spike without a temporary
curse; the31-input component supplies the specific upgraded-cast evidence.
