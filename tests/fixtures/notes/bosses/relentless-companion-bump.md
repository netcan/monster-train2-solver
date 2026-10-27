# Paid Bump restrictions and boss-controller movement

Sample20 Underlegion/LunaCoven, seed1043013037/Covenant0, unchanged Ring6
checkpoint110219-cf2a6848. The proposed65-input refinement reaches turn9's
63rd input, Hexcalation13 targeting boss11. Native keeps the paired relentless
companion on floor1. The old model moves it to floor2 because it processes
the actor's outer-boss flag before its companion flag. This invalidates the
old878HP model prediction; it is not a native terminal strategy result.

Native CardEffectBump.FindBumpSpawnPoint tests the paired companion restriction
before the outer anchor branch. It retains the ordinary blocked-attempt
callbacks. Automatic boss-controller moves and direct tower entry instead
call ForceMoveBoss and bypass that paid-card restriction. All three controller
callers now explicitly mark that operation; room attachments retain their
existing direct-entry behavior.

The original63-input counterexample123824-e7af6cb2 is preserved. Component
observation124447-2119f5a8 and independent repaired process125603-37fe2502
execute the same unchanged63-input request. Complete successors, forecasts,
RNG and input chains match. The counterexample also checks the53rd input's
automatic turn7-to8 companion movement and63rd paid-card refusal, with four
immutable branches each. Original19/20/22-case paid-Bump controls, three
companion phase boundaries and the52-input direct-tower attachment pair pass.

The intermediate release225 observation124859-019d40b3 fails its prefix when
the first repair also blocks controller movement. It is not accepted as the
independent pair and has not been installed. Release226 repairs controller
movement; release227 also marks direct tower entry. Windows remains223.
