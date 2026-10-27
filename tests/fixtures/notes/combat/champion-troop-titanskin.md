# Second-tier champion troop births with enemy Titanskin

Sample25 Underlegion/Pyreborne seed1860103187 Ring5 release268 refuses an
EndTurn after12 legal inputs. The preserved request starts at unchanged
deployment and reaches turn2 with champion2, primary troop cache[2,4] and
Titanskin source10. Native completes that input to turn3 and adds source16.

Read-only native observation013537-ed857cb6 records ten troop callbacks.
Three preview callbacks see temporary FunGuy18: Horde2 HP8 becomes12, then
12 becomes16; after another enemy death Horde4 HP28 becomes36. Each callback
adds exactly one health2 upgrade per Horde stack. Primary cache[2,4] never
changes in preview. The next decision has no preview18 or assigned identity;
NextUnitId remains18. The equipped champion's original LavaArmor OnHit
callback applies Pyregel to attackers and does not retain that birth.

Release270 accepts this original champion health2 aura together with one or
two original enemy-only Titanskin5 sources, including their destroyed
retained objects. It preserves the native enchantment-before-troop pass,
borrows the complete lifecycle world for writes and leaves the primary cache
unchanged in preview. Enemy-only status observers do not prevent forgetting
the unobserved cardless player birth. Other enchantments, aura signatures,
living departures and explicit summon catalogs keep their guards.

The focused checker compares all13 complete successors, forecasts and RNG,
asserts the native health/upgrade/cache trace, checks four immutable branches
and five changed champion/enchantment guards. Existing Squad Leader preview,
empty troop cache, champion identity and tier-two birth native pairs pass.
Independent frozen native replay and the subsequent actual-budget whole
battle remain separate acceptance steps.

Frozen revision3c3e1a45 independently repeats the unchanged request as
014434-3333c2e4. The complete13-input state/forecast/RNG pair passes, preserving
the source's rejected final prediction and the independently repaired one.
The unchanged Ring5 checkpoint continues under actual10000ms/two-worker
advice. This component acceptance does not assert a new whole-battle victory.

Later stronger-policy diagnosis must preserve the same policy while extracting
its native request. `extract-unsupported-endturn ... 2 --strength` now matches
`policy-battle ... 2 --strength`; the default remains tactical. On this unchanged
deployment, release271's tactical continuation examines43 roots without that
rejection, whereas the stronger continuation reaches the next composition
after15 inputs/turn3. The exported request independently replays that exact
prefix/root/RNG and reproduces the same EndTurn rejection before native work.
