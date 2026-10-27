# Native Underlegion champion inherited troop aura

Natural sample23 Underlegion/LunaCoven seed47305607 starts with champion3's
Underlegion1RegenOnHarvest1 upgrade. Release239 rejects every action with
Upgrade room modifiers. A native one-input champion birth at
full-battle-units-spells-and-junk-20261027-170948-c7e3bbcf captures30 attack,
20 health and an inherited all-friendly-unit troop aura. Its native upgrade
adds1 health per horde stack and its target cache initially contains the hero.

Release240 recognizes only that exact upgrade identity/stat/lifecycle signature
and validates the native room modifier and nested upgrade definition. Unit-card
birth inherits the aura; ordinary live-unit upgrade/removal retains its guard.
The existing aura engine now treats SubtypesData_None as an unrestricted subtype
and captures a world for cards carrying the known upgrade. Nested aura upgrades
retain their owner identity; stacked/incompatible inherited auras stay guarded.

The original rejected birth now replays full state/forecast/RNG, with eight
immutable parallel branches and nine changed-signature controls. The independent
repaired native birth (171826-09091baf) also replays fully. The checker proves
that the newly captured automatic world is the sole old/new capture difference;
runtime comparison keeps every new field. Both immutable archives are curated.

The default10000ms/two-worker sample23 Ring1 now passes two independent native
wins:38 inputs/31 card plays/7 EndTurns, Pyre80,13 train phases and complete
states/forecasts/RNG, with all four failure counts zero. Natural EagerConscript
and Harvest increase the troop from1 to19 stacks; maximum health progresses
5->10->...->95 with one owner-identity +1HP upgrade per stack. Original Squad
Leader birth/growth/loss/departure and last-troop regressions also pass.
This first battle does not yet establish a campaign clear; new-aura loss and
combined enchantment order remain separate boundaries unless witnessed.
