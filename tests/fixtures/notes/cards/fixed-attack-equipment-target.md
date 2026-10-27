# Fixed-attack equipment target strategy

The natural LazarusLeague/LunaCoven Ring5 winning route (release208 native
profile full-battle-units-spells-and-junk-20261027-085458-372d7f6f) equips
Irontongue on the6-attack/two-strike FaeMeddler at input14. Native attack
becomes30 with two strikes and Sweep. Replaying the same complete native
root with the previous tactical seed instead chooses the45-attack champion,
lowering its attack to30. The focused pre-fix check reproduces this choice.

Release209 counts the fixed attack's gain or loss across the host's strikes,
replacing its ordinary damage contribution in the equipment estimate. This
uses captured upgrade/status data and the native-calibrated attack getter;
it does not depend on card/character names or prune other legal targets.
Existing fixed-attack stacks or multiple fixed-attack upgrade entries retain
their previous estimate. This remains a heuristic, not a battle outcome or
damage lower bound. Public win/Pyre ordering and effect simulation stay intact.

The corrected seed chooses the native two-strike host; the full successor
and RNG match, four parallel choices leave their parent immutable, and the
existing16 targeted-effect cases, four targeted-ability team controls and
front-damage controls pass. A new default-budget victory needs separate
complete native acceptance.
