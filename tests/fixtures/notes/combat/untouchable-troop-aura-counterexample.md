# Untouchable troop-aura counterexample

Random campaign 25, Underlegion/Pyreborne, seed 1860103187, rank 0. The
0.2.393 player advisor used a 10-second, two-worker search from the isolated
eighth-battle checkpoint. After 56 decisions, the native action attached
Stasis Crystal to a cardless MonsterFunguy with four horde stacks, a tier-three
champion troop aura and Fight Club. The native unit changed from 19/28 HP to
16/16 HP; the model retained 19/28 HP and the aura upgrades. The native test
stopped on this mismatch, before a terminal outcome. It was not a win or an
independent replay at that point.

The 0.2.394 guard rejects newly untouchable horde targets already tracked by a
living troop-aura owner. It does not model the removal or claim that all
targetability changes behave alike. A focused test retains the original native
counterexample, checks rejection and parent immutability, and checks the
preceding observed action. An independent 0.2.394 native process reproduced the
same action from 55 frozen inputs: every prefix successor and RNG matched, and
the final player-captured state and RNG matched the original player capture.
The development probe has a separate card identity mapping and is not directly
compared to the player capture. The model still rejects the boundary; narrower
calibration is required before it can become a supported transition.
