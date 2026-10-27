# Harvest must publish completed deaths before troop writes

Sample25 Underlegion/Pyreborne seed1860103187 Ring5 release271's actual10000ms
advice wins with Pyre79 after31 plays/nine EndTurns. The unchanged source
015537-9ca20866 retains nine raw differences: room/exchange stages26/27,
42/43 and52/53, plus three combat phases. They are exclusively completed
ordinary enemy statistics listeners on Heavy8/Titanskin10, Heavy15 and
Heavy19/Titanskin21. Complete EndTurn successors already match.

CharacterState.CheckForDeath awaits deathSignal and characterRemovedSignal,
then RemoveSignals clears all listeners for a final death. The model clears
these flags, but PhysicalHarvest runs OnTroopAdded with a stale retained world.
ImportEnchantmentRoom then restores the old listener. A temporary diagnostic
stack trace confirmed this exact path; it is not retained in production.

Release273 publishes the current departed actors before TroopChanged borrows
the world. No comparison or captured counter is normalized. The focused check
preserves and isolates all nine historical differences, compares all78 raw
room stages/17 train phases/40 actual successors and published forecasts,
replays the continuous winning route and checks four immutable room branches.
Original three-source champion, Squad Leader preview and partial destruction
frame pairs pass. This diagnostic source is not an accepted campaign win;
fresh native advice and independent normal reward replay remain required.

Frozen revision3331413d independently replays the exact40 inputs in process
021157-cba7ae17: native victory, Pyre79 and all four failure counters zero.
The frozen actions use an accepted native deployment anchor whose complete root
is byte-equivalent to this source's root; the historical nine-counter archive
is unchanged. All40 native roots/successors/forecasts/RNG and the new raw stages
are paired explicitly. This is a repaired diagnostic replay, separate from the
concurrent actual10000ms/two-worker source and its normal reward replay.
