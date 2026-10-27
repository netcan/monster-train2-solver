# Living treasure movement removal

Release262 removes the enemy movement rule at physical removal. Native
CardEffectDespawnCharacter sets Despawned and removes its living actor from
HeroManager; positive health does not keep that actor in the active roster.
Earlier room snapshots can omit lifecycle flags. Their shared-world Sync
therefore cannot identify a vanished treasure by its serialized health/flags.
The fix uses the actual removal operation and preserves unrelated movement.

Sample25 Underlegion/Pyreborne1860103187 Ring1 source224814-db904c12 has one
raw difference, stage20/turn2/room1: TreasureBagLvl1Gold10 despawns at1HP.
Both sides retain players3/11 with23/3HP, but the historical movement is8/10
instead of native8. Removing only rule10 makes the entire historical room key
equal the native result. All28 whole-battle successors already match.

The checker reproduces all57 room stages,13 train phases and28 successors,
four immutable parallel removals, an unexpired-counter living-treasure
control and an explicit-lifecycle control. Existing27-input despawn-frame
timing,27-input listener removal and26-input terminal retained-birth pairs pass.
Independent fixed-build source230628-49787b6f reaches the same actual treasure
removal with complete raw room/train/state/forecast/RNG and four zero counters.
Its normal frozen campaign replay230829-216572ae also wins with28 inputs and
Pyre80; source revisiond1ca6fc9 is frozen separately from later curation edits.
The historical failure is retained unchanged rather than clearing its counter.
