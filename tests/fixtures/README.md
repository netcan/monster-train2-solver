# Native regression fixtures

`full-battle-spawn-status-relics-egg-hatch.mt2f` exercises unchanged
`EggHatchRemoval` on two authored Egg-subtype units with 3 starting Hatch.
Each native birth removes all Hatch and gains 30 Armor from that relic, for 35
total with the separately acquired original +5 Armor relic. Twelve native birth
phases, 55 combat stages,
18 actions and six EndTurns match the model; 128 serial/parallel search states
agree. The muted Instant battle wins with zero capture failures, mismatches,
unsupported transitions or pending records. The 37,006-byte binary has 5,668
unique nodes and SHA-256
`e4758089f069985953bd7b425ad61e7ca6f524a972d1fb20f04a4117e193a4d4`.
Natural Egg transformation is not covered by this carrier.

`full-battle-mix-unstable-explosion.mt2f` plays unchanged Mixology and its
Unstable/Frostbite BlastInfusion on a Train Steward. At the first ordinary turn,
the probe adds 12 original Frostbite stacks so the holder dies while two native
Heroes share its room. The Exchange leaves the guard at Armor 17 and the mage
alive at 1 HP; post-combat Frostbite and Unstable sacrifice the steward, kill
the mage and reduce the guard to Armor 12. This separates the 5 explosion
damage from the steward's earlier attack. The muted Instant battle wins with
19 actions, 13 card cycles, seven EndTurns and 58 room stages, all matching
the model with zero capture failures, mismatches, unsupported transitions or
pending records. A dedicated witness gate and bounded serial/four-worker
search pass. The 34,630-byte binary verifies the original no-relic explosion
path; Blast Resistant, revival and relic-modified explosions remain open.

`full-battle-mix-unstable.mt2f` plays original Mixology's `01` BlastInfusion
with Unstable 5 and Frostbite 6 on a Train Steward. On the deployment EndTurn,
Frostbite does not trigger because its consume-on-trigger removal is disabled
there. Later Frostbite leaves the steward at 2 HP and Unstable sacrifices it;
the room has no enemy at that death boundary, so explosion damage has no native
target witness. The native Instant battle wins
with 8 matched actions, 13 card cycles, seven EndTurns and 54 room stages, and
zero capture failures, mismatches, unsupported transitions or pending records.
Independent policy replay and bounded serial/four-worker search also match.
The 28,989-byte binary proves this ordinary no-relic lethal path; the separate
archive above verifies actual explosion damage. Other status and relic
combinations remain open.

`full-battle-mix-01.mt2f` and `full-battle-mix-10.mt2f` cover the two remaining
first-play Mix draft combinations. Both native battles win with 63 matched
stages and zero capture failures, mismatches, unsupported transitions or pending
records. The `01` path chooses BlastInfusion with Unstable and then Frostbite,
but does not play the resulting elixir; it proves draft and upgrade routing only.
The `10` path chooses MadmanInfusion with Regen and then Reanimate and plays the
elixir. Their 19/21 actions and 36,318/37,330-byte archives match independent
replay and bounded serial/four-worker search. The played-card Unstable
calibration is recorded above.

`full-battle-mix-alternate.mt2f` selects the second original Mix recipe and
second upgrade, then plays the resulting Regen/Spikes infusion. The native
DiscardCard path leaves Infusion/Persistent in DiscardBuffer without a discard
callback or count. Its 21 actions, 13 card cycles, seven EndTurns and 63 room
stages match, with zero capture failures, mismatches, unsupported transitions
or pending records. The four draft choices remain distinct in bounded search;
serial and four-worker results agree, but the node limit does not prove a global
optimum. The binary archive is 37,869 bytes.

`full-battle-mix.mt2f` plays original Mixology twice. The first play drafts a
new elixir and upgrades it; the second adds two more upgrades to the same
recipe, moving each previous card to Standby. Native and model match 21 actions,
13 card cycles and seven EndTurns, with zero capture failures, mismatches,
unsupported transitions or pending records. The original catalog has four
elixirs and 12 upgrade choices. All four first-play two-draft selections enter
the search frontier; bounded serial and four-worker search agree on the result.
The binary archive is 37,089 bytes and its SHA-256 is in [manifest.tsv](manifest.tsv).

`full-battle-frost-channeling.mt2f` plays unchanged `PerilsOfProduction`, two
`FlashFreeze` cards and `FrostChanneling`. An enemy and a friendly unit each
carry Poison 8 before the original FrostChanneling spell; afterward the enemy
has Poison 32 and the friendly unit has none. This exercises same-room,
cross-team collection, removal, redistribution and the card's subsequent
status multiplication. The muted Instant battle wins with 62 matched stages,
22 actions and seven EndTurns, with zero capture failures, mismatches,
unsupported transitions or pending records. The binary-only archive is
32,517 bytes and has 5,316 unique nodes. The evidence covers the unchanged
FrostChanneling owner, not every other use of this effect class.

`full-battle-brain-transfer.mt2f` plays unchanged `PerilsOfProduction` and
`BrainTransfer` on a rear original Imp behind a Train Steward. The Imp has Rage
3 and Scorch 2 when sacrificed; the Steward receives both statuses and rises
from 8 to 15 attack with a temporary spawner-card upgrade. The muted Instant
battle wins with 56 matched stages, 24 actions and seven EndTurns, with zero
capture failures, mismatches, unsupported transitions or pending records.
This is original-owner evidence for `CardEffectTransferAllStatusEffectsFromUnit`
and the `LastSacrificedCharacter` target chain. The binary-only archive is
34,440 bytes and has 5,754 unique nodes.

`full-battle-transfer-attack-ability.mt2f` calibrates the ability-user target
of the native `CardEffectTransferAttackFromUnit` class on a prepared unit
ability. Six ability actions match, including a Train Steward rising from 8 to
24 attack and gaining a temporary spawner-card upgrade. The muted Instant
battle wins with 38 matched stages, 17 actions and five EndTurns, with zero
capture failures, mismatches, unsupported transitions or pending records.

`full-battle-transfer-attack.mt2f` calibrates the native
`CardEffectTransferAttackFromUnit` class on a temporary VineGrasp definition.
A front Train Steward receives twice a rear Imp's current attack; both its live
unit and spawner card gain anonymous upgrades. This is effect-class evidence,
not unchanged-owner evidence for the four catalog cards. The muted Instant
battle wins with 67 matched stages, 26 actions and seven EndTurns, with zero
capture failures, mismatches, unsupported transitions or pending records.
`full-battle-transfer-attack-physical.mt2f` repeats the front-unit transfer
with physical spawn points; its 67 stages, 26 actions and seven EndTurns also
match, including 305 physical compaction checks.

`full-battle-damage-by-units-killed.mt2f` uses unchanged `StarterImp` and
`ShardtailSacrificeImps1` definitions. The Imp is summoned behind a Train
Steward; the spell is drawn next turn and sacrifices it while damaging enemies
on the same floor. The muted Instant battle wins with 57 matched stages, eight
actions and seven EndTurns, with zero capture failures, mismatches, unsupported
transitions or pending records. The binary-only archive is 24,616 bytes.

`full-battle-damage-by-units-killed-physical.mt2f` repeats the original effect
with physical spawn points captured; its 57 stages, eight actions and seven
EndTurns also match. The archive is 28,562 bytes.

`full-battle-set-health.mt2f` plays original `RiskyOperation` twice, setting a
living player's health directly to one before its upgrade and Lifesteal effects.
The muted Instant native battle won with 51 matched stages, 19 card actions and
six EndTurns. Capture failures, mismatches, unsupported transitions and pending
records were zero. Its binary-only archive is 29,333 bytes in `manifest.tsv`.

`full-battle-choose-discard-persistent.mt2f` keeps original `ScrapMetal` after
`FoundryLine` and verifies that it remains in hand over six nonterminal turns.
Its one action, 13 card cycles, seven EndTurns and 52 room stages match native
state; six choice branches and bounded serial/four-worker search pass with zero
unsupported transitions. The 22,534-byte binary is listed in `manifest.tsv`.

`full-battle-choose-discard-refine.mt2f` records unchanged `FoundryLine`
consuming original Refine-3 `ScrapMetal`. The original action, six keep-one
branches, 52 stages and seven EndTurns match, with zero native differences.
Its 23,022-byte binary and SHA-256 are recorded in `manifest.tsv`. Bounded
serial/four-worker search agrees on the best score, path and replayed state;
the two modes now use 128 nodes each. The four room-play attempts
for Unplayable `ScrapMetal` are illegal, leaving zero unsupported root actions.
The search limits still leave optimality unproven.

`full-battle-choose-discard-others-policy.mt2f` records unchanged `FoundryLine`
followed by 18 policy plays. Its 55 stages, seven EndTurns and 19 card actions
match; a focused check also proves empty-floor post-combat `Stealth` decay and
room focus. The binary-only archive is 31,447 bytes; its SHA-256 is in
`manifest.tsv`.

`full-battle-choose-discard-others.mt2f` records unchanged `FoundryLine` keeping
one cost-two card and consuming four other hand cards for Forge Points and
Ember. Its 52-stage battle, seven EndTurns, five search choices and serial/parallel
search checks pass. The binary-only archive is 22,301 bytes; its SHA-256 is in
`manifest.tsv`.

`full-battle-conditional-pyre-damage.mt2f` records the unchanged
`HeavyT3_PyreDamageIfUnblocked` enemy. Its PostCombat effect deals five damage
to the Pyre on an empty floor and none when a player unit blocks that floor.
The binary fixture verifies the remote target, preview statistics and seven
exact EndTurns; the full muted Instant battle has 67 matched stages and no
mismatches or unsupported transitions.
The 30,123-byte archive has 4,792 unique nodes and SHA-256
`40416639a3ef18295a7de007e8c6c784c66cc69465c984c249967c2976635e46`.
Run `scripts/Check-Models.ps1 -Fixture full-battle-conditional-pyre-damage.mt2f`
for the focused regression.

`full-battle-self-attack-status.mt2f` records unchanged Weldling applying
three Armor to a player Train Steward after combat: its attack is six and the
effect's divisor is two. The focused check proves the original target team,
positive status change and exact EndTurn. The archive is binary only.

`full-battle-conditional-no-player-status.mt2f` records an unchanged enemy
gaining Armor after combat without player units on its floor, then keeping the
same Armor after combat with a defender present. The focused check verifies
both branches. The archive is binary only.

`full-battle-moon-phase-callbacks.mt2f` records unchanged Selene Acolyte,
Moonrise and Fae Meddler. Its focused check verifies temporary upgrade addition
on Full Moon and removal on New Moon; the complete battle also covers natural
phase advances. The archive is binary only.

`full-battle-train-heal.mt2f` records an unchanged PayOff cast at two Dragon's
Hoard, healing the Pyre from 70/80 to 74/80. All 61 native battle stages match
the model; focused checks use `--train-heal-only`, and the SHA-256 is recorded
in `manifest.tsv`. The archive is binary only.

These fixed inputs reproduce the independently modeled battle and calibration
checks. Run `pwsh -NoProfile -File scripts/Check-Models.ps1` from the repository
root. The script reads the curated fixture list in this directory.

To run the standard model-check suite against selected archives, pass one or more
names with `-Fixture`, for example:

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-units-and-junk.mt2f
pwsh -NoProfile -Command "& './scripts/Check-Models.ps1' -Fixture @('full-battle-relic-card-status-upgrades.mt2f','full-battle-relic-card-piercing-upgrades.mt2f')"
```

`-Fixture` still runs the pure model checks and all native checks that consume
the selected captures. Some native coverage gates require a representative
full-battle capture or the complete inventory. For a narrower scenario check,
call its dedicated `ModelChecks` mode directly, for example:

```powershell
dotnet run --project src/ModelChecks -c Release -- --bump-only tests/fixtures/full-battle-companion-bump.mt2f
dotnet run --project src/ModelChecks -c Release -- --room-combat-only tests/fixtures/full-battle-companion-bump.mt2f
dotnet run --project src/ModelChecks -c Release -- --bump-only tests/fixtures/full-battle-outer-boss-bump.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-mageblade.mt2f
dotnet run --project src/ModelChecks -c Release -- --mageblade-only tests/fixtures/full-battle-original-mageblade-magnitude.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-units-spells-and-junk.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-dragon-hoard.mt2f
```

The default command checks the full curated inventory. For routine changes, run
the dedicated checks for the new and directly affected scenarios; run the full
inventory at completed milestones and before delivery.

Current inventory is 319 archives: 288 accepted battles and 31 component
calibrations. Original relic evidence is 81/224 (36.16%). The Pyre spawn-status
archive calibrates the unchanged `DantesCandelabra` and `PressurizedBoiler`
effects. Both relics are acquired before battle, and the game's natural Pyre
creation adds 2 Titan Skin and 4 Burst in its first recorded birth phase. The
schema-162
`full-battle-dragon-hoard.mt2f` archive captures Alchemy changing the hoard from
2 to 4. The Instant native battle wins with 61 matched stages, 21 card actions
and seven EndTurns, with zero capture failures, mismatches, unsupported
transitions or pending records. The focused checks cover cap clamping, hoard
based gold, immutable branch propagation and rejection when active units need
resource-scaled upgrade refresh. This 33,090-byte archive has 5,405 unique nodes
and SHA-256
`d22b5e5975c8b1b1ad9795c65aa7827b3714f5403b9d0478bcad61396c5fe633`; it has
no JSON companion.

`full-battle-choose-discard.mt2f` records original `StarterScrap` choosing a
two-cost card over a one-cost alternative. The native battle and independent
model match 23 actions, seven EndTurns, five legal choice branches, two Forge
Points and two Ember; 128-state serial/parallel search and the post-choice
policy chain match. Run `scripts/Check-Models.ps1 -Fixture
full-battle-choose-discard.mt2f` for the focused regression. The archive is
binary only; its SHA-256 is in `manifest.tsv`.

The schema-164 `full-battle-remove-all-status-effects.mt2f` archive records two
marked native `ResinRemoval` casts: one removes all visible Positive and
Negative statuses, and the second removes half, rounding removals up. The
native battle wins with 50 matched stages, 22 card actions and six EndTurns;
capture failures, mismatches, unsupported transitions and pending records are
zero. Focused checks verify exact native action replay, full and half removal,
and preservation of hidden and Persistent statuses. The 30,154-byte binary has
5,012 unique nodes and SHA-256
`4f91c877366cce01dd9c84932736ff323394fc081163ab63cdf181ddcd3c98a3`; it has
no JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-remove-all-status-effects.mt2f
dotnet run --project src/ModelChecks -c Release -- --remove-all-status-only tests/fixtures/full-battle-remove-all-status-effects.mt2f
```

The schema-161
`full-battle-unplayed-card-pyre-damage.mt2f` archive leaves all five original
fixed-damage negative-trigger cards in hand: `BlightPyreDamageInHand` (1),
`ScourgeT2` (3), `TFB_EntropyScourge` (3), `DantesCandle` (3) and `ScourgeT3`
(5). Their EndTurn effects reduce Pyre health by 15 total. The Instant native
battle wins with 62 matched stages, 21 card actions and seven EndTurns, with
zero capture failures, mismatches, unsupported transitions or pending records.
The focused check verifies each captured effect and the combined damage, plus
synthetic multiple-card resolution and parent-state isolation. The 34,660-byte
archive has
5,822 unique nodes and SHA-256
`74ca21e8cca5fb78f582b46db5e3a4737054833c13374e1f3499b473b87179a5`; it has
no JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-unplayed-card-pyre-damage.mt2f
dotnet run --project src/ModelChecks -c Release -- --unplayed-card-trigger-only tests/fixtures/full-battle-unplayed-card-pyre-damage.mt2f
```

The schema-163 `full-battle-unplayed-card-effects.mt2f` archive captures the
other two original negative-trigger cards. TitanCurse gives eligible deck
spells its temporary upgrade, and Scourge_SoulSavior is natively copyable and
creates one copy in the discard pile. The muted Instant battle wins with 63
matched stages, 11 card actions and seven EndTurns; capture failures, mismatches,
unsupported transitions and pending records are zero. The focused check
validates both effect definitions, the spell upgrade and the copied card. This
31,641-byte binary has 5,000 unique nodes and SHA-256
`ab5dd7fe0a2dcab0dbce146a632c2e7def068c802e63118572c27948e082ee81`, with no
JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-unplayed-card-effects.mt2f
dotnet run --project src/ModelChecks -c Release -- --unplayed-card-effects-only tests/fixtures/full-battle-unplayed-card-effects.mt2f
```

The schema-160
`full-battle-original-card-draw-on-card-play.mt2f` archive captures the
unchanged `CardDrawOnCardPlay` and `DrawOnSnackPlayed` owners. The first owner's
`AnyCardPlayed >= 3` condition draws on five active-turn third plays, not during
deployment. The second retains its Monster / Snack filter and draws once during
deployment when a synthetic Train Steward carrier with the Snack subtype is
played. All 52 stages, 20 card actions and six EndTurns match; the focused native
check confirms five threshold draws and one Snack draw, and the 128-state
serial/parallel search checks agree. The 30,781-byte binary has 5,043 unique
nodes and SHA-256
`9faa1a94d9283a366dfe2c591dcd9e0b72b792ab516b2ac14017026aceaf574b`, with no
JSON companion. The schema-146
`full-battle-original-moon-phase-energy-relic.mt2f`
archive adds accepted native evidence for the unchanged `MoonPhaseModifyEmber`
relic: three Full callbacks add one Ember and notify; three New callbacks are
no-ops. It also verifies exact moon markers across 24 plays, seven EndTurns and
57 matched battle stages. The binary has no JSON companion. See
[relic coverage](relic-coverage.md) for its owner evidence and replay details.

The schema-147 `full-battle-original-card-attack-relic.mt2f` archive adds
accepted native evidence for the unchanged `WhelpDragonAttack_DH` relic. Its two
fixed +8 effects match Whelp and Dragon spawn subtypes; native manager returns,
temporary attack offsets and reset behavior agree on both samples. The muted
Instant full battle wins with 60 matched stages, 21 card actions and seven
EndTurns, with zero capture failures, mismatches, unsupported transitions or
pending records. Cardless birth behavior has a focused model check; the native
archive covers card-state application. This brings accepted original relic
owners to 74/224 (33.04%). The 32,904-byte binary has 5,327 unique nodes and
SHA-256 `051f518a1e7dae9e2818a5bf940651dce35afde5cbc86943e4ebcf36bc7f3208`;
there is no JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-card-attack-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-card-attack-only tests/fixtures/full-battle-original-card-attack-relic.mt2f
```

`full-battle-card-effect-kill.mt2f` records the native `KillRandomUnit` spell's
two ordered random kills, one against an enemy and one against a player unit.
The muted Instant battle wins with 60 stages, 22 card actions and seven
EndTurns. The card is played twice; one matched action removes an enemy and a
player unit from the room, with zero capture failures, mismatches, unsupported
transitions or pending records. The 31,129-byte schema-103 binary
has 5,100 unique nodes and SHA-256
`fec1eb5dec554ab93d398c51c273012073aa5b521f3a52268fa593de42221f4a`; no JSON
companion is tracked.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-effect-kill.mt2f
dotnet run --project src/ModelChecks -c Release -- --native-actions-only tests/fixtures/full-battle-card-effect-kill.mt2f
dotnet run --project src/ModelChecks -c Release -- --sacrifice-pure
```

`full-battle-card-effect-kill-subtype.mt2f` validates the native subtype check
end to end. The isolated scenario assigns a subtype to one `KillRandomUnit`
effect based on its Battle-RNG-selected target, then runs the actual native cast;
the killed Train Steward matches the captured subtype. The direct native check
also retains a matching/mismatching pair. The muted Instant battle wins with 58
stages, 22 actions and seven EndTurns, with zero capture failures, mismatches,
unsupported transitions or pending records. The schema-103 binary is 31,054
bytes with 5,040 unique nodes and SHA-256
`e80d34ba3fae893410072619056f9a29918cf235908f78cc466ef394f1c5ca5a`; no JSON
companion is tracked.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-effect-kill-subtype.mt2f
dotnet run --project src/ModelChecks -c Release -- --native-actions-only tests/fixtures/full-battle-card-effect-kill-subtype.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-effect-kill-subtype.mt2f
```

`full-battle-card-recursion.mt2f` captures `WurmkinEtchings` returning five of
six exhausted Spells to Draw through `RandomToDeck`. The native battle and
independent action replay match all 61 stages and 22 actions with zero
unsupported transitions. The binary is 36,006 bytes, contains 6,025 unique
nodes and has SHA-256
`a1c9997717e630c84e3766efd8bc1ce9ad52e3823e26bb50bdc6644eff67c4a8`; no JSON
companion is tracked.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion.mt2f
dotnet run --project src/ModelChecks -c Release -- --native-actions-only tests/fixtures/full-battle-card-recursion.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-pure
```

`full-battle-card-recursion-choice.mt2f` is a controlled native calibration of
`WurmkinEtchings` with its recursion mode changed to `ChooseToDeck`. Six
exhausted Spells are eligible and the native choice screen selects instance 3.
The battle wins with 63 matched stages, 22 actions and seven EndTurns; all
capture and replay failure counters are zero. Search emits six distinct selected
card actions from that recorded decision state, and each branch restores only
its selected card without consuming Battle RNG. The schema-103 archive is
36,004 bytes with 6,165 unique nodes and SHA-256
`7be5784f3da5bd72f18cc212651772d891a2378d096fca9edc5d70785e0e6821`; it has no
JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-choice-only tests/fixtures/full-battle-card-recursion-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-choice.mt2f
```

`full-battle-card-recursion-choose-to-hand.mt2f` is a controlled native
calibration of `WurmkinEtchings` with mode `ChooseToHand`. Six exhausted Spells
are eligible; the native selection screen chooses instance 3 and returns it to
Hand. The battle wins with 63 matched stages, 22 actions and seven EndTurns,
with zero capture or replay failures. Action replay includes the native
`TimesDrawn` and `AnyCardDrawn` updates; search emits six distinct choices and
preserves Battle RNG. The schema-103 archive is 36,391 bytes with 6,227 unique
nodes and SHA-256
`8db0d52a13c6c28aae41de5821593c7295a3b5ae606d77db069a32be31beca28`; it has no
JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-choose-to-hand.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-hand-choice-only tests/fixtures/full-battle-card-recursion-choose-to-hand.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-choose-to-hand.mt2f
```

`full-battle-card-recursion-filter-choice.mt2f` is a controlled native
`ChooseToDeck` calibration with a Spell-only `CardUpgradeMask`. Five exhausted
Spells remain eligible while one Monster is excluded; the native screen selects
Spell instance 3. The muted Instant battle wins with 63 stages, 22 actions and
seven EndTurns, with zero capture failures, mismatches, unsupported transitions
or pending records. Search emits five card choices from the captured decision
state and preserves Battle RNG. The schema-148 binary is 36,379 bytes with 6,194
unique nodes and SHA-256
`542c5bf2c4511cb3fa27549946e716bca56a7fe21428621ed3b2574ee8c0ff32`; it has no
JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-filter-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-filter-choice-only tests/fixtures/full-battle-card-recursion-filter-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-filter-choice.mt2f
```

### Random recursion with a temporary upgrade (schema 149)

`full-battle-card-recursion-upgrade.mt2f` captures native random-to-deck
recursion with a synthetic temporary +3 Damage upgrade. Exactly one returned
Spell carries the upgrade; the remaining Exhaust candidates do not. The muted
Instant battle wins with 63 stages and 22 actions, with zero capture failures,
mismatches, unsupported transitions or pending records. Action replay matches,
and the 128-state search is equal in serial and parallel runs. The archive is
36,170 bytes with 6,185 unique nodes and SHA-256
`6fcfa4e9fcdda9ab4b71e24dcc52301820c8f949f88d5c22320721016a9ceccd`; no JSON
companion is tracked. Inventory: 266 archives, comprising 235 battles and 31
component calibrations.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -CardRecursionUpgrade -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-upgrade.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-upgrade-only tests/fixtures/full-battle-card-recursion-upgrade.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-upgrade.mt2f
```

### `RandomToHandWithUpgrades` recursion (schema 150)

`full-battle-card-recursion-upgrade-hand.mt2f` verifies the dedicated native
`RandomToHandWithUpgrades` mode with a full hand. It selects two of six Exhaust
Spells and applies temporary +3 Damage to both. One enters Hand; the other stays
in Draw after native `DrawSpecificCard` rejects the full hand. This mode ignores
`DisallowOverflowToDrawPile`, even when that flag is set. `RequiresRoomInHand`
is also enabled; its positive gate checks draw count and cards in Exhaust/Eaten,
not free hand capacity. The muted Instant battle wins with 50 stages, 19 actions
and six EndTurns, with zero capture failures, mismatches, unsupported
transitions or pending records. Replay matches both destinations and upgrades;
the 128-state search matches in serial and parallel. The archive is 34,875 bytes
with 6,042 unique nodes and SHA-256
`6f4fd6829758f1eaa1e01a87f21b08b4c3079e1e1dcb46dc6d24a84570d6e255`; no JSON
companion is tracked. Inventory: 269 archives, comprising 238 battles and 31
component calibrations.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -CardRecursionUpgradeToHand -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-upgrade-hand.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-upgrade-only tests/fixtures/full-battle-card-recursion-upgrade-hand.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-upgrade-hand.mt2f
```

### Chosen recursion with a temporary upgrade (schema 151)

`full-battle-card-recursion-upgrade-choice.mt2f` captures `ChooseToDeck`
recursion with a synthetic temporary +3 Damage upgrade. The native chooser
returns the selected Spell to Draw with the upgrade. Search expands all six
eligible Exhaust cards into separate choices; each branch upgrades only its
selected card and leaves Battle RNG unchanged. The muted Instant battle wins
with 63 stages, 22 actions and seven EndTurns; capture failures, mismatches,
unsupported transitions and pending records are zero. Replay matches, and the
128-state search is equal in serial and parallel runs. The archive is 36,151
bytes with 6,180 unique nodes and SHA-256
`ab01c1b813b8790019c630f88ed03ddb14863296dbd54963700bd9d200a19482`; it has no
JSON source. Inventory: 268 archives, comprising 237 battles and 31 component
calibrations.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -CardRecursionUpgradeChoose -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-upgrade-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-upgrade-only tests/fixtures/full-battle-card-recursion-upgrade-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-upgrade-choice.mt2f
```

### Filtered chosen recursion with a temporary upgrade (schema 152)

`full-battle-card-recursion-upgrade-filter-choice.mt2f` combines the native
Spell-only recursion filter, `ChooseToDeck`, and a temporary +3 Damage upgrade.
The native selection excludes one Monster from six Exhaust candidates and
returns the chosen Spell to Draw with the upgrade. Search emits all five
eligible Spells as separate actions; each branch upgrades only its selected
card and preserves Battle RNG. The muted Instant battle wins with 63 stages,
22 actions and seven EndTurns; capture failures, mismatches, unsupported
transitions and pending records are zero. Replay matches, and the 128-state
search is equal in serial and parallel runs. The archive is 36,331 bytes with
6,207 unique nodes and SHA-256
`1a4056a0e372146d01bbb91a65c629e133cdcde003464a565a2639e3630650da`; it has no
JSON source. Inventory: 269 archives, comprising 238 battles and 31 component
calibrations.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -CardRecursionUpgradeFilterChoose -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-upgrade-filter-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-upgrade-only tests/fixtures/full-battle-card-recursion-upgrade-filter-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-upgrade-filter-choice.mt2f
```

### Chosen recursion with a replacement upgrade (schema 153)

`full-battle-card-recursion-upgrade-replacement-choice.mt2f` captures a chosen
recursion where the selected card already has a temporary +2 Damage upgrade.
The native +3 Damage upgrade replaces the prior upgrade by AssetKey and adds
`CardTraitEphemeral`. Search emits all six legal card choices; only the selected
card receives the upgrade and trait, and only the branch selecting card 3 removes
the prior upgrade. Every branch preserves Battle RNG. The muted Instant battle
wins with 52 stages, 19 actions and six EndTurns, with zero capture failures,
mismatches, unsupported transitions or pending records. The native battle later
plays card 3 and sends it to Purged through both native callbacks; predicted and
captured piles match. Native replay and 128-state serial/parallel search match.
The archive is 33,101 bytes with 5,635 unique nodes and SHA-256
`daa9034d3db6ac809ca427f0b910262051bc67bbbe1fed10c0880b9f89fea0e3`; it has no
JSON companion. Inventory: 270 archives, comprising 239 battles and 31
component calibrations.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -CardRecursionUpgradeReplacementChoose -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-upgrade-replacement-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-upgrade-only tests/fixtures/full-battle-card-recursion-upgrade-replacement-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-upgrade-replacement-choice.mt2f
```

`full-battle-relic-card-negative-unhealed.mt2f` (schema 145)
captures a temporary `UnhealedHealth=-1` upgrade installed by the native relic
manager. The upgrade remains on the played Train Steward, but the summoned unit
keeps its baseline current and maximum health. The muted Instant battle wins
with 49 stages, 28 actions, 11 card cycles and six EndTurns; capture failures,
mismatches, unsupported transitions and pending records are zero. Independent
action replay matches all 28 plays; search matches 128 states with equal serial
and parallel results. This verifies the -1 card-birth case and adds no original
relic owner to the coverage count. The binary archive is 30,548 bytes with
5,134 unique nodes and SHA-256
`aeca84685bb2c0444c669901245236ecdbd837fa283c36a2d7598d29eeabccb2`; it has no
JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-relic-card-negative-unhealed.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-card-modifiers-only tests/fixtures/full-battle-relic-card-negative-unhealed.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-relic-card-negative-unhealed.mt2f
```

`enchantment-combat-calibration.mt2f` captures two native
`CardEffectEnchant` sources growing a player Train Steward from zero to three
Horde stacks (8 to 24 attack, 25 to 75 health). Muting the sources removes two
stacks and then the final stack. The final removal takes HP to zero and removes
the unit from the live train; simulated-death statistics and
`OnTroopRemoved`/`OnAnyMonsterDeathOnFloor`/`OnAnyUnitDeathOnFloor` callbacks
match, with no physical `OnDeath`. The 41-transition replay covers 74 status
calls, 135 callback payloads and 32 immutable branches. The 13,514-byte archive
has 2,452 unique nodes and SHA-256
`1a18b97b4c4b6434bd2c9de008b9951f5550d620abb5babaf5678e3ce12b7618`; it has
no JSON companion. Shared-world drain matches native `AfterQueue` for all five
Deathwish transitions whose callbacks add Armor; other actor/card-mutating
callback payloads remain guarded. `full-battle-companion-bump.mt2f`
covers a paired Relentless
companion Boss refusal, its attempted-ascension callback and the survivor's
terminal-death handoff. `full-battle-relic-card-status-upgrades.mt2f` verifies a native
temporary card status upgrade reaches the unit summoned by that card.
`full-battle-relic-card-replacement-upgrades.mt2f` verifies an original relic
manager call replaces an existing temporary armor upgrade, keeps the native
returned/list-growth distinction, and carries the replacement through a
complete independently replayed battle. Its synthetic payload does not add an
original relic owner to the coverage count.
`full-battle-original-mageblade-magnitude.mt2f` captures the unchanged original
`DoubleMagebladeDamage` relic. Its positive Monsters-team query, all four team
masks, exact owner ablation and resulting cached coefficient pass; at that
stage original relic evidence was 71/224 (31.70%). The schema-143 binary has
21,251 bytes, 3,050 unique nodes and no JSON companion.
`full-battle-original-team-spawn-status.mt2f` captures the unchanged original
`HymnistsApplyValorOnSummon` relic. A test carrier with the native Hymnist
subtype and a same-room clone produce 33 living teammate +5 Valor witnesses
across 22 birth phases; all 13 clone operations and 32 branches match. The
muted Instant battle wins with 41 stages, 14 actions and five EndTurns, with
zero capture failures, mismatches, unsupported transitions or pending records.
The schema-144 binary has 71,648 bytes, 10,516 unique nodes and no JSON
companion; original relic evidence is now 72/224 (32.14%).
`full-battle-original-endless-return.mt2f` verifies the original More Rares
relic's Endless upgrade routes a dead carrier to Draw, preserves its temporary
upgrade and leaves `TimesExhausted` unchanged across a 23-action, seven-turn
battle with all 62 room stages matched.
`full-battle-original-forge-points-relic.mt2f` verifies unchanged `HerzalsAnvil`
adds one point to positive Forge adjustments; independent action/room/turn
replay and exact owner ablation pass.
`full-battle-room-capacity-attachment.mt2f` verifies native
`IncreaseRoomCapacity`: the played card remains active on its floor, raises
capacity from 5 to 9, and routes to `Standby`. The muted Instant battle has zero
capture failures, mismatches, unsupported transitions and pending records;
independent replay matches 52 room stages and seven EndTurns. This is card and
room mechanism coverage, not original relic-owner evidence.
`full-battle-spawn-or-status.mt2f` covers fallback birth and existing-unit
branches of `CardEffectSpawnMonsterOrAddStatusEffect` across two native skill
activations.
`full-battle-sacrifice-effects.mt2f` covers native `CardEffectSacrifice` target
selection, death callbacks, spawner exhaustion and a 23-action, seven-turn battle;
the binary archive is 30,799 bytes with 5,088 unique nodes and no JSON source.
`full-battle-relic-card-piercing-upgrades.mt2f` verifies a native relic's
IgnoreArmor trait survives temporary upgrade composition and drives card piercing.
`full-battle-generation-relic-card-status-upgrades.mt2f` combines generated-card
modifier copies with that relic upgrade: 84 generation callbacks, 36 copied
modifier cases, 107 relic-manager calls and 74 notifications match with no
differential failures. Its focused native and model checks pass; the next
milestone full regression will include it.
`full-battle-relic-card-self-purge-upgrades.mt2f` verifies temporary relic
SelfPurge routing and native suppression of IgnoreArmor and Exhaust traits
listed in the same upgrade's add/remove sets.
`full-battle-relic-card-ability-upgrades.mt2f` verifies the real
`StewardSacrificeDamage_Upgrade` relic payload gives its summoned Train Steward
the recorded unit ability.
`full-battle-relic-card-trigger-upgrades.mt2f` verifies the real
`PyreHeartSavagery_UpgradeData` relic payload gives its summoned Train Steward
the native OnKill trigger and models repeated same-definition unit-upgrade
accumulation. Its focused battle replay matches all 49 room stages and 28 actions.
`full-battle-relic-card-cast-trigger-upgrades.mt2f` verifies an installed
temporary card upgrade carries its ordered OnCast card effect through replay
and applies it on both native plays. The authored relic payload is synthetic and
does not add original relic-owner evidence.
`full-battle-relic-card-ephemeral-upgrades.mt2f` verifies temporary relic-granted
Ephemeral traits purge played spells and spells left in hand at EndTurn. Its
authored payload is synthetic and does not add original relic-owner evidence.
`full-battle-generation-permanent-consume.mt2f` verifies `CardEffectAddBattleCard`
copies a permanent Consume upgrade into generated cards across all five pile
destinations, with fresh unexhausted trait state on the new instance. Its 91
generation effects match, including 60 copied Consume cards and three unit-trigger
paths; the complete battle and 16 parallel branch replays pass.

The initial Incant model's complete 180-archive regression passes.
The threshold model's combined181 regression has also completed successfully.
The relic model's combined181 regression has also completed successfully.
The corrected checker's combined182 regression has also completed successfully.
The extended trigger-count model's complete183 regression has completed:159
battles,155 paid policy chains,four no-more-card chains and24 calibrations pass.
The original spawn-status model's complete185 regression has completed:161
battles,157 paid policy chains,four no-more-card chains and24 calibrations pass.
Subsequent ability-count and relic-birth scheduling changes require their own
regression; the frozen185 baseline does not include those changes.

`full-battle-spawn-status-relics-clones.mt2f` retains schema110 with the three
original spawn-status artifacts and unchanged ExtraSpawnTrigger. It adds13 clone
operations and22 complete birth observations:13 standalone settlements and nine
deferred phases, including exact prior/remaining queues and dispatched payloads
across the whole train. Both native recordings and independent complete battles
pass; all28 pre/post-policy states repeat exactly. The final scheduling model's
complete186 regression also passes:162 battles (158 paid/four no-more-card
chains) and24 component calibrations, with every input and frozen binary hash
audited. This baseline precedes further card-upgrade-mask model work.

`card-upgrade-mask-calibration.mt2f` retains schema2 with62 original upgrade/card
filters against668 raw card definitions,15 owned cards, one live character and
null sources:42,470 native matrix queries plus58 controlled native boundary
queries. The independent component and32 immutable parallel replays pass. The
native capture preserves full context, units, gameplay/test RNG and frame and
its complete recorded battle also passes independent policy checks. This archive
is34,127 bytes/3,087 nodes, with no JSON dependency. It calibrates eligibility;
applying temporary relic upgrades in battle remains unfinished. The full187
regression is separate from the completed preceding-model186 baseline.

`card-modifier-overflow-calibration.mt2f` retains2,240 schema1 native arithmetic
queries:1,344 numeric results and896 original minimum-integer exceptions across
eight statistics, both floor settings and offset/upgrade placements. Independent
values/exception types and32 immutable replays pass. The corrected shared
modifier function also passes all pure checks, original256 numeric calculations,
the upgrade-mask calibration and this recording's complete paid battle. The
archive is9,282 bytes/2,949 nodes with no JSON dependency. Its correction is later
than the preserved frozen187 run and requires a separate full188 regression.

The frozen187 and188 runs now have complete output audits: every162 battle
passes (158 paid/four no-more-card chains), with25 and26 ordered calibrations
respectively. All input and retained binary hashes match. Their processes are
gone; interruption discarded the terminal handles, so original exit codes
cannot be verified. These are complete check-output audits, not preserved
terminal-exit-zero evidence. The complete186 baseline retains that evidence.

The later frozen189 and190 regressions retain successful terminal status and
complete output audits:162 battles (158 paid/four no-more-card chains), with27
and28 ordered calibrations respectively. Every input's manifest size/hash and
the frozen model/checker hashes match. Durable exit codes are zero, with terminal
timestamps2026-10-10T13:14:05.0335854Z and2026-10-10T13:21:27.8389773Z.
These validate the trait and status composition versions respectively; they
precede the owned-card mask and temporary-upgrade lifecycle additions.

The new trait composition model separately matches2,134 native forced-refresh
observations:668 raw card trait seeds,15 isolated owned-card copies and384
controlled combinations, each refreshed twice with persistent results carried.
It preserves ordered first-base replacements, parameter resets, the previous
combined list's insertion bound, temporary-original-type deduplication, removed
types, temporary replacement markers and Exhaust/SelfPurge suppression. Exact
base/temporary/created instance origins and32 immutable parallel replays pass.
The muted Instant capture exits zero in97.15 seconds without changing original
files, complete context, gameplay/test RNG or frame. Its separate complete
21-play/seven-EndTurn battle passes independent initial/mid-battle policies,
ending at Pyre73; all pure checks and the existing mask/overflow calibrations
also pass. Frozen validation binaries are retained under
.probe-runs/trait-composition-final-checks:Model SHA256
60795214D56C2FD301E5CC28656A69267225FB8FE706E3AB94B6DE78817CFA73,
ModelChecks SHA256
CC85004B4D78B5B2CB01E8AEE68F387893F69B3C66336E914DF5D8052B7237BD.
This calibrates trait composition inputs/outputs, not trait callbacks, permanent
upgrade installation or temporary relic upgrade lifecycle integration. It is
later than the audited frozen188 baseline; a combined regression is separate.

`card-trait-composition-calibration.mt2f` is the byte-identical schema1 native
trait refresh capture described above:31,261 bytes/4,744 nodes, SHA256
d22122720e4f265e8f036d82ed76017e332d7629310340095e5e6124710f9680.
It has no JSON source/companion and is included in the curated binary inventory.

The subsequent card status composition model separately matches1,039 native
ordered queries:15 owned CardState.TryGetStatusEffects results and1,024
controlled original-helper two-stage merges. Groups retain both status ID and
fromPermanentUpgrade, clamp after each unchecked Int32 addition, discard zero
groups after each stage and preserve their resulting order. Purify removes
starting statuses, not upgrades; card queries have no unit9999-stack cap.
Exact source flags, ordered counts and32 immutable parallel replays pass.
The muted Instant capture exits zero in64.56 seconds with complete context,
RNG/frame and original files unchanged. Its independent complete battle passes
21 paid plays/seven EndTurns, Pyre73 and initial/mid-battle branch policies;
all pure checks and mask/overflow/trait calibrations also pass. Frozen binaries
are retained in .probe-runs/card-status-composition-final-checks:Model SHA256
077E8D1778C05A20FB302D3232E151ADD931C0E324C380FC1DAA78661D56F884,
ModelChecks SHA256
5713EEBEA689CCD398FD3E9E89CFD44F345F39E0DE388BF67133888085BFF429.
This models card queries with applyDuality=false; Duality and live unit spawning
remain separately scoped. The full189 trait-version regression does not cover
this later status-source metadata/model change. Branch-derived owned mask views
and complete temporary relic upgrade lifecycles remain unfinished.

`card-status-composition-calibration.mt2f` is the byte-identical schema1 native
status query capture described above:11,026 bytes/3,316 nodes, SHA256
f2c2a6d4da1bb317b68e5778a66f9db8e6a761da4c5744e87033962191ac5582.
It has no JSON source/companion and joins the curated binary regression list.

The subsequent owned-card mask view model derives current cost/size, two-stage
status groups, trait names/parameter effects, visible upgrades, duplicate upgrade
IDs and unit ability/graft flags from immutable branch values. It carries native
lazy trait cache state:own dirty flag, both modifier counters and both saved
counters. Repeated clean queries preserve pending replacement order instead of
forcing another refresh. Native CardTraitData.None placeholders retain null
trait names; their replacement uses the definition's removable flag separately
from the runtime dummy flag. DefinitionRemovable is optional for older captures;
the original None definition supplies its known default when absent.

Exact native acceptance covers416 carried states/views:15 isolated owned copies,
192 controlled unit/spell combinations and a None-definition replacement edge,
each queried twice. All25,792 outcomes across62 unchanged original masks and
permanent-only costs match, with32 immutable branches. The muted Instant capture
exits zero in103.08 seconds with complete context, RNG/frame and original files
unchanged. Its separate21-play/seven-EndTurn battle matches independent initial/
mid-battle policies, Pyre73; all pure checks and the mask/overflow/trait/status
calibrations pass. Frozen checker binaries are retained under
.probe-runs/owned-card-mask-final-checks:Model SHA256
8714E08890A269EA7B6C2E19EEE65B68B124552D235C27B4C3297986DA51C285,
ModelChecks SHA256
32598863275AC0AC62E1BE0A987901F6DEE5EF1AF3808EA20DCC7B71BFF2D672.
This is the branch-query model; the battle adapter still needs to own and update
its descriptor through upgrade/trait/trigger lifecycles. Temporary relic effects
remain refused until that complete integration is verified. The earlier frozen
189/190 regressions do not establish this later model's combined result.

The frozen owned-view complete191 regression now retains terminal exit0 at
2026-10-10T13:57:56.6806960Z. Its audited output verifies all162 battles (158
paid/four no-more-card chains) and29 ordered calibrations, with no failed or
unsupported core transitions. Every input and frozen model/checker hash matches.
This baseline precedes temporary-upgrade lifecycles and branch-owned battle
descriptors; it does not establish either later version's full regression.

`card-owned-mask-calibration.mt2f` retains the schema1 native branch-query
acceptance described above:13,129 bytes/1,709 nodes, SHA256
845dfedbef2552e647dd8c4ee8a7cebcaf85670ea557e160fe4501156d7b0c9f.
The curated archive is byte-identical to native capture with no JSON dependency.

The subsequent temporary-card-upgrade lifecycle model matches212 native
operations with predicted state carried through each sequence:88 operations
using all11 original collectible relic upgrade payloads on unit/spell copies,
112 controlled operations and12 Moon multiplier query/reset operations. Exact
raw post-operation and post-query states match across36 sequences and32 immutable
parallel replays. Replays carry predicted state rather than recorded outcomes.
Coverage includes temporary-only uniqueness and empty IDs, shared upgrade
instances, ID/index removal cache differences, replacement asset/ID ordering,
AvoidClobbering/TraitsModified, tagged trigger installation/removal, discarded
modifier removal retaining triggers, reset/trait-only reset and magic rescaling.
Reset clears both the standby override flag and its pile value toNone. The two
native magic multiplier overrides include Moon's saved float factor with signed
nearest-even rounding; Moon resource callbacks remain separately scoped.

The muted Instant capture exits zero in63.65 seconds with original files,
complete context, gameplay/test RNG and frame unchanged. Its independent complete
battle matches21 paid plays/seven EndTurns, Pyre73 and initial/mid-battle policies.
All pure checks and the existing mask/overflow/trait/status/owned-view calibrations
pass. Frozen validation binaries are in
.probe-runs/card-upgrade-lifecycle-final-checks:Model SHA256
79C4850AE40800EF3D9E55C83318EC12AAEC634AC8725BE21B8168967142A8DD,
ModelChecks SHA256
4998DF3A9041770DE53C296C6BF03FF8ECDEA3257703980C3F727A8A8A83780A.
This models CardState upgrade operations and installed trigger definitions, not
trigger firing, live unit updates or RelicEffectAddTempUpgrade battle integration.
Those effects remain refused. The production battle adapter must still own/update
the query descriptor through ordinary upgrades, clones, new cards and resets,
then implement native relic ordering, applicability and notifications.

The subsequent battle adapter owns card mask descriptors and ordered upgrade
metadata on CardInstanceState and CardCreationRule. Ordinary play/discard,
hand upgrades, generated/copied cards, bonus draws, abilities and source-card
writebacks preserve those inputs; queries derive numeric cost/size and upgrade
status/icon/ability flags from the current branch. Creation refuses a missing
descriptor when the source registry uses owned descriptors. Native equipment
temporary aggregation creates an anonymous upgrade with no icon/ability and
non-permanent status groups; the model now retains its metadata too.

Six muted Instant native recordings cover ordinary combat, hand upgrades,
generation, clone upgrade refresh, bonus draw and equipment. Independent full
battles and initial/mid-battle parallel policies pass for all six with the final
frozen model:122 paid plays,38 EndTurns,3,125 complete decision contexts and
74,855 exact native card views, including34,725 upgraded-card observations.
Every view repeats in32 immutable query branches. The equipment recording also
matches25 complete native attachment/removal operations. The six mask/arithmetic/
trait/status/owned-view/temporary-lifecycle calibrations and all pure checks pass.
The explicit --branch-card-masks-only command rejects fixtures without enabled
acceptance, and enabled acceptance must contain at least one card view.

Final binaries are retained in .probe-runs/branch-card-masks-equipment-final-checks:
Model SHA256 B5B9B6D15F421241F9A22C7B359DDFBF9B323893FF334A816B662E7242D46045,
ModelChecks SHA2561AA00B4116BE8489348F6F6467D302559760D9350A704D8560E782C2EA81E52F.
This integrates the currently supported modifier paths; mutable trait/cache
callbacks, installed upgrade trigger execution, reset/relic ordering and complete
RelicEffectAddTempUpgrade integration remain open. Existing interaction guards
remain active. The frozen192 lifecycle regression validates its preceding model,
not this later adapter; a complete regression for this version is separate.

The six branch-owned recordings are retained byte-for-byte in the binary
inventory as full-battle-branch-card-masks.mt2f and its -hand-upgrades,
-generation, -clone-upgrade-refresh, -bonus-draw and -equipment companions.
Together they occupy205,839 bytes; all SHA256 hashes/node counts are in
manifest.tsv. The original192 manifest rows remain byte-identical and no
text/JSON sources or generated result logs are tracked. All six join the curated
regression list; the new full198 run is distinct from frozen192.

The native sampler now retains the state captured immediately after
StopCombatLoop, then waits for the original card animation callbacks before
exporting and exiting. This preserves the battle boundary while later game
cleanup resets Moon/resources/phase. The hand-upgrade validation completes23
paid plays/five EndTurns, Pyre80, with zero pending card movements at export;
the independent complete battle and original decision states match. The terminal
wait does not replace the recorded state with a later post-battle cleanup state.

`card-upgrade-lifecycle-calibration.mt2f` retains the schema1 native lifecycle
acceptance described above:8,370 bytes/1,334 nodes, SHA256
22024931ace427c1aa883c3389f5001f64ed0d605429a944daafd93ea0122552.
The curated binary is byte-identical to native capture without a JSON source or
companion. It joins the regression inventory; the complete192 run is separate
from the preceding frozen191 owned-view version.

The frozen lifecycle complete192 regression has now completed with durable exit0
at2026-10-10T14:38:57.9793705Z. Its audited output verifies all162 battles (158
paid/four no-more-card chains) and30 ordered calibrations, every manifest input
identity and both retained binary hashes, with no failed/unsupported core
transitions. This proves the lifecycle version's historical regression; the later
branch-owned adapter and full198 run remain a separate validation scope.

The frozen branch-owned adapter complete198 regression has also completed with
durable exit0 at2026-10-10T15:15:54.2894370Z. Its independent output audit verifies
all168 accepted battles (164 paid/four no-more-card chains),30 ordered component
calibrations and all six card-view oracles:3,125 contexts,74,855 exact views and
34,725 upgraded-card observations. Every manifest input and both retained binary
hashes match, with zero failed/unsupported core transitions. This proves that
adapter version; the later relic card kernel/full199 and subsequent actual battle
relic-manager integration require their own regressions.

The subsequent card-side RelicEffectAddTempUpgrade kernel preserves source-team
and null-upgrade gates, sequential filter dispatch, lazy query/cache changes,
fresh installed-upgrade identity, native temporary application and body refresh.
The original effect returns true even when unique application refuses a duplicate;
the kernel carries this result separately from whether an upgrade was added.
Relic manager conditions, ordering, reset scheduling and notifications remain
the enclosing manager's responsibility; the production battle still refuses this
effect until that integration is modeled.

The isolated native acceptance invokes all11 unchanged original collectible
relic effects on eligible native cards and15 copied owned cards. Its432 carried
operations cover repeat/reset/reapply for every original, forward/reverse ordered
effect chains and controlled source/null/first-filter/second-filter/no-filter
gates. Independent raw states, ordered filters, returned/added flags, next IDs,
queried views and post-query states match across32 immutable parallel replays.
Instrumentation wraps only the effect's native FilterCard call site, preserving
Mono's shared generic implementation and other CardData/CardState callers.

The muted Instant native capture exits zero in67.05 seconds, preserving complete
live context, gameplay/test RNG, frame and original files. Its separate complete
battle passes21 paid plays/seven EndTurns, Pyre73, initial/mid-battle policies
and16 parallel branches. All pure checks and the six existing mask/arithmetic/
trait/status/owned-view/temporary-lifecycle calibrations pass with frozen binaries
under .probe-runs/relic-card-upgrade-final-checks:Model SHA256
602A4C87B4049EC7B8563AF17995EE540A5DC3151544A0CF623D9152237A8CBC,
ModelChecks SHA256
D37F83F9730B70AE385A6FBCA7630B1DA514548B7C44DAD5895C705E1DBECEF9.
This calibrates the card-side effect kernel, not actual battle-manager dispatch,
live unit upgrades or execution of installed traits/triggers. Original relic
battle coverage remains6/224. The frozen198 adapter regression is a distinct
preceding version; complete regression of this later kernel is separate.

`relic-card-upgrade-calibration.mt2f` retains that schema1 native component
byte-for-byte:12,045 bytes/1,448 nodes, SHA256
e5b3e22265b6ac217c5879e9b8704aede11a3d062e8bc23404b2159e07cd356f.
It has no JSON source/companion and joins the curated regression list. The
previous198 manifest rows remain byte-identical, and all199 input identities
match their manifest. The queued full199 run uses an immutable input snapshot
and frozen kernel/checker binaries; its result is separate from frozen198.

`full-battle-incant-relic.mt2f` retains schema108 with the original obtainable
ExtraSpellCastTrigger acquired through SaveManager. The relic asset is unchanged;
authored unit/spell triggers exercise count2 player Incant and count1 enemy Incant,
once/silence/Purify and status children. All21 paid plays, seven EndTurns, complete
contexts/callbacks and initial/mid-battle parallel policies match the native game.

`full-battle-incant-relics-combined.mt2f` adds the original ExtraSpawnTrigger and
ExtraDeathTrigger artifacts. The unchanged production model matches all21 plays,
seven EndTurns, five actual player deaths,160 native fire-count queries and complete
parallel policies. The completed183 run retains its original frozen inventory;
that run cannot establish the subsequent inventory's complete result.

`full-battle-spawn-status-relics.mt2f` retains schema109 with three unchanged
original artifacts:SpawnWithArmor,FirstUnitGainDamageShield and FrostbiteOnEnemies.
All18 plays,six EndTurns,final Pyre80,12 native birth phases and170 exact FIFO
status callbacks pass. Shield triggers once on each of three different turns,
with two same-turn skips. Complete initial/mid-battle policies repeat in16
branches; original birth phases in32. Two native recordings retain identical
complete pre/post states for every one of the18 actions. The schema110 companion
above covers ordinary copied/cardless births. Grafted/aura births and further
relic effects still need native acceptance.

`full-battle-ability-incant.mt2f` retains schema107 with an isolated authored
relic using the original native ability-Incant effect. It is not an obtainable
artifact in the 2.2.1 asset catalog. Under the explicit card-animation settlement
protocol, two native recordings have identical complete decision states; all17
plays, five EndTurns, callbacks and initial/mid-battle parallel policies pass.
The earlier recording with outstanding animation callbacks remains excluded.

The retained inputs are `.mt2f` binary archives. They store typed, deduplicated
value graphs, not JSON documents. Regression reads those graphs and constructs
model states directly. Every captured value is preserved; identical repeated
states share storage. See `src/FixtureArchive/FORMAT.md` for the versioned layout.

`manifest.tsv` records the original capture length/SHA-256 and each archive's
length/SHA-256. The regression script verifies its complete curated inventory
and archive hashes before running. `SIMULATION.md` records native coverage and
capture versions; historical JSON/gzip paths there describe the original inputs
before the binary migration. Import verification compares all values, exact
number lexemes, property order and duplicate keys; JSON whitespace and escape
spelling are not part of the retained representation.

Direct native archives use a `native:` source label and zero text-source
length/SHA-256, because no source JSON document exists. Their binary payload and
complete archive hashes still protect integrity. The capture records the game
version and module MVID; optional diagnostic JSON is used only for independent
value comparison.

`results/` is ignored and holds local logs, catalogs and benchmark outputs.
Isolated native probes use `.probe-runs/`, which is also ignored. Successful
captures become fixed inputs here after native and independent comparisons pass.

`full-battle-harvest-triggers.mt2f` retains schema 70 with four actual physical
deaths across both teams: lethal maximum-health loss and lethal Horde damage.
It records complete operation states and 31 OnDeath/Harvest room/actor/dying
dispatches, including death children, player/enemy group order, Horde repetition,
silence, once flags and required dying statuses. Native and independent checks
pass; operations and dispatches repeat in 32 branches, and the complete later
policy matches initial/mid-battle roots in 16 branches. The curated inventory
contains 101 archives: 92 battles and nine calibration suites.

`full-battle-horde-statuses.mt2f` retains schema 69 with eight actual Horde
status/damage/maximum-health operations on a real summoned Steward and a second
Steward observing rally/harvest queues. It preserves accepted queue contents and
actual dispatch order independently of queue calls, plus complete states before
the operation, after it and after draining. Zero/negative additions, troop
thresholds, simulated deaths and spawning cooldown gates compare in 32 branches;
the complete six-play, five-EndTurn policy compares from initial and mid-battle
states in 16 branches. Preview updates are suppressed only during controlled
setup and restored for the full battle. The separate removal fixture below covers
final-stack removal; runtime Horde casualty upgrades, Rally, merging and cloning
remain work.

`full-battle-horde-removal.mt2f` retains schema 71 with nine native direct API,
effect and running-trigger operations, 63 independently compared death/Harvest
phases and a real paid removal spell affecting both teams. It distinguishes raw
zero-HP status changes without physical death signals from sacrifice, preserves
orphan standby cards and responsible sacrifice cards, and checks exact before,
effect-return and drained states, accepted queue counts and dispatch order.
All operations, phases and the paid spell repeat in 32 branches; the later whole
battle also compares from initial/mid-battle roots in 16 branches. Direct effects
and actual card play preserve their different standby settlement boundaries.

`full-battle-direct-unit-upgrades.mt2f` retains schema 53 with 21 actual
CharacterState API operations before the first recorded player decision.
They cover repeated definition IDs, single-copy removal using caller stats,
fresh versus identical anonymous objects, unique/clone/capacity gates, negative
modifiers, equipment-limit caps, attributed maximum HP and partial lethal
application. The source card remains independent. The complete subsequent
policy and 16 parallel branches match native combat; the API operations also
match independently in 32 parallel branches. This establishes the attribute
primitive used by the equipment lifecycle fixture below.

`full-battle-equipment.mt2f` retains schema 54 with 25 actual attachment,
oldest-first replacement and reverse removal operations. Equipment carries
base, permanent and aggregated temporary upgrades; permanent anonymous upgrade
objects retain explicit identity when removed. Card links and standby closures
record the current host separately from the original host: removed equipment
continues waiting until the original host dies. Global return-to-hand checks
occur before ordinary next-turn draws. All operations compare complete native
room/context states independently in 32 parallel branches; the subsequent
19-play, seven-EndTurn policy matches from initial and mid-battle inputs in
16 parallel branches. Equipment-granted triggers are covered separately below;
grafted equipment, equipment-granted abilities and special return overrides
remain unsupported.

`full-battle-equipment-exhausted.mt2f` uses the same attachment/removal setup
with ordinary equipment whose destination is Exhausted. It captures 15 native
equipment operations and a complete 14-play, seven-EndTurn battle, including
attached death returns and global returns of already-detached equipment. Exact
pile order, standby holes, current card links and live TimesExhausted statistics
match independently. Equipment API cases repeat in 32 parallel branches; the
policy matches initial and mid-battle roots in 16 parallel branches. This
fixture has no exhaustion listeners; their callbacks remain separate work.

`full-battle-equipment-overflow.mt2f` retains schema 55. Setup fills the native
hand to its ten-card limit, kills an equipped host through the native direct
upgrade API, then requests a zero-card draw. Attached equipment returns to the
draw pile during death; replaced equipment returns there during the global
check before DrawHand's full-hand exit. Raw death snapshots retain native
equipment references, while spawning declares canonical references matching
decision inputs. Its 30 equipment operations and one lethal direct upgrade
compare independently in 32 parallel branches; the full 21-play, seven-EndTurn
policy matches from initial and mid-battle roots in 16 parallel branches.

`full-battle-equipment-triggers.mt2f` retains schema 58. Base equipment upgrades
add once-only attachment, repeated attachment-to-any and post-combat triggers;
an anonymous permanent equipment upgrade adds another repeated trigger. Every
trigger records the raw native upgrade ID separately from its bound equipment
instance. CardState.GetID() returns a definition ID, so copies share the same
removal key: removing one copy can revoke matching triggers on another copy
while preserving ordinary base triggers. A base equipped-only removal trigger
also retains its condition after the final equipment disappears. Its 25 actual
equipment operations compare in 32 parallel branches; the complete 19-play,
seven-EndTurn policy compares from initial and mid-battle roots with 16 parallel
branches. Its refreshed capture includes persistent trigger IDs and allocation
cursors across equipment addition/removal. Immediate moon phase triggers and
equipment-granted abilities remain separate work.

`full-battle-trigger-mutation.mt2f` retains schema 57. Three native PreCombat
API cases exercise appended triggers firing in the same iteration, removal of
an earlier trigger skipping the next shifted item, and a self-removed trigger
finishing its remaining effects while its removed sibling never fires. These
deployment-turn tests author the balance timing list to permit PreCombat;
the game's original Boss and enemy waves remain intact. Raw upgrade IDs
also distinguish direct API keys from unit-effect upgrade definition IDs.
All three room/context states compare independently in 32 parallel branches;
the subsequent 20-play, seven-EndTurn policy compares from initial and actual
mid-battle roots in 16 parallel branches. Internal immutable trigger identity
is preserved through copies without entering the binary fixture schema.

`full-battle-detached-bonus-draw.mt2f` retains schema 58. Per-unit persistent
trigger IDs and allocation cursors distinguish callbacks from removed triggers
after list shifting and re-addition of the same upgrade definition. Four native
API transitions retain five separate pending counters/listeners, including an
effect that runs after its trigger removes itself. The next actual hand is
capped and receives the callbacks' temporary card upgrades before clearing
their counters. All transitions compare in 32 parallel branches; the complete
17-play, six-EndTurn policy matches initial and actual mid-battle roots in 16
parallel branches. Older captures retain their legacy index keys; detached
execution requires a captured persistent ID.

`full-battle-conditional-triggers.mt2f` retains schema 59. Trigger conditions
require positive status presence, match IDs case-insensitively, and ignore the
configured required stack counts. Self requirements must all pass; dying-target
requirements are bypassed when that separate queue payload is null. Failed
conditions do not consume once flags, and later triggers see status changes
from earlier triggers in the same phase. Eight controlled native cases and six
natural dispatches compare complete room, actor and dying-target states in 32
parallel branches. The natural cases include two actual Slay callbacks with
dead targets. The complete 19-play, six-EndTurn policy matches initial and
actual mid-battle roots in 16 parallel branches, ending at Pyre 80. Relentless
transitions are covered separately below.

`full-battle-companion-boss.mt2f` and `full-battle-companion-boss-actions.mt2f`
retain schema 60. A single companion Boss moves before gaining relentless
after wave exhaustion, then selectively removes flagged triggers before the
status callbacks drain. The original Boss keeps its attack/health and ordinary
enemies keep their waves; the Boss container moves to the second wave so its
forced return from floor two is observed. Loop/PostAscension/OnShift rewards
total 75 before removal, and the surviving status callback adds 55 afterward.
Trigger order and allocation cursors remain intact. Ten no-card phases and
eight actual-card phases compare independently in 32 parallel branches; both
complete policies compare from their initial roots in 16 parallel branches,
with an actual mid-battle root for the card policy. Raw before/after states and
pending Unity destruction IDs are retained separately from canonical phase
boundaries. Independent checks verify that their only projected changes are
destroyed attacker/equipment references. Synthetic model checks also cover
paired-Boss terminal deaths and a companion dying during its movement callback.
The native captures here still exercise one Boss; paired-Boss group/Pyre
routing, Boss-specific Bump behavior, and outer/final Boss states remain
unverified.

`full-battle-sentry.mt2f` and `full-battle-sentry-lethal.mt2f` retain schema 61.
Both record two native OnSentry dispatches in physical guard order against the
original Boss. Explicit moved targets bypass LastAttackedCharacter team filters;
once flags, silence and zero-argument threshold gates match complete room,
actor and target states. The lethal case preserves the killed Boss as the
later guard's dead target without applying damage to an old living copy.
Complete movement queues include PostAscension/OnShift, deferred hit/death
callbacks and retained earlier-floor events at terminal combat. The authored
Stewards gain 50 health and reduced size, with one silenced copy; the original
Boss loses initial relentless and gains looping, retaining its stats/spawn wave.
Both whole policies match initial and actual mid-battle roots in 16 parallel
branches: 25 plays/eight EndTurns and 21 plays/seven EndTurns, both Pyre 80.
Native dispatches repeat in 32 isolated branches. Additional unsupported effect
targets continue to reject transitions explicitly.

`full-battle-room-capacity.mt2f` and `full-battle-room-capacity-lethal.mt2f`
retain schema 52 battles with live capacities used by subsequent summons and
restricted size upgrades. Their 220 exact effect contexts and 2,050 native
casting/runtime tests cover occupied-room shrinking, 1/30 bounds, signed wrap,
ignored quantity ranges, exact team flags, conditional cancellation, ordered
paid-cost scaling and unit PreCombat effects. Both policies match from initial
and mid-battle roots in 16 parallel branches; the lethal fixture also verifies
that subsequent capacity effects skip after the spell kills the Boss.

`full-battle-bonus-draw.mt2f` and `full-battle-bonus-draw-lethal.mt2f` retain
schema 51 battles with 125 exact future-draw contexts. They cover signed/ranged
amounts, optional upgrades, actual ordered listeners sharing private effect
counters, ordinary zero draws clearing callbacks while keeping the pending draw
count, and capped hands. Both complete policies match from initial and
mid-battle states in 16 parallel branches; the lethal fixture kills the Boss
with a spell and verifies subsequent-effect gates.

`full-battle-x-cost.mt2f` and `full-battle-x-cost-lethal.mt2f` retain schema 50
native battles with 39 X casts, including 16 zero and 23 positive payments,
96 exact paid-cost damage callbacks and 38 exact subsequent energy contexts.
The lethal fixture finishes with an X spell killing the Boss. Both complete
policies match from initial and mid-battle inputs with 16 parallel branches.

`full-battle-status-callbacks.mt2f` retains the complete schema 46 native battle
and 481 ordered status callback dispatches, including zero additions, actual
removals, silence loss and dying actors. These checks compare captured native
unit/context states independently of the predictions stored in the capture.

`full-battle-status-callback-actions.mt2f` retains a complete schema 48 battle
with nested damage, healing, status, upgrade and source-copy callbacks. It checks
390 status and 419 other character dispatches, 95 nested queue payloads, both
source-card writeback boundaries, and continuous simulation from initial and
mid-battle roots with 16 isolated parallel branches.

`full-battle-energy-effects.mt2f` and `full-battle-energy-effects-lethal.mt2f`
capture current, next-turn and persistent energy changes from spells and unit
triggers. They include phase gates, signed/ranged/zero amounts, native caps,
late end-turn accounting, carried combat gains, cancelled ranges and post-boss
gates. Both complete policies match from initial/mid-battle states in 16 parallel
branches; all 448 observed energy applications also compare complete contexts.

`full-battle-ability-cooldown.mt2f` retains schema 62. Two native Steward summons
carry a real unit ability, its configured activation/spawn cooldowns and the
original common ability triggers. Additional damage-card effects exercise
relative/absolute adjustments, zero/negative values, units without abilities,
spawn/activation reset modes, excess-stack retention and deferred available /
unavailable callbacks. Its 89 native effect states compare independently in 32
branches; the complete 15-play, five-EndTurn battle compares from initial and
actual mid-battle roots in 16 parallel branches, finishing at Pyre 80. Complete
room and action states also verify cooldown decay and available-marker changes.
Captured Mono status dictionary slots/free lists preserve LIFO hole reuse after
cooldown cleanup, including later armor/valor insertion. Stewards gain 50 health
and reduced size; all original Boss stats and spawn waves stay intact. Skill
activation, shared cached skill-card state, assignment/replacement/removal,
equipment abilities and Horde re-spawn interactions remain separate work.

`full-battle-ability-cache.mt2f` retains schema 63 and starts before any ability
card exists. Two natural Steward summons share one detached cached skill card;
OnSpawn generation verifies that cache allocation precedes ordinary generated
cards. A distinct skill on the original Boss exercises enemy cache creation.
Four native creation/reuse contexts compare independently in 32 branches, with
starting upgrades, exact identity, unchanged ownership/statistic membership and
complete context preservation. The 17-play / five-EndTurn policy matches initial
and mid-battle roots in 16 parallel branches. Skill activation/payment and ability
assignment/replacement/removal remain separate work; live cache Clear is not
sampled here. The accepted archive has no JSON source dependency.

`full-battle-ability-activation.mt2f` now uses schema 103; the X and lethal
archives remain schema 64. Each records two real units activating one shared
detached skill card. Complete states verify fixed/X/zero payment, pre-own/own
callbacks, self healing through an enemy-only team mask, cooldown/marker changes
and global history without ordinary ownership/discard. The fixed-cost archive
also exercises native `DamagePerSourceAttack`: the unmodified multiplier uses
the activating unit's live attack, while its card-traits flag selects whether
damage traits come from the ability card or attacker. It now also executes
`AttackWithUnit` on both the ability actor and a Pyre target. The live native
probe observed selected room 3 during the Pyre effect even though the ability
action targets room 0; replay therefore keeps that attack on the Pyre floor.
The fixed-cost archive also seeds 2 Damage Shield before applying native
`CardEffectAddStatusEffectPerOtherEffect` to the ability actor with multiplier 3
and target-only counting. Both activations finish with 2 Damage Shield and 6
Armor. The updated 17-action, five-EndTurn battle wins at Pyre 80 after 41
matched room states; complete native and offline replay report zero mismatches
or unsupported operations, and the effect's pure checks cover live sequencing,
unchecked overflow, empty targets and 32 parallel branches.
The lethal fixture ends
when the second skill kills the Boss; ordinary piles clear while skill
identity/history and permanent-deck statistic fallback remain. Whole policies
match initial and mid-battle roots with 16 parallel branches, and each
activation compares in 32 branches. These archives use direct native binary
capture with no source JSON. Ability assignment/replacement/removal and equipment
skills remain separate work.

`full-battle-ability-lifecycle.mt2f` retains schema 65 and 21 native assignment,
replacement and removal API operations. Equipment restores the raw activation
cooldown, preserves the original across repeated replacements and loses that
history after direct assignment. Permanent disable IDs preserve order and
repetitions; explicit assignment remains legal, while later natural player and
enemy births omit the disabled skill and its common triggers. Removal's yielding
callback boundary preserves native available markers and fresh trigger flags.
All complete API states compare in 32 branches; the subsequent 16-play,
five-EndTurn policy matches initial and mid-battle roots in 16 branches and wins
at Pyre 80. The 26,797-byte binary archive has no JSON dependency. Card-effect and
equipment attachment integration of these primitives remains separate work.

`full-battle-ability-effects.mt2f` retains schema 66 and integrates native
SetUnitAbility/RemoveAbility into real card and character-trigger execution.
Seventeen complete effect states and 17 queued dispatches, including 35 generated
payloads, compare independently in 32 branches. Coverage includes reverse
multi-target assignment, sticky last-target follow-ups, an inactive relic gate,
pre-own replacement inside the running queue, cached skill self replacement /
removal, and exact ability/common-trigger allocation. Pre-own replacement changes
the current skill without changing the card already selected for this cast;
permanent removal disables that current replacement and preserves duplicate IDs.
The 17-play, five-EndTurn policy matches initial and actual mid-battle roots in
16 parallel branches and wins at Pyre 80. The 29,332-byte direct binary archive
has no JSON dependency. Equipment attachment grants and global relic mechanics
remain separate work.

`full-battle-equipment-abilities.mt2f` retains schema 67 and integrates ability
upgrades with initial summons, direct unit upgrades and real equipment cards.
Permanent B then temporary C selection, keep-existing B preserving base A,
and two naturally disabled upgraded C births are observed. Seventeen native
equipment operations include repeated replacements and restoration of original
C with its raw activation cooldown 6. Seven direct upgrade cases cover matching
removal, skipped/nonmatching removal, grants to empty slots and direct assignment
clearing equipment history. Two actual equipment skill casts exercise damage,
self-heal and shared detached card state. Twenty-two queued character callbacks
and 12 generated payloads compare completely, with 32 isolated branches for
initial summon, direct upgrades, equipment and callbacks. The complete 17-action,
five-EndTurn policy also matches initial and actual mid-battle roots in 16
parallel branches and wins at Pyre 80. This 31,439-byte direct binary archive has
5,000 unique nodes and no source JSON. The first-battle Boss and original waves
remain intact. Broader quiet-callback death, Horde, relic and room ability
interactions remain separate work.

`horde-stat-calibration.mt2f` is a direct native schema 1 numerical calibration,
with 560 exact raw-stat steps and 175 casualty threshold/count boundaries.
It covers first/later additions, independent HP/max HP buckets, signed overflow,
99999 HP caps, 9999 stack caps, lethal HP and the separate threshold gate.
Temporary definition/map/raw-field substitutions are restored before an ordinary
steward-once battle, whose complete chain still matches in 16 branches. The
calibration recomputes all samples independently in 32 branches and records an
unchanged complete live actor/context. The 4,625-byte archive contains 1,051 unique
nodes and no source JSON. UI calls and actual status removal are intercepted
during this numerical oracle; it does not prove Horde status callbacks, simulated
deaths, merging or complete Horde battles. Those remain explicit integration work.

`full-battle-trigger-repeats.mt2f` retains native schema 68 and adds eight complete
queue batches and 26 independently compared conditional trigger dispatches.
The original queue count multiplies the trigger's configured count after its
conditions and once flag are checked. A once trigger executes every repetition
in its first batch; later batches skip it. Zero and negative counts mark flags
without executing effects. The first three-repeat batch queues three separate
armor children, which drain after the parent and share their own once flag.
Silence and explicit fire permission retain hidden-trigger behavior. Independent
checks compare whole batches and each dispatch in 32 branches, then the seven-turn
battle in 16 branches, winning at Pyre 68. The 26,319-byte archive contains 4,135
unique nodes and no source JSON. Complete Horde status lifecycle remains pending.


`full-battle-dying-horde-upgrades.mt2f` is a native schema-74 battle with five
complete dying Horde operations, seven self-upgrade/removal effects and 45 exact
death/Harvest phases. It covers ordinary/unhealed/attributed maximum-health loss
on corpses, troop casualties, partial additions/removals, accepted queue counts,
retained-card writes and positive follow-ups without revival. Mechanism checks
repeat in 32 branches, and the complete 19-play/seven-EndTurn policy wins at Pyre
53 from initial and actual mid-battle roots in 16 branches. The 36,888-byte archive
contains 6,137 unique nodes and no source JSON. Pending-original-death-listener
upgrades and broader Horde summoning/merging/cloning remain separate work.

`full-battle-rally-triggers.mt2f` is a native schema-75 battle with five exact
Horde growth/birth/removal operations, ten post-play player/enemy phases and twelve
Rally dispatches. It records last-spawned reference timing, explicit overrides,
cached original-room actors, other-floor/new-summon exclusion, repeated/once-only
rewards, silence, required armor and an enemy override that bypasses a player-only
effect filter. Complete operation/phase/actor states and accepted queue counts
match independently in 32 branches. The complete 13-play/five-EndTurn policy wins
at Pyre 80 from initial and actual mid-battle roots in 16 branches. The 30,867-byte
archive contains 4,937 unique nodes and no source JSON. Ordinary/cardless summons,
retained/dead references, lethal Rally and Horde merging/cloning remain separate
native integration work.

`full-battle-rally-lethal.mt2f` is a native schema-76 battle with twelve paid team
phases and six Rally dispatches. An ordinary summon triggers the original Boss's
self-death, then receives permanent attack/health and unit-death armor upgrades
before queued OnDeath/Harvest rewards settle. Complete states preserve the
finished-but-not-yet-removing Boss, live last-spawned unit, resolving card history,
cleared costs and fresh terminal Standby allocation. Mechanisms match in 32
branches; the complete thirteen-play/four-EndTurn policy matches initial and
actual mid-battle roots in 16 parallel branches, winning at Pyre 80. The
21,543-byte archive has 3,333 unique nodes and no source JSON. Original Boss
stats and waves remain intact. Cardless summons, retained/dead targets, Rally
movement and broader Horde merging/cloning/revival remain separate work.

`full-battle-unit-identities.mt2f` and `full-battle-unit-identities-lethal.mt2f`
are native schema-77 recordings of the ordinary and lethal Rally scenarios.
The shared unit allocation counter agrees with every room and outer spawn state
at 50/48 spawn/decision boundaries, including ten/thirteen births across recorded
actions and turns. Existing summon, callback, death and terminal-card states
compare completely; the counter advances at births and survives removals and
terminal clearing. Thirteen-play policies finish after five/four EndTurns at
Pyre 80, matching initial and actual mid-battle roots in 16 parallel branches.
The 30,824/21,475-byte archives have 4,954/3,345 unique nodes and no source JSON.
These prove shared identity state and ordinary births; cardless/nested creation
effects remain separate native integration work.


`full-battle-multi-summon-upgrade.mt2f`,
`full-battle-multi-summon-upgrade-unique.mt2f` and
`full-battle-multi-summon-upgrade-restricted.mt2f` retain schema 79. They record
22 extra spawn-upgrade applications at separate direct-character and source-card
write boundaries, including five unique duplicate source rejections and four
capacity-rejected unit changes followed by successful source writes. Births,
copies, upgrades and Rally states compare independently in 32 branches. Complete
15/15/14-play policies from initial and mid-battle roots match in 16 branches,
with five EndTurns and Pyre 80. The original Boss/waves remain intact, captures
are direct binary archives, and original profile signatures are unchanged.
The curated inventory now contains 113 archives: 104 battles and nine
calibration suites. Fresh fallback sources and lethal/removed birth targets
remain separate native integration work.




`full-battle-multi-summon-fresh.mt2f` and
`full-battle-multi-summon-fresh-deaths.mt2f` retain schema 80 with fourteen actual
fresh fallback source setups and 66 global standby checks. Each source starts
without copied resolving-card history/upgrades or an earlier birth's temporary
upgrades. Source presence and the cardless marker remain independent. The death
scene uses a real paid friendly-damage spell and records two original unit cards
still in Standby after their differently sourced hosts die; the global check
returns both to Exhausted with exact statistics and dictionary free slots.
Complete states compare in 32 mechanism branches, and 15/21-play policies match
initial and mid-battle roots in 16 branches with five/seven EndTurns. The original
Boss/waves and profile signatures are preserved. Both captures are direct binary
archives. The curated inventory now contains 115 archives: 106 battles and nine
calibration suites. Null-source, pool/replacement and broader room/relic creation
remain separate native integration work.

`full-battle-multi-summon-additional.mt2f` and
`full-battle-multi-summon-additional-fresh.mt2f` retain schema 81 mixed summons.
Each original four-plus-four request caps to seven positions before choosing
three TrainStewardBig and four TrainStewardSmall units. Ordinary mode records
twelve detached copies and mismatched source-character upgrades; fresh mode
records fourteen matching source setups with no copies. Both retain seventeen
complete births and fourteen explicit extra upgrades, including source writes
that still succeed for mismatched characters. Self-growth skips that source
write. Full birth/copy/upgrade/Rally states match in 32 branches, and initial
and mid-battle 15-play/five-EndTurn policies match in 16 branches at Pyre 80.
Original Boss/waves and profile signatures are preserved. Both captures are
direct binary archives. The curated inventory now contains 117 archives:
108 battles and nine calibration suites. Pool selection, spawn-count relics,
replacement and broader room/relic creation remain separate native work.

The five schema-82 archives `full-battle-multi-summon-pool-additional.mt2f`,
`full-battle-multi-summon-pool-additional-fresh.mt2f`,
`full-battle-multi-summon-pool-singleton-additional.mt2f`,
`full-battle-multi-summon-pool.mt2f` and
`full-battle-multi-summon-pool-no-primary.mt2f` record actual pooled unit choices.
Native pool order and duplicate weights are retained. Every birth consumes a
Battle RNG draw, even for a singleton or a subsequent additional-character
override. Copied/fresh source selection, extra upgrades and complete callback
states are checked independently in 32 branches; initial/mid-battle complete
policies pass in 16 branches, with fifteen plays, five EndTurns and Pyre 80.
The no-primary scene captures base size zero and the native paid-card Rally
gate while preserving cardless birth Rally. Its earlier mismatching exploratory
recording is excluded. All five accepted captures have zero failures,
mismatches, unsupported transitions and pending observations. Original
Boss/waves and profile signatures are preserved; all are direct binary captures.
The inventory contains 122 archives: 113 battles and nine calibration suites.
Missing fresh sources, spawn-count relics, removed birth targets/replacement
and broader room/relic creation remain separate native integration work.

The full 122-archive regression exits zero with 113 battle passes, nine
calibration passes and all five pooled summon checks. Unsupported transition
counts remain zero, and every archive matches the inventory/SHA-256 manifest.

The four schema-83 archives `full-battle-multi-summon-pool-missing-fresh.mt2f`,
`full-battle-multi-summon-pool-additional-missing-fresh.mt2f`,
`full-battle-multi-summon-pool-no-primary-missing-fresh.mt2f` and
`full-battle-multi-summon-pool-no-primary-missing-fresh-deaths.mt2f` record actual
births after a completed no-match fallback lookup. The first two mix 3/11
source-free births with 4/3 matching fresh sources; the latter two record seven
source-free births and no fresh setup. A missing choice cannot reuse the
primary fallback or allocate a card identity. The first source-free birth is
cardless, triggers `OnSpawnNotFromCard`, and skips only the source write of an
extra unit upgrade. The original paid card retains its first-host binding.
The death scene records actual delayed returns during a global Standby check.
Complete mechanisms pass in 32 branches; initial/mid-battle policies pass in
16 branches. Ordinary scenes win at Pyre 80 after 14/15 plays and five EndTurns;
the death scene wins at Pyre 49 after 21 plays and seven EndTurns. The Boss,
waves and original profile signatures are preserved. All four captures are
direct binary archives with zero failures, mismatches, unsupported transitions
and pending observations. The inventory contains 126 archives: 117 battles
and nine calibration suites. Parentless trigger creation, spawn-count relics,
removed birth targets/replacement and broader creation remain separate work.

The full 126-archive regression exits zero with 117 battle passes, nine
calibration suites, four missing-source checks and nine pooled summon checks.
Unsupported transition counts remain zero; all archives match the inventory,
sizes and SHA-256 manifest.

The schema-84 `full-battle-spawn-points.mt2f` archive records 21 real native
position operations and 63 current/allow-last-known queries over three
source-free Stewards. It preserves slot occupants, independent unit pointers,
last-known references, holes, invalid-index no-ops, pivot shifts, cross-room
moves, zero-HP/undying occupancy, preview-born removal and the destroyed-detached
assignment guard. These are controlled API setups, rather than real death,
revival or preview-mode lifecycle tests. Actor-owned points are restored before
ordinary play. Complete operations match in 32 branches; the initial and
mid-battle policies match in 16 branches, winning at Pyre 80 after 18 plays and
six EndTurns. The Instant run takes 45.18 seconds; Boss/waves and original
profile signatures stay intact. All native capture gates are zero. The direct
archive is 25,202 bytes with 4,058 unique nodes. The inventory contains
127 archives: 118 battles and nine calibration suites. Integrating physical
point state throughout ordinary battle transitions, unit-trigger summons and
death replacement remains separate work.

The full 127-archive regression exits zero: all 118 battle archives and nine
calibration suites pass, including one physical spawn-point check, four
missing-source checks and nine pooled summon checks. Unsupported transition
counts remain zero. The complete archive inventory, sizes and SHA-256 values
match the curated manifest.

The schema-85 `full-battle-physical-spawn-points.mt2f` and
`full-battle-physical-spawn-points-no-cards.mt2f` archives carry physical group
occupants and retained current/last-known references in every complete battle
context. Independent checks cover 204 complete room contexts, 30 physical
removals and 17 cross-room moves, plus full roots in 16 branches and complete
rooms in 32 branches. Player/enemy/treasure births, front/back rearrangement,
death/queued completion, treasure despawn, same-room hole filling, ascension
and post-room centering update the shared position state. Retained identities
survive removals; malformed identities/references reject without partial children.

The first archive records 18 plays/six EndTurns/Pyre 80, 254 native compaction
boundaries, 26,962 bytes/4,408 nodes and a 43.66-second Instant run. The no-card
archive records seven EndTurns/Pyre 49, 233 compaction boundaries,
18,096 bytes/2,735 nodes and a 35.44-second run. All native capture gates are
zero, with muted audio and intact original Boss/waves/profile signatures.
The inventory contains 129 archives: 120 battles and nine calibration suites.
Physical capture remains opt-in; general triggered births, removed-target
replacement/equipment transfer, revival, Horde merging, spell movement and
special Boss paths still require further native integration.

The full 129-archive regression exits zero: all 120 battle archives and nine
calibration suites pass, including two integrated battle-position checks, two
standalone spawn-point checks, four missing-source checks and nine pooled
summon checks. Unsupported transition counts remain zero. The complete archive
inventory, sizes and SHA-256 values match the curated manifest.


The schema-86 triggered summon family contains four direct binary archives:
`full-battle-triggered-summons.mt2f`, `full-battle-triggered-summons-fresh.mt2f`,
`full-battle-triggered-summons-death.mt2f` and
`full-battle-triggered-summons-death-fresh.mt2f`. Real player unit definitions
receive native summon, spawn/heal gold and Rally triggers; an extra upgrade
adds one damage, two health and one armor. Death variants apply native lethal
Tower damage using the real rearrangement spell. Boss/waves stay original.

The two live variants each win at Pyre 80 after 15 plays and five EndTurns;
each records ten summon applications, four births and six retained zero-birth
caches. The death variants each win at Pyre 49 after 21 plays and seven
EndTurns, recording two dying-source applications/births. Copied and fresh
source paths remain separate. The archives preserve flat ID-based definitions,
physical positions, first-born effect caches and queued spawn/healing/Rally
boundaries, including actual shared weak-reference changes during previews.

Forty complete native damage steps include two cross-room pending-death
boundaries. Independent checks compare every complete train state and the
zero-HP victim's flags in sixteen branches, including death-statistic listener
removal, pending callbacks, source return and physical centering. Duplicate
deaths reject without partial children or parent changes. Full policy and
position checks remain active. All four native capture gates are zero, with
muted audio and unchanged original profile signatures.

Sizes/nodes are 24,791/3,941; 24,661/3,941; 32,000/5,308; and 32,021/5,308
in the archive order above. Instant sampling times are 43.59, 44.02, 44.63 and
45.73 seconds. The inventory contains 133 archives: 124 battles and nine
calibration suites. Equipment transfer, revival, Horde merging, additional
trigger summon pool/multi-birth combinations and broader special mechanics
remain incomplete; these archives establish the recorded paths only.


The full 133-archive regression exits zero: all 124 battle archives and nine
calibration suites pass. Four summon suites independently compare all forty
native damage steps, along with six integrated position checks, two standalone
position checks, four missing-source checks and nine pooled summon checks.
Unsupported transition counts remain zero. All binary sizes and SHA-256 values
match the complete curated inventory and manifest.

Schema 87 adds four triggered summon equipment archives:
`full-battle-triggered-summons-equipment.mt2f`,
`full-battle-triggered-summons-equipment-fresh.mt2f`,
`full-battle-triggered-summons-equipment-death.mt2f` and
`full-battle-triggered-summons-equipment-death-fresh.mt2f`.
Native setup plays a real Steward and attaches two ordinary cards and one
ReturnToHand card. Real definitions receive equipment upgrades, temporary
offsets, permanent anonymous spikes and equipment triggers. Two-birth summons
exercise the child's one-slot replacement path. Death variants retain the
native Tower lethal spell; original Boss and waves remain intact.

Copied sources duplicate attached equipment for live actors and reuse originals
after native actor removal. ReturnToHand and IgnoreCardUpgrades exclusions,
non-permanent ownership, standby host changes, displaced gear and outer-queue
callback timing are checked independently. Seventeen clone boundaries and
forty-six attachment/removal operations compare full native contexts. Sixteen
damage phases include four cross-room pending deaths. Full policies and
physical state checks run in sixteen/thirty-two branches; missing attachment
definitions reject without partial children or parent changes. The death
variant also verifies that final death-born equipment misses the preceding
TimesPlayed event while receiving later events normally.

In the archive order above, sizes/nodes are 28,661/4,694; 26,359/4,244;
35,034/5,982; and 33,340/5,650. Native Instant times are 51.58, 50.78, 54.33
and 50.41 seconds. Live variants finish after sixteen plays/five EndTurns at
Pyre 80; death variants finish after twenty-three plays/seven EndTurns at
Pyre 49. Every native capture passes with zero failures, mismatches,
unsupported transitions and pending observations, muted audio and intact
original profile signatures. The inventory contains 137 archives: 128 battles
and nine calibration suites. Sizes and SHA-256 values are in `manifest.tsv`.

The full 137-archive regression exits zero: 128 battles and nine calibration
suites pass, including eight queued-summon suites, four equipment suites,
fifty-six independently compared native damage phases and ten integrated
position suites. Unsupported transition counts remain zero; complete archive
sizes and SHA-256 values match the curated inventory and manifest.


Schema 88 adds full-battle-revival.mt2f (35,564 bytes / 5,941 nodes). The native
fixture keeps the original Boss and waves, plays two real Stewards, adds two
ordinary enemy observers and authors both-team Harvest/reanimated/death gold
callbacks. It covers first/last-stack revival, a zero-stack direct API call,
final removal, lethal max-health sacrifice and two revivals inside a running
self-damage trigger queue. One-time statistics listeners and sacrifice flags
are distinct from the retained source reference. Eleven complete revival
boundaries, seven direct/removal operations, ninety callback phases, complete
policies and physical state are independently recomputed with parallel branches.

Standalone setup defers automatic UI queue runners during the captured API,
then drains through the native caller; natural combat has no deferrals. The
muted Instant run wins at Pyre 53 after nineteen plays/seven EndTurns in 50.62
seconds, with zero capture failures, differences, unsupported or pending records
and unchanged original profile/log signatures. The inventory contains 138
binary archives: 129 battles and nine calibrations. Sizes/SHA-256 values are
in manifest.tsv; no source JSON is required for regression.

The complete Check-Models.ps1 regression passes all 138 archives, including
129 battle suites and all nine calibrations, with zero unsupported transitions.
Archive inventory, sizes and SHA-256 values match the curated manifest.

Schema 89 adds four equipment-owned summon archives:
`full-battle-triggered-summons-equipment-owned.mt2f`,
`full-battle-triggered-summons-equipment-owned-fresh.mt2f`,
`full-battle-triggered-summons-equipment-owned-death.mt2f` and
`full-battle-triggered-summons-equipment-owned-death-fresh.mt2f`.
Real return-to-hand equipment receives once-only OnTurnBegin and OnDeath
summons. Native card plays attach it to the ordinary host, and the policy can
re-attach the same equipment after it returns. The original Boss and waves
remain intact. Effects use the host's spawner card; the equipment binding is
separate and is excluded from child transfer. Copied/fresh sources exercise
both inheritance paths, including sources retained during death.

The archives record fifty complete summon effects, twenty-eight equipment-owned
birth applications (eighteen from dying hosts), sixty native damage phases and
1,330 source cache writes. Independent effect checks compare complete room/actor states,
first-birth caches and accepted callback FIFO in sixteen branches. Complete
policies run from original/middle roots; physical state checks use thirty-two
branches. Raw effect/death point references remain intact. An explicit stable
decision protocol exports 2,165 raw/canonical mappings, verifies the clearing
of 1,290 removed-unit reference records and preserves live positions and
physical groups.
These counts include repeated snapshots of the same actors.

In the archive order above, sizes/nodes are 31,013/5,105; 27,905/4,507;
53,404/9,150; and 47,865/8,209. Muted Instant native runs take 134.37, 83.83,
78.06 and 80.17 seconds. Live variants play sixteen cards/five EndTurns and win
at Pyre 80; death variants play twenty-five cards/five EndTurns and win at Pyre
79. Native and independent checks pass with zero failures, mismatches,
unsupported or pending observations and intact original profile/log signatures.
The inventory contains 142 binary archives: 133 battles and nine calibrations.
Sizes and SHA-256 values are recorded in manifest.tsv; no JSON is required.

The full 142-archive regression exits zero: all 133 battle suites and nine
calibrations pass, with zero unsupported transitions. It includes fifteen
integrated physical-position suites, twelve queued-summon suites, eight
triggered-equipment suites, four direct effect suites, four raw/canonical
decision mapping suites and 116 independently compared summon damage phases.
The complete binary inventory matches the curated sizes and SHA-256 manifest.

Revival with equipment/child summons, Horde merging/cloning, broader relic/room
effects, special Boss/Pyre behavior and complete optimal search remain unfinished.

Two new equipment-owned revival archives add complete ordinary card/combat
paths, preserving the original Boss and waves. Hosts start with two undying
stacks and children with one; OnReanimated precedes OnDeath, whose live source
copies equipment while keeping originals attached. There are no standalone
setup revival operations or natural queue deferrals.

`full-battle-triggered-summons-equipment-owned-revival.mt2f` uses schema 91,
85,318 bytes / 15,420 nodes, and records 32 plays / seven EndTurns, final Pyre
76, 41 revival boundaries and 60 callback phases in 113.50 native seconds.
`full-battle-triggered-summons-equipment-owned-revival-fresh.mt2f` uses schema
92, 73,284 bytes / 12,902 nodes, and records 31 plays / seven EndTurns, final
Pyre 75, 43 revivals and 61 phases in 110.98 seconds. Both muted Instant native
runs and independent complete policies pass, with zero native failures,
differences, unsupported or pending observations and intact original profile
and log signatures. Timings measure instrumented capture, not search speed.

Raw card room-cache membership is distinct from the alive-only query view;
zero-HP members become visible again after revival. The archives preserve 534
restored cache observations, 92,140 independently checked raw/canonical cache
mappings, 47 complete summon effects and 146 additional native damage phases.
Schema 92 also retains a zero-HP attacker snapshot omitted from visible units
while its physical slot remains occupied. Independent checks preserve
attribution and physical references without inserting that attacker into the
visible unit list, reject missing cache/reference data and compare states and
accepted FIFO in parallel branches. Counts include repeated snapshots, not
distinct actors.

The inventory now contains 144 binary archives: 135 battles and nine
calibrations. Sizes and SHA-256 values are in manifest.tsv; regression has no
source JSON dependency. The complete Check-Models.ps1 regression exits zero:
all 135 battles and nine calibrations pass, including seventeen integrated
position suites, fourteen queued-summon suites, ten equipment suites, six direct
effect suites, six decision reference suites, the standalone revival suite and
both new revival/summon suites. All 262 native summon damage phases match, with
zero unsupported transitions and intact complete inventory/size/SHA-256 checks.

Horde merging/cloning, broader relic/room effects, special Boss/Pyre behavior
and complete optimal search remain unfinished.

`full-battle-horde-merge.mt2f` adds schema 97 native Horde cloning/merging with
17 API operations, four temporary previews and five manager-order selections.
Exact API/drained room/context/source states, complete primary restoration,
accepted/dequeued callbacks and evolving once flags match in 32 branches.
Coverage includes both teams, different definitions, self/cross-team/null/
non-Horde/immunity gates, a removed actor's pending Rally entry, mixed attached
gear destinations, orphan unit Standby cards and opposing-team physical gaps.
Preview merges retain physical points and equipment; primary restoration
preserves and resolves native shared raw weak room caches.

Its complete 17-play/five-EndTurn battle wins at Pyre 80; initial/middle policies
match in 16 branches. Native failures, differences, unsupported/pending records
are zero, and the original profile/log signatures are intact. The capture uses
real paid hosts and native equipment/enemy API preparation; original Boss/waves
are unchanged. The 31,631-byte archive retains 5,159 unique nodes. The curated
inventory now contains 145 binary archives: 136 battles and nine calibrations.
Sizes and hashes are in manifest.tsv; no source JSON dependency is introduced.

The complete 145-archive regression exits zero: all 136 battles and nine
calibrations pass. It retains eighteen physical-position suites, fourteen
queued-summon suites, ten equipment suites, six direct summon-effect suites,
seven decision reference suites, all 262 summon damage phases and the new Horde
API/preview suite, with no unsupported transitions. Both restored raw weak cache
memberships match; complete inventory/size/SHA-256 checks pass.

The schema 97 coverage above proves the direct Horde APIs. Schema 98 adds the
primary Bump card paths described below; broader simulator/search work remains.

### Primary Bump cards (schema 98)

`full-battle-bump.mt2f` records eighteen actual Bump card plays, including
up/down/zero/clamped quantities, an ignored authored integer range, rooting,
immobility priority, looping, simultaneous enemy targets, player Pyre blocking,
enemy Pyre entry, full/partially blocked rooms and both-team cross-room Horde
merges. Complete raw train/context/retained-target states and dispatch payloads
are independently compared in 32 branches. Merged unit cards remain in Standby
without death/Harvest/Rally/re-spawn callbacks.

All 38 paid-card records compare individually, including the eighteen authored
plays marked `ScenarioAction=true`. The subsequent ordinary policy consists of
20 card plays and five EndTurns, wins at Pyre 74, and matches from initial/middle
policy roots in 16 parallel branches. Native recording uses paid Steward hosts,
native observer/filler births and gold callbacks with the original Boss/waves.
It preserves raw effect boundaries and stable decisions after preview refresh.
Later rearrangement plays also verify missing Shift/Sentry reward callbacks.

Native capture is muted and Instant, takes 55.49 seconds and has zero failures,
differences, unsupported/pending records; original save/log signatures match.
The archive has 7,189 graph nodes and is 37,157 bytes. Manifest size/hash and
regression inventory now cover 146 binary archives: 137 battles and nine
calibrations, without any source JSON dependency.

The full 146-archive regression exits zero with all 137 battles and nine
calibrations passing and no unsupported transitions. The audited inventory
retains nineteen physical-position suites, fourteen queued-summon suites, ten
equipment suites, six direct summon-effect suites, eight decision-reference
suites, standalone/two summon revival suites, Horde/Bump suites and all 262
native summon damage phases. All archive sizes and SHA-256 hashes match.

Actual Bump previews, teleportation, disabled/destroyed floors, paired
companion-Boss forced movement/destruction and movement callbacks involving
damage/revival still need coverage, as do ordinary cloning and wider
relic/room/card mechanics. The full simulator and optimal search remain
unfinished.

`full-battle-outer-boss-bump.mt2f` covers 21 native Bump operations against the
Outer Train Boss. Its anchor and reserved outside physical spawn slot move
together, with callback order and the boss attack phase matched. The muted
Instant fight wins at Pyre 74 with 22 actions, two EndTurns and 23 room stages;
capture failures, mismatches, unsupported transitions and pending records are
zero. Focused replay matches 21 Bump states, all 23 room stages and all 22
native actions; 16 policy branches match. Capture took 50.54 seconds. The
34,008-byte binary archive has 6,825 unique nodes and SHA-256
`90c49da7ebf3c8cead2acff9d80d291460fe760539a8ceac92f11174f7eefc30`. It adds
no original relic-owner evidence. Inventory is 236 archives: 205 battles and
31 component calibrations; full-inventory replay remains deferred to a
milestone.

```powershell
& ./scripts/Check-Models.ps1 -Fixture full-battle-outer-boss-bump.mt2f
```

### Ordinary unit clone APIs (schema 99)

`full-battle-unit-clone.mt2f` preserves thirteen native clone operations and 44
full intermediate train/context boundaries. It exercises front/back/selected
placement, excluded permanent/temporary source upgrades and their status
contributions, wounded/buffed/negative actors, non-grafted equipment, natural/
replacement skills and configured cooldowns, clone-of-clone and source-free
births. Null/invalid-room/selected-boundary gates allocate nothing; a full room
allocates the copied card but no unit. Horde cloning allocates neither.

Copied source/equipment references are detached rather than owned pile cards.
Clone flags precede spawn callbacks. Raw observation pauses only background
queue runs while intrinsic birth/gear-upgrade queues execute; the subsequent
explicit drain retains complete callback payloads and order. All states and
queues compare in 32 branches without parent mutation.

The complete subsequent fourteen-play/five-EndTurn policy wins at Pyre 80 and
matches from initial/middle roots in sixteen branches. Native capture is muted
and Instant, takes 64.12 seconds and reports zero failures, differences,
unsupported/pending records, with original save/log signatures intact. The
30,687-byte native binary preserves 4,972 graph nodes. Manifest inventory now
contains 147 archives: 138 battles and nine calibrations, without source JSON.

The complete 147-archive regression exits zero with all 138 battle fixtures and
nine calibrations passing, including 134 independent policy chains. The final
audit checks all required native suites and 262 summon damage phases, with zero
unsupported transitions. Every archive matches its size/SHA-256 manifest, and
the curated clone archive is byte-identical to the native binary capture.

Paid copy-spell integration, enemy/grafted/preview cloning and room/relic or
destruction/revival interactions still require their own coverage. Broader
simulator and optimal-search work remains unfinished.

### Paid unit copy spells (schema 100)

`full-battle-unit-copy.mt2f` preserves fourteen real paid `CardEffectCopyUnits`
effects and all 28 paid card results. Coverage includes signed/ignored-range
counts, targeted and room-wide copies, sequential births, clone-of-clone,
incoming status/ability callbacks, non-grafted equipment, cardless sources,
full/partial room failures, selected-last gates and Horde without a new actor.
The immutable definition catalog resolves each clone from its current actor
and card references. Raw train state, incoming/outgoing queues and all intrinsic
dispatch payloads/order match independently in 32 branches without mutation.

The subsequent fourteen-play/five-EndTurn policy wins at Pyre 80 and reproduces
initial/middle roots in sixteen branches. The first policy turn has no legal
play and is independently recomputed. Every ascent input is empty after combat;
physical checks verify zero cross-room moves, fourteen removals and complete
position states in 32 branches. Native recording uses paid hosts, an ordinary
third-floor clone, native fillers and authored gold callbacks while retaining
the original Boss/waves. Muted Instant capture takes 89.85 seconds; all native
failure/difference/unsupported/pending gates are zero and original files match.

The 35,606-byte binary has 6,511 unique nodes and matches the native archive
exactly. Inventory is 148 archives: 139 battles and nine calibrations. Complete
regression and audit pass, including 135 independent policy chains, all nine
calibrations, 21 physical-position and ten decision-reference suites, and all
262 summon damage phases. All archive sizes/SHA-256 hashes match; unsupported
transitions are zero. Enemy/mixed-team copying is covered below; grafted/preview copying, broader room/relic/
destruction/revival interactions, special Boss mechanics and complete optimal
search remain unfinished.

### Enemy and mixed-team copy spells (schema 101)

`full-battle-hero-copy.mt2f` preserves sixteen real paid copy effects: raw versus
live stats, repeated/clone-of-copy births, incoming status and replacement-skill
queues, optional detached equipment, room targets, last/full/partial physical
groups, Horde and both mixed-mask source teams. Exact Heroes masks create
enemies; mixed masks use monster cloning even from an enemy source. Enemy
births retain natural skills, clear IsSpawning before OnSpawn, drain global
setup/child queues before that phase and enchant only the newborn. Their
counted effect spawn total stays zero; copying Horde stats preserves its
separate Rally. Initial hero compaction is distinct from effect-end compaction.

All raw train states, queued callbacks and nine-field intrinsic dispatches
compare independently in 32 branches. All seventeen paid actions and the
subsequent first-turn loss compare, including a real ordinary Champion birth's
player-wide enchant phase before Rally. Initial and later policy roots reach
the same Pyre-zero result in sixteen branches. Twenty room contexts, twenty-one
cross-room moves and 275 decision-reference mappings pass; no ordinary unit
dies. Pyre death preserves raw occupancy and ordinary death statistics, and
the terminal transition skips new waves without changing phase/cache/RNG.

Native setup uses two paid player hosts, three enemy Shield Steward hosts,
authored skills/gold callbacks/equipment and native fillers. Original Boss
definitions and waves stay intact. Muted Instant capture takes 43.30 seconds;
all failure/difference/unsupported/pending gates are zero and original files
match. The 24,418-byte binary contains 4,755 unique nodes and is byte-identical
to native output, with no source JSON. This verifies the observed primary
copying and loss paths; grafted/substituted/Boss/preview births, broader effects,
room/relic/covenant interactions and birth destruction/revival remain work.

Inventory is now 149 binary archives: 140 battles and nine calibrations.
Complete regression and audit pass, including 136 independent policy chains,
22 physical-position and eleven decision-reference suites, all fourteen
queued-summon, ten triggered-equipment and six direct summon-effect suites,
and all 262 summon damage phases. Sizes/SHA-256 hashes and native/original-file
gates match; errors and unsupported transitions are zero.

### Spawn enchant lifetime and Horde children (schema 102)

`full-battle-spawn-enchant.mt2f` records ordinary one-unit Shield Steward and
repeated two-unit Sword Steward paid births, player-wide Self Horde/armor
enchant and authored spawn/unscaled/not-from-card/status/Rally callbacks.
Original Boss definitions and waves stay intact. Newborn IsSpawning stays true
until enchant and its child queue settle, then clears before paid Rally. Source
card room caches exclude that newborn while enchant effects execute.

Fifteen enchant phases, seven complete births and 66 exact nine-field child
callbacks compare in 32 isolated branches. Native observations include newborn
and older actors, initial and later Horde growth, raw/canonical room caches,
two detached source clones, ten paid Rally team phases and 27 Rally dispatches.
All fifteen paid actions, 38 room stages and five EndTurns match. Independent
initial/mid-battle policy roots win at Pyre 80 in sixteen branches. Seventy-six
room contexts, fifteen removals, eight cross-room moves and 370 reference
mappings verify physical state. The old model fails this archive's first action
because it incorrectly marks the newborn's paid Rally trigger as fired.

Muted Instant capture takes 47.39 seconds, with zero failures/differences/
unsupported/pending records and unchanged original files. The 30,572-byte
binary contains 4,966 unique nodes and exactly matches native output. No source
JSON is retained. Wider enchant effects, grafting, birth destruction/revival,
room/relic/covenant interactions and special Boss mechanics remain unfinished.

Inventory is now 150 binary archives: 141 battles and nine calibrations.
Complete regression and audit pass, including 137 independent policy chains,
23 physical-position and twelve decision-reference suites, all fourteen
queued-summon, ten triggered-equipment and six direct summon-effect suites,
and 262 summon damage phases. All archive sizes/SHA-256 hashes and native/
original-file gates match; errors and unsupported transitions are zero.

`enchantment-lifecycle-calibration.mt2f` is a direct native schema-1 calibration
of persistent CardEffectEnchant at the status API request boundary. It records
1,460 ordered primary/preview-map and two-stream RNG transitions, 644 complete
requests with map snapshots at API entry, a 1,152-case actor gate matrix,
192 signed-count/Duality cases, preview key retention and Setup/binding gates.
Independent output-carrying chains cover 34 initial and 72 random steps;
all cases and chains run in 32 branches without mutating roots. Native status
calls are suppressed for this calibration, so automatic combat updates and
actual status/callback integration remain unfinished and explicitly unsupported.
The native full battle after restoration and its independent seven-turn policy
both win. Muted Instant capture takes 47.92 seconds; capture/native/original-file
gates pass. The binary is byte-identical to native output, 19,551 bytes/4,856
nodes, SHA-256 `2b7bdc7a17da201945b3fd35e3d2cd21ad0edd720825b9d34b6d8d523c76c618`.
Complete regression and audit pass for 151 archives: 141 battles and ten
calibrations, 137 independent policy chains, 23 physical-position and twelve
decision-reference suites, fourteen queued-summon, ten triggered-equipment,
six direct summon-effect suites and 262 damage phases. All sizes/hashes and
native provenance/original-file gates pass; errors and unsupported transitions
are zero. Persistent aura integration into the combat engine remains open.

`enchantment-combat-calibration.mt2f` captures actual status mutations and drained
child callbacks for persistent CardEffectEnchant on two real paid units. No
native gameplay/status API is suppressed. Thirty-four aura/control samples cover
global allow/reentry guards, direct nested global updates, muting/silencing,
signed/zero/Duality-overflow counts, poison, sixteen pooled random steps and
Horde growth plus nonlethal removal; five isolated Deathwish steps verify
front/mute membership and exclusive activation/loss queues.
The Deathwish callbacks carry native `CardEffectRewardGold` and self-targeted
armor effects: activation and loss produce 5/5/5/10/10 gold across the five
transitions, while each matching callback adds one Armor. Exclusive trigger
identities prevent paired queue records from double-firing. The standalone
combat queue matches both effects; the aura-attached queue continues to reject
actor/card-mutating callbacks, and the check verifies that rejection leaves its
parent unchanged. All 72 API calls and 124 complete callback payloads,
per-effect maps, train/context states, explicit UI preview preparation events
and supported child drains match in 32 isolated branches. Live captured
train/context, every living actor's primary state/triggers/identity map, RNG and
update flags restore; the queue is empty afterwards. The Horde probe isolates
native card statistics and restores the simulated-death counters and FunGuy run
stat after recording the matching state transition.

The following native and independent ordinary battle win after 21 paid actions,
seven EndTurns, Pyre 73; original profile/log file gates pass. Muted Instant native
capture takes 51.21 seconds. The 9,760-byte/1,776-node archive is byte-identical to
native output, SHA-256 a68ec95cafa6b9fd202b8201a0c5d892f97fd2cce8fa28c673497b87e58600da.
There is no source JSON. Generic combat still rejects persistent CardEffectEnchant
until automatic birth/movement/death/preview hooks and mutating child callbacks
are integrated. This is a real-status update kernel, not full aura battle support.

Inventory is now 152 binary archives: 141 battles and eleven calibrations. Complete
regression/audit pass with 137 policy, 23 physical-position, twelve decision-reference,
fourteen queued-summon, ten triggered-equipment, six direct summon-effect suites
and 262 damage phases. Native provenance, all archive hashes and original-file gates
pass. Final explicit refusal/binding capture fixes build, and both enchantment
calibrations plus unsupported-boundary checks pass independently afterwards.

`enchantment-world-calibration.mt2f` adds 32 real AddStatusEffect/RemoveStatusEffect
calls on the same isolated paid hosts, including silenced/muted, zero/repeated
adds, zero/all removals, ordinary armor/dormant writes, Spark with dormancy,
disabled updates and sixteen consecutive random-pool steps. These calls invoke
native global aura updates automatically; the calibration does not call
UpdateEnchantments itself or suppress gameplay APIs. Complete train/context/aura
state, both RNG streams, 76 callback payloads and drained gold effects match;
24 aura status calls are observed and retained for diagnostics. All cases repeat
in 32 isolated branches, and the random chain carries model outputs forward.
UI preview preparation is recorded explicitly. Shared world serialization and
live/retained identity checks also pass.

The accepted native capture takes 60.38 muted Instant seconds. Its 8,428-byte,
1,470-node binary is byte-identical to native output, SHA-256
`3a2b4af9f7f1c94d3d0b36cee560c0fc5e056a271a054d08ee641daf58132d51`.
There is no text source/JSON companion. Captured live state, bindings/trigger
identities, gold and RNG are restored before the subsequent ordinary paid battle
wins after 21 actions/seven EndTurns, Pyre 73; original-file signature gates pass.
Isolated run-stat/UI effects remain outside the restoration claim. This verifies
automatic control-status updates, not birth/movement/death/preview scheduling or
actor/card-changing children. Generic persistent CardEffectEnchant combat remains
refused until those paths are integrated and verified.

Complete regression and audit pass for 153 archives: 141 battles/twelve
calibrations, 137 policy, 23 physical-position, twelve decision-reference,
fourteen queued-summon, ten triggered-equipment, six direct summon-effect suites
and 262 damage phases. Binary hashes/provenance and native original-file gates
pass. Final shared-train refusal and both real-status/control-status calibrations
also pass independently after the full run.

`enchantment-source-order-calibration.mt2f` records the actual native manager list
collector: 172 ordinary lists across the real status matrix/restored paid battle,
and 32 read-only incoming-list stress cases with lengths 0/1/15/16/17/31/33/64.
All 204 arrays match independently in 32 branches, covering both teams, equal
physical positions across floors, deduplication during room collection and repeated
sorting after empty rooms. Native team values sort enemies before players; same-
team sorting uses physical IndexInRoom, correcting the earlier floor-first account.
Multiple aura-source floors explicitly require captured physical points.

The byte-identical native archive is 2,681 bytes/581 nodes, SHA-256
`b9011555d2a5cd35f410b917166240a64c83d56de74de4766a5e77581bfe5804`,
with no text source/JSON companion. The 74.43-second muted Instant run passes/wins,
has zero errors/unsupported/pending records, preserves original-file signatures
and independently wins the restored ordinary battle in 21 actions/seven turns,
Pyre 73, initial/middle roots and 16 branches. Final control/real-status/lifecycle
suites pass on the corrected collector, including missing-point refusal.

Inventory is 154 archives: 141 battles/thirteen calibrations. All manifest hashes
pass. Regression evidence is the preceding complete 153-archive run/audit plus
the new 204-list suite and final affected aura suites; a second complete
154-archive run was not performed. Generic persistent-aura combat and the full
simulator remain incomplete.

`full-battle-persistent-enchantment.mt2f` keeps paid Steward singleton armor auras
bound throughout a real game 2.2.1 battle, targeting both teams and draining
status-change gold children. The original Boss/waves remain intact. Schema 103
captures the context-owned aura world, preview maps/cache, retained destroyed
targets, physical positions and update guards at every ordinary action boundary.
The muted Instant native run takes 53.17 seconds and wins in 15 actions/five
EndTurns, Pyre 79, with zero differences/failures/unsupported/pending records and
unchanged original profile/log signatures. Independent checks reproduce all
42 room stages, nine train phases, seven spawns, actions/turns, initial/middle
policy roots and 16 branches with unchanged parents. Coverage rejects an ordinary
battle without persistent auras or without native preview/retained-target evidence.

The byte-identical native binary has 26,528 bytes and 4,444 unique nodes, with no
text source/JSON companion. The SHA-256 is in manifest.tsv. Inventory is now 155
archives: 142 battle fixtures and thirteen calibrations. Whole-battle random pools,
source death/revival, actor/card-changing aura children and broader combat effects
remain outside this fixture's proof; the full simulator remains incomplete.

Complete regression and audit pass for all 155 archives, including 138 policy
chains, 24 physical-position and thirteen decision-reference suites. All archive
sizes/hashes, native provenance and original-file gates pass, with no errors or
unsupported transitions and no tracked source JSON fixtures.

`full-battle-persistent-enchantment-deaths.mt2f` adds actual OnTurnBegin source
deaths and OnDeath gold callbacks to the paid singleton armor policy. Two formerly
bound sources withdraw statuses from live targets, retain skipped corpse map
entries and release their bindings when Unity destruction completes. Later UI
previews leave those source caches unchanged. Native and independent simulation
win in 21 actions/seven EndTurns, Pyre 65, with initial/middle roots and sixteen
branches. All 61 room stages, fourteen train phases, eleven spawns and actions/
turns match. Muted Instant native capture takes 63.43 seconds and has zero errors/
differences/unsupported/pending records and unchanged original-file signatures.

The byte-identical native archive has 32,431 bytes / 5,518 nodes and no text source
or JSON companion. Inventory is 156 archives: 143 battles/thirteen calibrations.
All manifest hashes pass. Validation combines the preceding complete 155-archive
run with the new battle, three affected historical battles, all thirteen
calibrations and pure checks; the complete 156-archive run was not repeated.
Aura-source revival and wider aura/combat mechanics remain unverified.

`full-battle-persistent-enchantment-revivals.mt2f` captures two paid bound sources
consuming their first/last Undying stacks in the ordinary OnTurnBegin FIFO before
normal death. Four natural revival APIs, 24 accepted callback payloads and 24
callback phases match independently in sixteen branches, retaining binding and
exact primary/preview maps and cached status. There are no setup operations or
queue deferrals. The full policy also matches from initial/middle roots and
16 branches, with 21 actions/seven EndTurns, final Pyre 68. Native capture takes
64.85 muted Instant seconds, has zero failures/differences/unsupported/pending
records and preserves original profile/log signatures.

The byte-identical native binary has 34,456 bytes / 5,860 nodes, no text source
or JSON companion. Inventory is 157 archives: 144 battles/thirteen calibrations;
all sizes/hashes pass. The new battle, seven affected historical battles, all
thirteen calibrations and pure checks pass. Evidence builds on the preceding
complete 155-archive run and source-death checks; a complete 157-archive run was
not repeated. Whole-battle random pools, changing aura children and broader
combat mechanics still require implementation and native coverage.

`full-battle-persistent-enchantment-random.mt2f` and
`full-battle-persistent-enchantment-random-deaths.mt2f` use the paid native
armor 2 / regen 1 / buff 1 aura pool on both teams. The ordinary scene wins at
Pyre 80 after fifteen actions/five EndTurns in 51.55 native seconds; the source
death scene wins at Pyre 65 after twenty-one actions/seven EndTurns in 64.72 seconds.
Both muted Instant captures have zero differences/failures/unsupported/pending
records and preserve the original profile/log signatures. Each continuous policy
matches initial/middle roots and sixteen independent parallel branches, including
complete primary/preview caches, both RNG streams and retained identities.

Preview RNG isolation includes the post-preview primary room-order update and its
callbacks. All three cached selections, actual preview draws and whole-scope RNG
restoration are mandatory coverage. Spawn/card/rearrangement/turn order updates and
cross-floor treasure cache propagation now follow the native cadence. These two
byte-identical archives have 29,412 bytes / 4,818 nodes and 33,542 bytes / 5,642 nodes,
without text sources or JSON companions. Inventory is 159: 146 battles and thirteen
calibrations. Random source revival remains unverified because native death dissolve
completion can defer Unity destruction; rejected revival/stage captures are not
curated. Broader aura effects and the complete simulator/optimal solver remain work.

The complete 158-archive run (145 battles/thirteen calibrations) exits zero,
including 141 continuous policies. The final rebuilt model passes nine affected
battles, including both new random-pool archives, plus all thirteen calibrations;
no errors or unsupported transitions occur. All 159 current archive sizes and
SHA-256 values match the manifest and regression inventory. These results combine
the complete prior158 run with the new source-death coverage; a single combined
159-archive regression was not repeated. Rejected destruction-stage changes are
excluded from the verified model.

character-removal-calibration.mt2f observes 55 original removal API transitions,
ten actual destructions and twelve manager queues, without changing gameplay or
timing. The independent model checks all three destruction stages, weak-reference
survival until OnDestroy, primary/preview/temporary references, live attacker
cleanup, numeric/status preservation, pending actors excluded from manager queues,
minimum-ten-frame callbacks and 32 immutable parallel branches. It has 4,282 bytes
and 816 nodes, matches the native binary exactly and has no text source.

The source diagnostic battle still has four whole-decision differences and is
not curated. Passing API calibration does not prove automatic whole-battle
removal scheduling. Inventory is now 160 archives: 146 battles/fourteen
calibrations; existing persistent-aura battles and policies pass separately.

All fourteen calibrations and pure checks pass with the rebuilt model. All 160
archive sizes/hashes and regression-list entries pass; a combined 160-archive
regression was not repeated for the independent removal API model.

full-battle-persistent-enchantment-random-revivals.mt2f enables the explicit native
death-dissolve settlement protocol. The original callbacks complete before the
game's existing turn-end removal flush, preserving their duration and ten-frame
minimum. This resolves the late removed enemy's attacker reference without
overwriting native gameplay, queues or RNG. Arbitrary unsynchronized frame timing
remains outside this stable decision protocol.

The scene wins at Pyre 72 after 21 paid plays/seven EndTurns, including four natural
revivals/24 exact callback phases, 60 room stages, fourteen train phases, eleven
spawns and thirteen card cycles. The initial 65.46-second capture and 63.67-second
repeat are muted Instant runs with zero differences/failures/unsupported/pending
capture records and unchanged original profile/log signatures. Each has six
nonterminal flushes, twelve callbacks and four extra awaited frames. Terminal
visual callbacks may outlive StopCombatLoop; they do not trigger a new flush.
Continuous policies match from initial/middle roots and sixteen immutable branches.

The curated native schema104 binary has 35,337 bytes / 5,846 nodes, no text source,
and the manifest hash matches its raw native provenance. Inventory is now 161
archives: 147 battles/fourteen calibrations. The new settled battle and all
fourteen calibrations/pure checks pass with the final rebuild. A combined161 run is
not claimed; the preceding full158 and affected historical battles provide the
baseline. Rejected unsynchronized revival battles/prototypes remain excluded.

full-battle-persistent-enchantment-upgrades.mt2f and
full-battle-persistent-enchantment-random-upgrades.mt2f cover four actors whose
aura status-change callbacks add the same temporary stat/status/trigger upgrade
twice, remove both copies, then add one permanent upgrade with status-change and
OnHit children. Full states prove actor stats, attributed trigger removal,
monotonic trigger IDs, retained zero-stack definitions and source-card lifetimes.
No setup operations run after preparation; fifteen paid plays/five EndTurns win
at Pyre 80 with 39 room stages, nine train phases, seven spawns and nine cycles.
Initial/middle policies and sixteen immutable branches match. The random scene
selects all three aura pool entries and records 58 isolated native previews.

The model now retains completed queued spell deaths in its shared aura world,
then performs the native once-per-removal-batch aura update and drains its child
callbacks through the existing queue. Complete comparisons include destruction
flags and both Battle/BattleTest streams; the previously missing two random draws
at the lethal spell are restored. Successful muted Instant captures take
61.22/56.38 seconds with zero differences/failures/unsupported/pending records and
unchanged original profile/log signatures. Rejected diagnostics are excluded.

The byte-identical native archives have 27,301 bytes/4,541 nodes and
30,314 bytes/4,970 nodes, without text sources or JSON companions. Inventory is
163 archives: 149 battles/fourteen calibrations. New and historical aura policies,
all fourteen calibrations and pure checks pass with the rebuilt model; the
complete 163-archive regression exits zero with 149 battle suites, fourteen
calibrations and 145 continuous paid policy chains. All inventory sizes/hashes,
byte-identical native provenance, PowerShell syntax and whitespace checks pass.

Four enchantment-summon[-fresh|-random|-random-fresh]-calibration.mt2f archives
observe original queued SpawnMonster applications whose new units carry persistent
auras. Each contains two batches/four births/51 full callback payloads and matches
complete room/world/source-card/physical/RNG state in 32 immutable branches. The
copied variants additionally compare four detached source-card clones each; fresh
variants use the matching child fallback definition. The model now performs the
original final room-order aura update after a triggered summon batch, before
queued OnSpawn callbacks drain.

These are component calibrations with WholeBattleVerified=false, not accepted
new full-battle policies. Preview-only births retained by aura dictionaries still
expose incomplete actor retention, source-cache restoration, synthetic identities
and preview physical references. Rejected diagnostic battles are not curated.
The four binaries have 7,258/6,772/7,721/7,124 bytes and 1,140/1,027/1,168/1,049
nodes respectively, match native bytes exactly and have no JSON companions or
text sources. Inventory is now 167: 149 battles and eighteen calibrations.

Validation includes all 22 historical triggered-summon/persistent-aura battles,
their continuous policies, all fourteen historical calibrations/pure checks and
the four new curated calibrations/pure checks. All 167 hashes/sizes, regression
entries and PowerShell syntax pass. A combined167 run is not claimed; the prior
complete163 run remains the baseline. The failed new whole-battle diagnostics
are excluded.

preview-reference-calibration.mt2f observes nineteen original native combat-preview
actor restorations and one temporary birth before EnableCombatPreviews. Independent
checks compare complete states, the cleared preview-overwritten first-born cache,
a preserved real-unit cache, primary once flags and aura dictionaries retaining
the temporary target. Thirty-two immutable parallel branches pass. The native
birth's primary origin has attack 8/HP 30 and no applied stat upgrades or statuses;
its temporary point is room 0/index 2 and original removal reaches stage 3.

The model now restores summon and aura effect state in the same shared world,
preventing a later refresh from reintroducing the old summon cache. The archive is
a byte-identical, source-free native binary with 3,787 bytes/580 nodes. It explicitly
keeps FullPreviewSimulationVerified=false: complete preview-born actor retention,
physical position planes and identity advancement remain open, and rejected
whole-battle diagnostics are excluded. Inventory is now 168 archives: 149 battles
and nineteen calibrations.

The rebuilt model passes thirty affected historical battle suites, 28 continuous
paid policy chains, all nineteen calibrations and pure checks. All 168 inventory
hashes/sizes and regression entries match, and the new binary equals its raw
native source. Probe has zero build warnings/errors; the checker keeps fourteen
existing nullable warnings with no errors. A full168 run is not claimed; the
earlier complete163 regression remains the full baseline.

Four preview-birth[-fresh|-random|-random-fresh]-calibration.mt2f archives observe
the original lifecycle of 9/9/18/17 preview-only aura summons. Independent checks
initialize each primary origin from its character/card/ability inputs, then
restore from the observed preview actor and compare complete original On and
OnDestroy states in 32 immutable branches. Native attack 8/HP 30 restores from
preview attack 9/HP 32; preview upgrades/statuses vanish, shared endless immunity
and aura maps remain, and destruction bypasses ordinary death/listener flags.

The model retains these primary origins internally, carries referenced newborns,
their detached source cards and observed identity counters through automatic aura
previews, and restores card room caches against the surviving primary actors.
The observer assigns birth/source identities before subsequent room captures.
Raw temporary position planes and a late removed actor's attacker reference still
prevent acceptance of the new whole battles. The latest fixed diagnostic chain
matches nine paid actions and two EndTurns before that remaining difference.

All four lifecycle captures have zero errors and WholeBattleVerified=false;
their entire rejected battle traces are excluded. The muted Instant native runs
preserve original profile/log signatures. Native binary copies are byte-identical,
have no text source or JSON companions, and measure 3,193/3,151/4,067/4,068 bytes
with 396/396/572/566 nodes. Inventory is 172: 149 accepted battles/23 calibrations.

The final build passes thirty affected historical battles, 28 continuous paid
policy chains, the earlier nineteen calibrations/pure checks and four new
lifecycle calibrations. All 172 manifest hashes/sizes and regression entries
match. Probe builds without warnings/errors; ModelChecks keeps fourteen existing
nullable warnings and no errors. A combined172 run is not claimed; the previous
complete163 regression remains the full baseline.

physical-plane-calibration.mt2f captures 100 original SetSpawnPoint assignments and
40 original copied-list compactions. Complete before/after worlds match in 32
immutable branches, including ten distinct copied lists, cross-plane ownership,
preview-born detachment, retained point pointers and native pivot counts. The model
keys positions by room/team/PreviewCopyId/index; null denotes the primary layout.
The observer preserves historical copied points and all three actor-state planes.
Copied-slot actor closure is observed before freezing inputs, correcting a rejected
prototype whose last copied group retained actor 27 outside its unit inventory.

This byte-identical native binary has 4,032 bytes/825 nodes, no text source or JSON
companion, zero incomplete/differing samples and WholeBattleVerified=false. Inventory
is 173: 149 accepted battles and 24 component calibrations. The final muted Instant
native run preserves original profile/log signatures, removes all ten former
physical capture failures and independently verifies 370 raw/canonical decisions
with 11,835 primary/preview/temporary mappings. Original dissolve settlement resolves
the third-turn attacker gap; terminal aura/preview/cache differences still reject
the full battle, which is excluded from the accepted inventory.

All 31 affected historical battles/29 paid policies, 23 earlier calibrations/pure
checks, the new calibration, binary provenance, all 173 manifest entries and script
syntax pass. A combined173 regression is not claimed; complete163 remains the full
baseline.

The latest temporary Boss-preview model preserves newborn preview states, shared
card-room targets and last-spawned references, skips primary spawn statistics in
preview, and cancels retained preview births when terminal ClearCards runs.
An opt-in schema105 card-animation settlement protocol waits for original
temporary-pool movements and discard completion before previews/policy input.
It changes preview start timing; arbitrary unmodified frame timing remains open.

Two new diagnostic battles independently match fifteen paid actions, five EndTurns
and complete canonical terminal states, including sixteen immutable parallel
branches. The latest muted Instant run takes 162.27 seconds, preserves original
profile/log signatures, and has zero capture failures/unsupported effects/pending
records. Its 370 raw/canonical decision mappings and 11,785 position-plane mappings
pass. Three intermediate copied-group/callback-boundary differences still reject
full-battle verification, so these traces remain outside the curated inventory.

The final model passes 32 affected historical battles, 30 continuous paid policies,
all 24 calibrations and pure checks. Inventory remains 173 binary archives:
149 accepted battles and 24 component calibrations. A combined173 regression was
not repeated; complete163 remains the full regression baseline.

`full-battle-persistent-enchantment-summons.mt2f` now accepts the complete battle
under schema105 and explicit original death/card callback settlement. All fifteen
paid actions, five EndTurns, 41 room stages, nine train phases and seven spawns
match independently, including the terminal Pyre80 state, a mid-battle root and
sixteen immutable parallel branches. Four nested aura births and six bound
sources retain detached source cards, shared weak targets, native copied-list
allocation counters and selected/primary position planes. All 154 Before/Actual
contexts have complete registries and counters; 370 complete raw/canonical
decisions and 11,785 position-plane mappings pass.

The final muted Instant native recording takes 162.55 seconds and has zero
differences, capture failures, unsupported paths or pending records; original
profile/log signatures remain unchanged. The archive is a byte-identical native
binary, has no text source or fixture JSON companion, and contains 39,816 bytes
and 7,342 unique nodes. Card metadata records 24 original movements scheduled and
none pending. This acceptance applies to the explicitly settled protocol;
arbitrary unmodified frame timing and broader combat mechanics remain open.

`full-battle-persistent-enchantment-summons-fresh.mt2f` independently covers the
fresh-source variant of the same aura battle. Four nested child births create
new source cards instead of copying the previous source's runtime state. All
fifteen paid actions, five EndTurns, 41 room stages, 154 closed contexts and the
complete initial/mid-battle policies match, with sixteen immutable branches and
Pyre80. The muted Instant native run takes 181.73 seconds, has zero capture
failures/differences/unsupported/pending records and preserves original profile
signatures. Its byte-identical source-free native binary is 39,470 bytes with
7,242 nodes.

The completed historical baseline checks every one of its 173 archives:
149 complete battle suites, 145 continuous paid policies, four complete
no-more-card chains and all 24 calibrations. No room, card-cycle, train, spawn,
EndTurn or action operation is skipped as unsupported. This run uses Model.dll
SHA-256 59153b6d1959e90bd39ee4de0ba729863a628c616f8a0cd6408ee1900e8f2ffd;
the later retained-preview callback model is being verified separately.

`full-battle-persistent-enchantment-random-summons.mt2f` accepts schema105 with
three randomly selected aura statuses, four nested child births, six bound
sources and retained preview actors outside the selected room list. Local aura
callbacks use the current retained state; temporary Boss previews preserve
complete states for actors outside their restore list, including withdrawn
statuses and once flags. All fifteen paid actions, five EndTurns, 41 room stages,
nine train phases, seven spawns and 154 closed contexts match independently.
The complete initial/mid-battle policies match Pyre80 in sixteen branches;
370 raw/canonical decisions and 14,385 position-plane mappings also pass.

The muted Instant native run takes 211.59 seconds, completes all 23 scheduled
original card-movement callbacks, has zero capture failures, differences,
unsupported operations or pending records, and preserves original profile/log
signatures. Its byte-identical source-free binary is 47,789 bytes with 8,710
nodes. The final model also passes all 58 affected historical archives:
34 battle suites, 32 paid policies, two no-more-card chains and 24 calibrations.
This is an explicitly settled recording; broader native timing and combat
coverage remain work. The three previously rejected random diagnostics are
outside the curated inventory.

`full-battle-persistent-enchantment-random-summons-fresh.mt2f` independently covers
random aura statuses with four fresh child source cards and no source copies.
The current model preserves explicit retained callbacks while restricting
ordinary attacks, team/unit/post-combat callbacks, status decay and relentless
continuation to actual room members. All 154 closed contexts, fifteen paid
actions, five EndTurns and complete intermediate/terminal states match, including
the initial/mid-battle policies and sixteen immutable parallel branches ending
with Pyre80. All 370 raw/canonical decisions and 14,385 position-plane mappings
also pass.

The muted Instant native run takes 218.97 seconds, has zero capture failures,
differences, unsupported operations or pending records, completes all 23 original
card-movement callbacks and preserves original profile/log signatures. Its
byte-identical source-free binary is 47,591 bytes with 8,608 unique nodes. The
current model separately passes 59 affected archives: 35 complete battles,
33 paid policies, two no-more-card chains and all 24 calibrations. The complete
battle simulator and optimal solver remain in progress.

The completed room-membership baseline now passes all 177 binary archives:
153 complete battle suites, 149 continuous paid policies, four complete
no-more-card chains and all 24 component calibrations. Each complete policy
checks independent initial/mid-battle roots and sixteen parallel branches.
No recorded room/card-cycle/train/spawn/EndTurn/action operation is unsupported
or skipped, and all manifest sizes and SHA-256 hashes are verified. The isolated
runner uses Model.dll SHA-256
d46c6961737888c3b84bfb883c5cdbfd85741536ea3dee8400b6cb91c6a68cbb;
the new Purify work is being validated separately.

`full-battle-purify.mt2f` records ordered starting statuses, active Purify blocking
positive/zero/negative additions, zero-stack clearing, explicit removal and
reapplication, native trigger admission and six real paid room spells. Accepted
callbacks survive later purification, while newly forbidden status/combat
callbacks are absent from the queue. All 159 FIFO dispatches and eighteen status
boundaries match independently, including exact dictionary/zero definitions,
source statistics and eight explicit removal effects.

The muted Instant native battle takes 86.30 seconds and preserves the original
Boss/waves and profile/log signatures, with zero capture failures, differences,
unsupported operations or pending records. All 61 room stages, 21 paid actions,
seven EndTurns, 228 closed contexts and complete initial/mid-battle policies match
in sixteen immutable branches ending with Pyre65. Its source-free binary is
42,703 bytes with 7,162 nodes, copied byte-identically from native capture.
The final Purify model separately passes 27 historical battles and all 24
calibrations; the completed177 baseline remains tied to the earlier model.

The -PurifyQueues native probe extends the Purify scene with ordinary birth,
AfterSpawnEnchant, Rally, Sentry, Harvest and death rewards. The final muted
Instant battle takes 81.00 seconds with zero differences, capture failures,
unsupported operations or pending records. Complete independent replay passes
21 paid actions, seven EndTurns, 61 room stages, 228 closed contexts, both policies
and sixteen parallel branches ending with Pyre65. All 759 character-overload queue
requests match captured rules and original queue-count deltas, including purified
birth/Rally/Sentry/Harvest/death rejection. No queue-data overload is observed.

Removal entries preserve physical death/spawner cleanup while gating the actual
OnDeath request at removal time. Accepted batches survive later purification and
branch-local retained actor updates. Pure tests and two deliberately broken models
verify these admission/death boundaries. Final Model.dll SHA-256 is
fb6341d949d6b98027472cf3735bd5dda0ed9540351b22221b0cf9d61c432a3f;
its final regression passes 29 complete battles, 28 continuous paid-policy chains,
one no-more-card chain and all 24 curated component calibrations. The incidental new removal
calibration lacks its required nonempty pending-dissolve queue observation and is
not curated; existing calibration gates remain strict. Horde/aura births with
Purify and the complete simulator remain open.

The status-Purify version has completed all 178 required archives: 154 accepted
battles, 150 continuous paid-policy chains, four no-more-card chains and all 24
component calibrations. No recorded room/card-cycle/train/spawn/EndTurn/action
operation is unsupported or skipped. All input identities, archive sizes and
manifest SHA-256 hashes pass. This complete178 baseline uses Model.dll SHA-256
e546dcf00cbda02d12431a4289be8b24b864a0f8cc7062bbaa7180530854ff0b;
the newer removal/admission version is validated separately.

full-battle-purify-queues.mt2f is a byte-identical native archive: 49,314 bytes,
8,697 nodes and SHA-256
1b852c9c28642bbbc254cee356c143e8112ae3d1336a023e6b81665a83f6fc20, without a text source.
The manifest and required list contain 179 archives: 155 accepted battles and 24
component calibrations. The complete178 baseline belongs to the preceding model;
the new queue/removal model's combined179 regression has now completed: 155
accepted battles, 151 continuous paid-policy chains, four no-more-card chains and
all 24 strict component calibrations, with no unsupported or skipped native
operation. All 179 required identities, archive sizes and manifest SHA-256 hashes
are independently rechecked. This baseline uses Model.dll SHA-256
fb6341d949d6b98027472cf3735bd5dda0ed9540351b22221b0cf9d61c432a3f;
the subsequent Incant changes require separate validation.

The Incant model captures native card types and ability flags, retains original
room actor identities, admits/drains players before enemies, and supports empty
and summon-effect spells through the ordinary CardSpellPlayed queue. Equipment,
monster and default unit/room ability classifications are checked in the pure
suite; the native scene records paid ordinary spells, natural units, status
children, silence removal and exact Purify admission.

The final muted Instant Incant recording takes 87.29 seconds with original profile
and log signatures preserved. It independently matches 32 team phases, 18 actor
dispatches, 51 original admission requests with 12 Purify rejections, eleven paid
empty-effect spells, 139 FIFO status callbacks and 23 removal-effect boundaries.
Thirty-two independent branches retain their parents. Two deliberately incorrect
current-source models fail on native empty-spell routing and cached actor scope.
The same model also passes 29 historical battles, 28 paid policies, one no-more-card
chain and all 24 strict component calibrations (53 unique inputs). Model.dll SHA-256:
e26db6f690855eadc4adc90ec5dd2feaf5570de1a1a9695ab826195e5f5b5cea.
The preceding complete179 baseline is not a combined regression of this Incant
version. That version refused positive Incant thresholds; relic-triggered ability Incant and the whole
simulator remain unfinished.

`full-battle-incant.mt2f` is the byte-identical final native Incant recording:
49,010 bytes, 8,274 unique nodes and no text-source provenance. SHA-256:
01812c137f392da5b24408df6999705c26b45d2b243cb871784713c1e5409d4c.
The required list and manifest now contain 180 archives: 156 accepted battles and
24 strict component calibrations. The completed179 baseline belongs to the prior
Purify queue model; the Incant model's combined180 regression is pending.

The threshold Incant version now accepts positive CardSpellPlayed thresholds while
preserving the native default-zero card-play argument. Its -IncantThresholds
recording observes a skipped positive threshold, fired zero/negative thresholds
and spent negative-once callbacks. The muted Instant native battle takes 97.95
seconds, with the original Boss/waves and profile/log signatures preserved and no
capture failure, mismatch, unsupported operation or pending record.

All 21 paid plays/seven EndTurns, 32 team phases, 18 actor dispatches, 51 native
admissions with 12 Purify rejections, eleven paid empty spells, 139 FIFO status
callbacks and 23 explicit removals match independently. Thirty-two parallel
component branches retain their parents; sixteen complete branches end at
Pyre61. An intentionally incorrect current-source model loses the positive
observer's untouched HasTriggered flag. Equality/above-threshold arguments and
zero/negative repeat batches are covered by pure/source-derived checks, not by
this ordinary-card native recording, whose argument/count are always zero/one.

The final threshold model also passes 30 historical battles, 29 paid-policy chains,
one no-more-card chain and all 24 strict component calibrations (54 unique inputs).
Model.dll SHA-256:
ab9e8f6fe6b86fb4d2b66075621d6c3719c385616f51363dbcc798020528db96.
The earlier combined180 run belongs to the initial Incant model e26db6f6. Further
relic/ability/card/trigger combinations and the complete simulator remain open.

`full-battle-incant-thresholds.mt2f` is the byte-identical final native recording:
49,221 bytes, 8,328 unique nodes, no text-source provenance and SHA-256
8cb07e36205d0f19db5a5f1fdc40cab8489b082eb12eb956762dae396f31d13f.
The manifest and required list now contain 181 archives: 157 accepted battles and
24 strict component calibrations. The threshold model's combined181 regression is
pending; the earlier combined180 run remains bound to the initial Incant model.


### Complete initial Incant regression baseline

The isolated initial Incant model's combined180 run has completed successfully:
156 accepted complete battles, 152 paid policies, four no-more-card chains and
24 strict component calibrations. Every recorded room, card-cycle, train, spawn,
turn and paid action is modeled, with zero unsupported or skipped operations.
The 180 explicit input identities are unique; their archive sizes and SHA-256
hashes match the manifest. Native calibration output includes all 24 required
suites, including the four aura-summon and four preview-birth variants.

This baseline belongs to Model.dll SHA-256
E26DB6F690855EADC4ADC90EC5DD2FEAF5570DE1A1A9695AB826195E5F5B5CEA,
retained in .probe-runs/incant-verified-checks. Evidence is the terminal-zero
.probe-runs/incant-complete-regression.log and its explicit
.probe-runs/incant-complete-inputs.txt inventory. The subsequent threshold
model's combined181 run remains separate and running; ongoing relic work is
not covered by this initial Incant baseline. The whole battle model remains
incomplete for the explicitly refused mechanics.


### Complete Incant threshold regression baseline

The isolated threshold model's combined181 run has completed with terminal exit
zero: 157 accepted complete battles, 153 paid policies, four no-more-card chains
and 24 strict component calibrations. No recorded room, card-cycle, train, spawn,
turn or paid action is unsupported or skipped. All 181 unique required archive
identities, sizes and SHA-256 hashes match the complete manifest. The final
calibration output includes all 24 required suites.

This baseline belongs to Model.dll SHA-256
AB9E8F6FE6B86FB4D2B66075621D6C3719C385616F51363DBCC798020528DB96,
retained in .probe-runs/incant-threshold-final-checks. Evidence is
.probe-runs/incant-threshold-complete-regression.log and its explicit
.probe-runs/incant-threshold-complete-inputs.txt inventory. The initial Incant
model's completed180 baseline remains separately bound to e26db6f6. Ongoing
relic/combined-prefix work requires its own final-binary native and historical
validation; neither completed baseline establishes that new model or completion
of the whole battle simulation.


### Relic-enabled ability Incant and combined card-play queues

The model now retains ordered active relic identities and runtime effect types,
including the complete list searched by RelicManager.GetRelicEffect: hero
blessings, collected artifacts, covenants, mutators, Pyre artifacts and souls.
All production context copies preserve this immutable list. Only the native
RelicEffectIncantTriggeredByUnitAbilities marker is modeled here; other active
effect types are explicitly refused by the model and captures. Duplicate marker
instances remain a boolean presence query, without multiplying Incant.

Unit skills cache the selected room after PreOwn and before effects, keep their
resolving flag and paid-cost queries through both card-play manager phases,
and queue Own then Incant per actor in creation order. The entire player batch
is admitted before any callback runs; enemies are admitted after the player
batch drains. Existing accepted effect callbacks stay at the head of this same
batch. Ordinary spells and Rally also retain those prefixes; draining them
separately can change Purify admission, status children and trigger flags.

The game 2.2.1 asset catalog contains no collectable relic using this effect
class. The -AbilityIncant scenario therefore creates an isolated test artifact
PojuNativeAbilityIncantRelic (id c2f6ed7f-18ce-4070-b65f-7dd9f5160068), registers
it through the native SaveManager.AddRelic pipeline and runs the original native
effect class. Its setup builds on the actual shared-skill activation scene,
including that scene's modified Boss ability, and preserves the natural wave
pattern. It adds Incant observers to owned Stewards and naturally scheduled
ordinary enemies. This is an authored test asset, not a naturally obtainable
artifact in this game build.

The final muted Instant native recording takes 106.41 seconds and retains schema
106: 17 plays, five EndTurns, two actual activators sharing one skill, 20 manager
phases, 25 Incant dispatches, 242 status callbacks and 780 original queue-admission
requests. Final Pyre is 80. Native capture reports zero failures, discrepancies,
unsupported stages or pending records, and original profile/log files are
unchanged. An ordinary cooldown spell exposes 34 accepted prefix callbacks.
Independent exact action/component checks and 32 phase/dispatch branches pass.

Evidence is .probe-runs/ability-incant-native-final.log and
.probe-runs/full-battle-units-spells-and-junk-20261009-170051-2fe13390/full-battle.mt2f.
The --ability-incant-only check passes against that recording in
.probe-runs/ability-incant-component-final.log. Pure checks cover absent,
duplicate and unknown relics, missing card classification, post-PreOwn lethal
cache admission, skill allocation, per-actor Own/Incant order, accepted prefixes,
Purify and parent isolation. Source-mutation negatives that ignore the marker
or omit the prefix fail on native gold or trigger flags respectively.

The final model SHA-256 is
F418B74A91DF12254ECBA6A1EF2A9AC3EA88FAA11D84DD5ED687590CB984B7FF,
retained in .probe-runs/ability-incant-final-checks. Its 37-input targeted
historical run has completed: 13 affected battles/paid policies and all 24 strict
calibrations, with archive identities, sizes and hashes verified. Evidence is
.probe-runs/ability-incant-prefix-historical.log and its explicit input list.
The separately completed 181-input threshold baseline belongs to ab9e8f6f;
it does not establish a complete regression of this relic model.

The new recording is NOT accepted into the curated inventory yet. The complete
independent policy fails after action 2 because native floor selection performs
an extra interface preview between plays: previous action's next copied-list
id is 41, next native action starts at 49 and finishes at 57, while the continuous
model finishes at 49. All isolated plays and EndTurns match, but the missing
selection transition prevents claiming complete replay from the initial root.
The original full-policy and physical-reference comparisons remain strict;
no oracle state or recorded actions are injected into the model to hide this
difference. Room-selection preview modeling, further relic effects, unmodeled
card/trigger/effect combinations and the whole battle simulation remain work.

`full-battle-relic-card-modifiers.mt2f` is the accepted muted Instant native
recording for original relic card-side temporary upgrades: 51,448 bytes, 10,080
unique nodes and SHA-256 `2c8e68457d536a04f07aa6bf66ece80b76dd730e36034290ea15f2aaa2751f31`.
It preserves an 18-play, five-EndTurn winning battle with original
`ReduceStarterCost`, 124 exact relic-manager calls, ordered filter/effect results,
49 trigger notifications and an eligible generated card carrying the relic
upgrade. Independent manager replay and 32 immutable branches pass. The battle
capture has zero failures, pending records or unsupported stages, and original
game files remain unchanged. Run its focused check with
`pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-relic-card-modifiers.mt2f`.

The complete 200-input regression passes: 169 battles (165 paid/four no-more-card
chains), 31 ordered component calibrations and seven branch-card oracles with
3,540 decision contexts, 103,940 card views and 61,735 upgraded observations.
Every input archive matches its manifest size/SHA-256; all core transitions are
supported. The run exited 0 at `2026-10-10T16:41:19.3333019Z` UTC on commit
`8b938988600f7890c13d28035b6dbb8493ae7f7a`. Model SHA-256 is
`9d8df99fb2c11cf16c7e902453c0c9ca49d221af036023d0e06ef1c18c3cb48e` and
ModelChecks SHA-256 is
`3b0e6ec1da26542a91f71b5c27cc0498f22470dc85b213e54967b249b8b60b87`.

`full-battle-room-selection-preview.mt2f` adds native selected-room state to the
schema108 battle recording. It captures the floor-selection preview between
cross-room actions and the selected floor after EndTurn. The muted Instant battle
wins with 17 actions, five EndTurns, zero capture failures/mismatches/unsupported
stages/pending records; independent replay matches all actions and turns and
finishes at Pyre80. The archive is 51,499 bytes/8,650 nodes, SHA-256
`b037950662c2cc337101ac9e7e6cb6fe0773815bef1d2e2b4f754afa9c817950`, with no
JSON source. Its focused regression passes. The complete 201-archive stage run
also passes in about 38 minutes: 170 accepted battles and 31 component
calibrations; every archive matches the integrity manifest.

`full-battle-cardless-relic-upgrades.mt2f` adds the original `PyreHeartSon`
relic to the clone-birth scenario with one ordinary policy card play. Its 22 native
`RelicManager.CharacterAdded` calls match: the +5 damage/+12 health upgrade is
applied to five ordinary cardless births and skipped on five cardless clones.
The Instant battle wins with 42 matched stages, five EndTurns, zero capture
failures, mismatches, unsupported stages or pending records. The binary-only
archive is 56,537 bytes/7,789 nodes, SHA-256
`f0d0cc92cabcd364a3023150c9c0bdbaa10ea81731ee7bacd40a79f72dbb2380`. Run its
focused regression with
`pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-cardless-relic-upgrades.mt2f`.

`full-battle-conditional-relic-card-upgrades.mt2f` combines the original
`ReduceStarterCost` relic with a native card-generation battle and a controlled
ThisTurn, one-trigger condition. Its 109 manager calls match, including the
spent-condition skip and reset before a later turn; generated-card state and 16
parallel policy branches also pass. The muted Instant battle wins with 18 actions
and six EndTurns, across 49 matched stages with no capture failures, mismatches,
unsupported stages or pending records. The binary-only archive is 60,444 bytes/
11,473 nodes, SHA-256
`25b1ed257205c6539dff32b08b795ed0d0ebb4b3c4631ac07b4cc337e87967d7`. Run its
focused regression with
`pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-conditional-relic-card-upgrades.mt2f`.

The complete 203-archive regression exits zero: 172 accepted battles and 31
component calibrations pass, and every archive matches the integrity manifest.

`full-battle-relic-card-piercing-upgrades.mt2f` captures the original
`StingBuffPiercing` trait applied by a relic to eligible monster cards. The
muted Instant battle wins with 27 matched plays, six EndTurns and 585 complete
branch-mask contexts; five native relic manager calls, all 49 battle stages,
and all 32 immutable model branches pass with zero capture failures,
mismatches, unsupported stages or pending records. The archive is 28,105 bytes/
4,641 nodes, SHA-256 `2501705d0562a618146c81ecf5d99b9386143ac0707bedb84ca3cc8c2baa9f41`.
Its focused check passes; the full 206-archive regression remained deferred at
that milestone.

The relic reset adapter now preserves existing upgrade lifecycle data while
rebuilding an ordered `RelicEffectAddTempUpgrade` sequence. A pure regression
confirms that a permanent removed-trait rule remains active after reset and
still blocks a later relic filter. The focused status-upgrade and piercing
fixtures both pass after this change; the full 206-archive regression remains
deferred to a completed milestone.

### Temporary relic SelfPurge routing

`full-battle-relic-card-self-purge-upgrades.mt2f` captures native
`RelicEffectAddTempUpgrade` adding `CardTraitSelfPurge` to eligible spells while
the same upgrade both adds and removes `CardTraitIgnoreArmor` and
`CardTraitExhaustState`. Native trait composition suppresses both additions and
retains SelfPurge. Two played cards
enter the Purged pile and leave permanent deck membership. The full native
battle wins with 51 stages and 28 actions, zero mismatches, unsupported
transitions, capture failures or pending records. Seven relic-manager calls,
600 decision contexts, 32 immutable search branches and the focused card-cycle,
relic and battle-search checks pass.

### Temporary relic unit-ability upgrades

`full-battle-relic-card-ability-upgrades.mt2f` uses the real
`StewardSacrificeDamage_Upgrade` payload from the native relic catalog. The
captured relic manager adds the ability upgrade, resets the temporary group and
filters other owned cards. One played Train Steward summons with the same unit
ability. The battle wins with 49 matched room stages and 28 matched actions; all
five manager calls and 32 immutable branches pass with zero capture failures,
mismatches, unsupported transitions or pending records. The focused fixture
passes; the full 208-archive regression remains deferred to a completed
milestone.

`full-battle-original-relic-batch.mt2f` validates four unchanged catalog artifacts
in one muted Instant battle: MoltenManifold, SpawnWithRage, UnitsGainLifesteal and
ContingencyPlans. The fixture authors X-cost spells and gives one Steward type
the Monstrosity subtype; the other type witnesses rejection. All 30 native relic
manager calls and 12 births match, including non-unique repeat stacking, reset,
five Rage applications, five Lifesteal applications and two Spikes applications.
Sixteen X-cost plays include nine positive payments and twenty independent
damage-scaling counterfactuals. The complete 21-play/five-EndTurn policy wins at
80 Pyre health and passes 16 parallel branches; modifier queries pass 32 branches.
There are zero native capture failures, mismatches, unsupported or pending records.
The refreshed schema-103 archive comes from the 2026-10-12 native capture at
`.probe-runs/full-battle-units-spells-and-junk-20261012-162810-9262b85a`.
It is 30,061 bytes/4,781 nodes, SHA-256
`61a94ec4d156357fb850748f42bb26faa7118df184bedf91a5e8033e5e9a7be3`, with no
JSON companion.

The associated reset fix preserves permanent upgrade lifecycle metadata when
replacing numeric values; repeated resets and the prior piercing-relic fixture
pass. The prior 210-archive milestone was completed as a 159-fixture passing
prefix, four affected checks after the retired-owner-pointer fix, and a passing
51-fixture suffix (20 full battles and 31 calibrations, 765.53 seconds). This is
split regression evidence, not a fresh 210-fixture run on one final binary.
At that milestone the inventory contained 211 archives; the new batch received
focused checks.

### Typed draw, shared upgrades and Consume

`full-battle-typed-draw.mt2f` validates `CardEffectDrawType` and
`CardEffectAddTempCardUpgradeToNextDrawnCard` in one muted Instant battle.
Channelsong and GiftsForAGuard retain their original effects, costs and upgrades;
both actually play and exhaust. One originally non-Consume recipient drawn by
Gifts also actually plays, exhausts and increments native exhaustion statistics.
The authored JustCause carriers exercise primary/secondary filters, zero and
negative counts, ranged draws, full hands, duplicate upgrade definitions,
uniqueness and filter short-circuiting. One carrier has an instance-specific
StrongerMagicPower upgrade to expose different multipliers on shared owners.

All 57 typed-draw records, 32 schedules and 100 individual upgraded draw states
match complete independently replayed contexts. The sample includes eight
fallbacks, five ranges, five unique suppressions, 86 full-hand refusals and six
magic-power draws. Seven completed OnCardPlayed boundaries show the queue
clearing. Two installed upgrades refresh their still-pending queue objects;
three refresh observations update multiple shared card owners after the queue
is empty.
Explicit captured reference IDs survive serialization and preserve immutable
parallel branches. Native copies receive fresh private objects even when their
upgrade definitions match.

The complete 23-play/five-EndTurn policy wins with Pyre health 80 and passes
16 parallel branches. The 490 captured decision contexts additionally match
17,080 native card queries and 15,885 upgraded observations in 32 query branches.
All 19 captured starter-relic manager calls are compared exactly. Its dedicated
fixture retains the separate reset, rejection and successful-generation gates;
this composite fixture acquires that relic before the recording starts.
There are zero native capture failures, mismatches, unsupported or pending
records. Native sampling takes 147.81 seconds.

The byte-identical archive is 45,763 bytes/8,470 nodes, SHA-256
`f1e6c1c7285051415a07b2e89f19a74de6bff280058062545b03d94f2b728c4e`.
The complete new fixture, three new pure suites, the existing strict relic
manager check and four affected fixtures (drawing, generation, bonus draw and
equipment exhaustion) pass. A full 212-archive rerun remains deferred.
These accepted paths do not imply complete card coverage: new permanent Consume
installation, unsupported Consume summon callbacks and source-driven next-draw
scaling remain explicit guards.

Focused commands:

```powershell
dotnet run --project src/ModelChecks -c Release -- --typed-draw-pure
dotnet run --project src/ModelChecks -c Release -- --typed-draw-only tests/fixtures/full-battle-typed-draw.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-typed-draw.mt2f
```

### Original unit relic batch

`full-battle-original-unit-relics.mt2f` exercises four unchanged catalog artifacts
in one muted Instant battle: `ImpDamageBuff`, `StewardSacrificeDamage`,
`MorselsGetDamageShield` and `ValorTransferOnAngelDeath`. Only the carrier unit
subtypes, card effects and equipment trigger are authored. Original relic IDs,
upgrade IDs, subtype filters and the cardless-only spawn effect are retained.

The strict check compares 40 complete modifier calls, ordered filtering,
unique rejection, resets and notifications in 32 immutable branches. Three
paid and three cardless Morsel births distinguish the one starting shield from
the additional cardless-only shield. Twenty-eight native Shift queries cover
player count 2, enemy body count 1, enemy equipment count 2 and excluded trigger
count 1; the exclusion probe restores the original flag and rechecks the count.
Eight player shifts and one equipped-enemy shift double the moved unit's
five-gold reward while also matching the other same-team shift callbacks.

Six original Steward abilities actually sacrifice their owners; three damage
enemies. Paid source cards exhaust, while temporary cardless source copies leave
ownership and retain only their registry history. Twenty-seven exchange
observations exercise original Demon Multistrike, and one shield-block
observation preserves Morsel health while consuming its shield. Complete native
states additionally cover queued sacrifice callbacks before physical-position
centering and the lifecycle descriptor on an equipment's anonymous aggregate.

The 40-action/seven-EndTurn policy battle wins with Pyre health 78. Four opening
actions are fixture preparation before the native equipment and Bump cards are
injected; the independent policy starts at that recorded turn-one decision and
replays 36 card plays and six EndTurns. Native sampling takes 206.00 seconds with
zero capture failures, mismatches, unsupported or pending records; original
installation files are unchanged. The independent suffix, 16 policy branches
and all recorded component checks pass. Eight affected historical fixtures pass
across the equipment and sacrifice changes. A full 240-archive rerun remains
deferred under the incremental workflow.

The native archive uses schema 103, has 69,249 bytes/11,309 nodes and SHA-256
`5f0eb05bb64b8716ee6a28db7d7024e9d61d9e4a02058202262bed761d3e3794`. There are
no JSON companions. See [relic-coverage.md](relic-coverage.md) for the 60/224
original-relic coverage scope and effect witnesses.

```powershell
dotnet run --project src/ModelChecks -c Release -- --original-unit-relics-only tests/fixtures/full-battle-original-unit-relics.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-unit-relics.mt2f
```

### Original added-trigger relic batch

Two byte-identical native archives validate nine additional unchanged original
owners of `RelicEffectAddTrigger`: `EquippedUnitsGainArmor`, `DamageShieldOnEquip`,
`ManyBuffonAngelEquip`, `SurgeonsStitcher`, `AngelsSpawnWithShiftyValor`,
`RageOnHymnistDeath`, `WhelpsGainExtinguishHealPyre`, `DragonsGainStrikeAddPyregel`
and `WhelpStrikeAddPyregel_DH`. Relic definitions and trigger payloads are
unchanged. Unit subtypes, damage/equipment/Bump carriers and an additional
equipment-count relic are authored; the original +1 Death and Shift relics are
also present and are not counted again.

The earlier stationary archive supplies the natural Angel-targeted death
witness. The later physical archive supplies actual Shift and enemy equipment
witnesses. Each has eight positive owner effects; the combined check requires
all nine across the pair. An effect witness removes only that owner's installed
callbacks and requires a difference in actual unit health, attack, membership or
status stacks. Changes only to trigger identities or flags do not count.

Both fixtures compare all nine ordered installation definitions, complete
CharacterAdded states, negative team/subtype gates and 32 immutable branches.
Fourteen births per archive include enemy Angels; player equipment Armor fires
twice while original excluded equipment, Death and Shift callbacks retain fire
count 1. The physical fixture plays three equipment cards, including one on an
enemy, restores a wounded 68/80 player to 80/80 and records ten Bump plays.

The stationary battle has 13 plays, five EndTurns, 35 matched stages and final
Pyre health 80; native sampling takes 78.56 seconds. Its schema 103 archive is
45,490 bytes/6,244 nodes, SHA-256
`e22ebd80a7f827948a97e7cb444803f4df3b96a7885d682c31b82e533aa369a6`.
It records the earlier policy with no physical movement metadata or weak-Whelp
damage step; its independent chooser preserves that recorded policy.

The physical battle has 28 plays, seven EndTurns, 54 matched stages and final
Pyre health 75; native sampling takes 102.72 seconds. Its schema 106 archive is
65,645 bytes/10,228 nodes, SHA-256
`6c40aafc15c0393fb0c1d139a1865529c18ff14cd710732fd5ba057fb1105ed5`.
Both muted Instant samples have zero capture failures, mismatches, unsupported
or pending records, settled terminal effects and unchanged installation files.
They have no JSON companions.

Both complete independent policies, mid-battle suffixes and 16 policy branches
pass. Native card-view comparisons cover 350/650 decision contexts and
7,595/16,610 exact queries respectively, with 32 immutable query branches. The
physical archive additionally compares 108 complete room contexts, 19 removals,
12 cross-room moves and one empty ascent. Three affected old fixtures pass
(original unit relics, spawn status relics and cardless relic upgrades), as do
the three focused pure suites. All 215 archive names, sizes and SHA-256 hashes
match the curated manifest; a full 215-archive regression remains deferred.

Original relic coverage is now 25/224 (11.16%), with its exact scope documented
in [relic-coverage.md](relic-coverage.md). Definition-ID filters, Deathwish
installation and unsupported trigger payloads retain explicit guards; the
shared class does not establish coverage of its other 17 catalog owners.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-triggers-pure
dotnet run --project src/ModelChecks -c Release -- --original-trigger-relics-combined tests/fixtures/full-battle-original-trigger-relic-deaths.mt2f tests/fixtures/full-battle-original-trigger-relics.mt2f
& ./scripts/Check-Models.ps1 -Fixture @('full-battle-original-trigger-relics.mt2f', 'full-battle-original-trigger-relic-deaths.mt2f')
```

### Original status stack relics

`full-battle-original-status-relics.mt2f` accepts five unchanged originals:
`ApplyExtraPyregel_DH`, `IncreasePyregelStacks`, `ExtraValorOnValor`,
`IncreaseArmorStacks` and `ExtraSpellWeakness`. The shared model follows native
relic order, exact team equality, positive-input and first-application conditions, signed overflow,
outer-condition notification phases and preview behavior. Starting statuses
and live equipment upgrades now retain their added stacks and condition state.

The muted native Instant battle takes 151.04 seconds, wins at Pyre 80 after 26
actions and five EndTurns, and has 39 matched stages with zero capture failures,
mismatches, unsupported transitions or pending records. Independent replay
matches the complete battle, a mid-battle suffix and 16 parallel policy branches.
There are 545 complete decision contexts and 12,005 exact native card views.
All 269 distinct status queries match: 29 real call cases and 240 read-only
boundary cases. Removing each original loses its exact added stack in real
call cases; the five witness counts are 4, 1, 8, 5 and 2. Query checks pass
32 parallel branches without changing their parents.

Permanent/deferred, nullable-target, overflow and transfer/multiply exclusions
are native value-query evidence. Separate permanent starting-status groups and
permanent-upgrade status metadata remain guarded; this archive does not claim
their complete execution paths. Other status families and owners remain open.

The binary archive has no JSON source: 32,935 bytes, 5,654 unique nodes, SHA-256
`1965dcded188d8ae23b140fc8d1688b97bdaabec2df576911be144cad361aa2a`.
Its raw native source is
`.probe-runs/full-battle-units-spells-and-junk-20261013-044528-5081dd10/full-battle.mt2f`.
The selected fixture checks pass. At that milestone, the 248-archive
inventory's complete replay remained deferred. Original relic coverage is
now 65/224 (29.02%); [relic-coverage.md](relic-coverage.md) lists the exact
original definitions and witnesses.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-status-stacks-pure
dotnet run --project src/ModelChecks -c Release -- --original-status-relics-only tests/fixtures/full-battle-original-status-relics.mt2f
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-status-relics.mt2f
```

### Original status magnitude relics

`full-battle-original-magnitude-relics.mt2f` accepts the unchanged originals
`IncreaseRageDamage`, `StrongerSpikes` and `AwokenRegen`. Native attack stores a
separate `DamageBuffFromStatusEffects` accumulator: a later condition change
does not recalculate older Rage stacks. Starting statuses, live additions,
explicit removals, natural decay, copying and upgrades preserve this history.
Spikes and Regen query their current magnitude at each actual trigger.

The initial Instant native battle took 49.85 seconds, won at Pyre 80 after
18 plays and five EndTurns, and matched 39 combat stages with zero capture
failures, mismatches, unsupported transitions and pending records. The independent
full battle, mid-battle suffix and 16 parallel policy branches passed. All 31
deduplicated native getter cases matched: 20 status/team matrix, seven live and
four preview cases. Getter checks also passed 32 immutable parallel branches.

In that initial capture, removing each original preserved the captured starting
status attack cache and changed only future processing: 21 Rage attack witnesses,
12 player-owned Spikes damage witnesses and 14 player-owned Regen actual-healing
witnesses. Complete fixture checks and focused original-status-relic, post-combat-healing and
unit-clone regressions passed. The final stricter player-owner filter was checked
again with the same production `Model.dll`; its SHA-256 is
`bd5e8766d33f228263723a0f56b0a95126d0107bb0849bb418d7b28e1978623f`.

The initial binary archive had no JSON source: 26,951 bytes, 4,298 unique nodes,
SHA-256
`5bc2dd6bd19127eb4b93347ee88578d189efca16b658df66fb80bb30eea0a36d`.
Raw source:
`.probe-runs/full-battle-units-spells-and-junk-20261011-115413-e2b090f3/full-battle.mt2f`.
At that initial milestone, all 217 archive names, sizes and hashes matched the
curated list and manifest; the full 217-archive replay remained deferred.
Original relic coverage was 32/224 (14.29%). Mageblade remains guarded until
room spell power and its cached coefficient have independent native evidence.

The current schema-114 archive was refreshed after fixing the status-definition
validation dummy's missing attack cache. That bug incorrectly excluded
`JustCause` (FloorRearrange followed by Valor 2) from the generic supported-card
policy. A new native capture now validates the same policy with that card
available; no compatibility rule excludes historical cards. Original Boss,
waves and relic definitions remain unchanged.

The refreshed Instant battle takes 67.79 seconds and wins at Pyre 80 after
24 actions, five EndTurns and 38 matched combat stages. Complete native
action/stage checks, full-battle and mid-battle replay, 16 policy branches and
32 immutable getter branches pass, with all differential failure counters zero.
Getter cases remain 20 matrix, seven live and four preview. Current owner
witness counts are 19 Rage attack, ten player Spikes damage and 12 player Regen
healing, with starting status attack caches retained under each owner removal.

The refreshed archive replaces the same name without adding an inventory entry:
28,039 bytes, 4,637 unique nodes, no JSON source, SHA-256
`45fe3b83212147081d5c352e01f01cb826be5715eaf87c40dd0f7965f52ed80c`.
Current raw source:
`.probe-runs/full-battle-units-spells-and-junk-20261011-124820-792d54a1/full-battle.mt2f`.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-status-magnitudes-pure
dotnet run --project src/ModelChecks -c Release -- --original-magnitude-relics-only tests/fixtures/full-battle-original-magnitude-relics.mt2f
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-magnitude-relics.mt2f
```

### Original lifecycle trigger relics

`full-battle-original-lifecycle-relics.mt2f` validates five unchanged originals:
`IncreaseArmorStacks_DH`, `AvariceOnSummon`, `ShroomsGainRegen`,
`DamageOnGoingDownFloor` and `LifeInsurer`. Two authored Steward carriers
exercise native unit abilities, the original Shroom filter, natural lethal
damage and one-stack Undying revival. The Boss, waves, relic definitions,
payloads and count-exclusion flags remain original.

The Instant native battle takes 104.40 seconds and wins at Pyre 80 after 24
actions (18 ordinary plays and six abilities), five EndTurns and 35 complete
combat stages, with zero capture failures, mismatches, unsupported transitions
or pending records. Complete fixture checks, independent policy replay,
mid-battle continuation and 16 parallel branches pass. Fourteen complete
birth/train/queue records include seven enemy gates, three Shroom births and
four other player subtypes; installation checks pass 32 immutable branches.
One natural revival and 17 callback phases match complete states and the
pending Reanimated-before-Death FIFO. Once-listener checks also preserve
physical lifecycle flags and preview metadata through the existing model.

Removing only each owner's installed callbacks preserves accumulated upgrades,
raw attack and all other caches. Required future-effect witnesses are 24
ability Armor, 24 non-unique +5 attack upgrades, 16 Shroom Regen, one front-unit
Death Armor and one revived-unit Armor witness. Avarice's exact `Plus5Attack`
upgrade is non-unique and lasts `TemporaryUntilEndOfBattle`; the misleading
`DamageOnGoingDownFloor` asset actually adds Armor on Death. Focused existing
revival, equipment-owned-revival and original-trigger-relic regressions pass.

The schema-115 binary archive has no JSON source: 54,584 bytes, 7,977 unique
nodes, SHA-256
`a1bdc38ec74d932ab637d65545b3df4f623b3f8b316a533d3112ac0cc1f2cfef`.
Raw source:
`.probe-runs/full-battle-units-spells-and-junk-20261011-121109-1646fe31/full-battle.mt2f`.
All 218 archive names, sizes and hashes match the curated list and manifest.
The full 218-archive replay remains deferred to a completed milestone.
Original relic coverage is now 37/224 (16.52%); Mageblade and broader effect
and Boss coverage remain open.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-lifecycle-pure
dotnet run --project src/ModelChecks -c Release -- --original-lifecycle-relics-only tests/fixtures/full-battle-original-lifecycle-relics.mt2f
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-lifecycle-relics.mt2f
```

### Original Rage and Frostbite decay relics

`full-battle-original-decay-relics.mt2f` validates the unchanged originals
`RagePreservedOnFriendlies` and `IncreaseFrostbiteStacks`. The former prevents
Rage (`buff`) decay only for Monsters, the native player team. The latter adds
2 Frostbite (`poison`) to positive incoming applications on either team and
prevents its natural decay. Original Boss, waves and relic definitions are
retained. Two authored high-health Steward carriers and a native three-effect
spell apply buff2, poison1 and armor30 as separate singleton effects. The real
card produces poison3 on both teams; armor allows enemies to survive long
enough to observe natural status processing.

The schema-116 Instant battle takes 62.55 seconds and wins at Pyre 77 after
21 actions, five EndTurns and 42 complete combat stages. Capture failures,
mismatches, unsupported transitions and pending records are zero. Complete
native action/stage checks, independent full-battle and mid-battle replay and
16 parallel policy branches pass. All 37 decay getter cases match: 16 matrix,
11 live and ten preview, alongside 239 stack queries and 32 immutable getter
branches. Native card views also match: 10,665 views across 460 decision contexts.

Actual player Rage preservation supplies 14 owner-removal witnesses, with
enemy Rage decay as a negative team case. Frostbite supplies 25 combined
preservation and real-application witnesses, covering both teams. That count
includes card poison3 → poison1 when its owner is removed; it is not a count
of preservation alone. Complete states and prior attack accumulators remain
unchanged at each counterfactual's starting boundary. Pure checks also cover
multi-effect status-card validation, cache retention, natural and trigger
consumption, explicit removal, deployment/relentless and metadata guards.
The affected original-status (39 stages) and post-combat-healing (43 stages)
fixtures and the refreshed magnitude fixture pass complete checks.

The binary archive has no JSON source: 30,547 bytes, 5,228 unique nodes, SHA-256
`34cd0132a74417bb5749f75b9ba4a3d30d18e7cae2b15c2421ca3b71d70d7712`.
Raw source:
`.probe-runs/full-battle-units-spells-and-junk-20261011-124340-9b1f1384/full-battle.mt2f`.
The accepted native and final frozen `Model.dll` SHA-256 is
`2a4aa15ce74af48c4105ca16b762837470ac4bcf59112ce89079bcc599e17f42`.
At the decay milestone, inventory was 219 archives: 188 battles and 31 component
calibrations. Original relic coverage was 39/224 (17.41%) across 12 evidence
archives; the status-applied
effect covers five of eight original owners and decay prevention covers both
original owners. All 219 archive names, sizes and SHA-256 hashes match the
curated list and manifest; no JSON companions are present. The full 219-archive
replay remains deferred to a completed milestone. Arbitrary card/relic
combinations, all Bosses and the optimal solver remain unfinished.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-status-decay-pure
dotnet run --project src/ModelChecks -c Release -- --original-decay-relics-only tests/fixtures/full-battle-original-decay-relics.mt2f
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-decay-relics.mt2f
```

### Original energy, draw and Pyre relics

`full-battle-original-resource-relics.mt2f` validates eight unchanged originals:
`SilverIncreasedDraw`, `SilverIncreasedEmber`, `Inkbound_LeakyPen`,
`LessDeployEmberMoreOtherEmber`, `MoreDeploymentPhaseEmber`, `BossEmber`,
`BossEmber2` and `BossEmberDH`. All 20 original effects have positive actual
effect evidence across ordinary/deployment energy, starting-hand rules, Pyre
health and Pyre attack. Native acquisition happens before the first deployment
callbacks. Authored cards are free empty spells, with no manual energy setters,
hand draws or player unit deployment. Original Balance, selected Pyre, Boss
and wave definitions remain unchanged.

The schema-117 Instant battle takes 88.40 seconds and wins at Pyre 123 after
42 actions, seven EndTurns and 50 matched combat stages. Capture failures,
mismatches, unsupported transitions and pending records are zero. Fifty-six
resource operations compare eight real acquisitions and 48 signed native energy
callback bodies. Fresh Balance values are ordinary energy 3, deployment extra
1, base draw 5, maximum energy 99 and maximum hand size 10. Deployment starts
at 4, becomes 8 after deployment callbacks, then 12 after ordinary callbacks;
later ordinary turns produce 7. This battle does not reach the energy cap.
Six actual empty-hand draws produce eight cards; removing the +1/+2 draw
owners changes the real results to seven/six cards, below the hand cap.

The selected Pyre has raw attack 45 and raw maximum HP 80. Its native upgrade
tables are attack `[30,55,20]` and health `[20,20,15]`; the three Boss owners use
indices 0/1/1. Their initialized attack contributions are 30/55/55, producing
Pyre attack 185, and their normal acquisitions each add 20 HP, producing maximum
HP 140. Original catalog ParamInt10/30 are not used as these actual bonuses.
Six positive native Pyre combat stages and complete acquisition/birth/combat
comparisons validate the consumers. Owner witness totals are 6/7/12/7/1/14/14/14
in the owner order above, requiring every effect to contribute rather than
counting only resource metadata.

Native, pure, strict and complete-fixture checks pass. Independent full-battle
and mid-battle chooser replay each pass 16 parallel branches; immutable getters
pass 32 branches. These checks reproduce the fixed policy, not optimal search.
Card checks match 32,515 native views across 845 decision contexts and retain
32 immutable query branches. Focused EnergyEffects (49 stages), TypedDraw
(35 stages) and NoCards (52 stages) regressions also pass. NoCards independently
finishes seven EndTurns at Pyre 49 with 16 branches. Pure checks cover signed
callback order and caps, ledger rollover, draw-rule rounding and separate
clamps, notifications, Pyre initialization/acquisition caches and metadata guards.

The binary archive has no text or JSON source: 39,703 bytes, 8,249 unique nodes,
SHA-256
`92c7f41e6c7c3f1fb1c6ccb3639e735629d3df9fd8808b8f4a31dc805334c419`.
Raw source:
`.probe-runs/full-battle-units-spells-and-junk-20261011-131145-588ea4aa/full-battle.mt2f`.
The frozen production `Model.dll` matches the native sandbox plugin; SHA-256 is
`7d441722cf4298383114edbfdb55f6ead1b6f580645ca11341c7a7f448cc9adf`.
Acceptance uses `.probe-runs/resource-acceptance2/ModelChecks.dll` with
`resource-pure.log`, `resource-strict2.log`, `resource-full2.log` and
`resource-native1.log` under `.probe-runs/`.
Game `2.2.1` and module MVID `8fb07b96-f4db-4d2b-884d-c00536d6ccf4` are fixed
by the strict checker.

At the resource milestone, inventory was 220 archives: 189 battles and 31
component calibrations. Original relic coverage was 47/224 (20.98%) across 13
evidence archives. Full-inventory replay remained deferred; that batch used
focused regression. All 220 archive names, byte lengths and SHA-256 hashes
matched the manifest and curated list, with no JSON companions. The next
schema-118 batch independently validates live gold/cache refresh, while nonzero
`SaveManager.GetModifiedPyreBaseAttack()` consumers remain guarded.
This evidence does not establish all resource relics, arbitrary
card/relic combinations, all Bosses or an optimal solver.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-resources-pure
dotnet run --project src/ModelChecks -c Release -- --original-resource-relics-only tests/fixtures/full-battle-original-resource-relics.mt2f
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-resource-relics.mt2f
```

### Pyre attack refresh and original Boss draw relics

`full-battle-pyre-resource-refresh.mt2f` validates unchanged originals
`BossIncreaseTurnDraw`, `BossIncreaseTurnDrawDH` and `BossIncreaseTurnDraw2`.
Each original keeps its ordered draw +1, Pyre health and Pyre attack effects,
with no outer conditions or required mutator. The selected Pyre's initialized
tables supply HP 20/20/20 and attack 30/55/55 at indices 0/1/1 in this recording.
Three real pre-battle acquisitions each add 20 current/max HP, six real draw8
applications fall to seven cards under individual owner removal, and six
positive Pyre combat stages change actual attack results under owner removal.
All nine original effects require positive evidence, with 13 witnesses per
owner. No previously accepted draw relic is installed to obscure these +1
contributions at the ten-card cap.

One real compound Pyre-room spell applies unique temporary attack upgrades
+7/-10, Buff11, Rage2, Gold0/+5, a further -3 upgrade and Gold-5. The +7 upgrade
is the native state marker for an independent one-play chooser. Gold0 retains
raw attack 182 and sends no refresh. Gold+5 changes 50 to 55 and Gold-5 restores
50; both synchronously refresh raw attack 182 to 185, preserving the actual
upgrade accumulator 0, the upgrades already applied, Buff11 and Rage's attack cache 4.
Each nested refresh sends three original attack-effect notifications. Its
private display cache is 185 at the handler boundary, then SetGold's subsequent
display signal records the live buffed attack 200. Gold0 preserves its earlier
display cache 193. The checker derives the expected values from captured raw
save/Balance/definition inputs, rather than assuming a fixed gold balance or
Pyre table.

The schema-118 Instant battle takes 61 seconds and wins at Pyre 123 after one
card action, seven EndTurns and 50 matched combat stages. Capture failures,
mismatches, unsupported transitions and pending records are zero. Five native
gold/refresh records cover three outer gold bodies and two nested refreshes.
Complete intermediate trains, actions and combat match the final independent
model. Whole-battle and mid-battle chooser replays each pass 16 parallel
branches; 32 operation branches preserve source states, caches and RNG.
Pure gold/Pyre and engine-room spell checks pass. Complete focused regression
passes the new archive and five directly affected old fixtures; the full
221-archive replay remains deferred to a milestone.

The binary has no JSON source: 23,282 bytes, 3,603 unique nodes, SHA-256
`d6ed8926500ba39d49b5e28c23540d2df73f47173d535ea19a39e3ea82d4a5c2`.
Raw source:
`.probe-runs/full-battle-units-spells-and-junk-20261011-135122-a2a11a08/full-battle.mt2f`.
Native sampling uses `Model.dll` SHA-256
`8ed16973ba4818f88c2d002ec1853146f356a582bdc960b48a4ed5cf5acda2c6`.
The final frozen model, after a Room-only API correction, has SHA-256
`b5c93f54799791d81d60f8f829fc7138926002f9c314ea43f3a59306345c1c91`;
its `ModelChecks.dll` SHA-256 is
`04d88771f050f49d749876d5178728cfb616cd1d7d27943609cdbcff985f8227`.
These are distinct model hashes. Acceptance replays the native archive with
the final checker in `.probe-runs/pyre-refresh-acceptance3/`; pure, strict and
complete focused logs are `pyre-refresh-pure3.log`, `pyre-refresh-strict3.log`
and `pyre-refresh-full3.log`, all retaining successful terminal exit status.

At the Pyre-refresh milestone, inventory stood at 221 archives: 190 battles
and 31 calibrations. Original relic coverage at that milestone was 50/224
(22.32%) across 14 evidence archives. All 221 archive names,
byte lengths and SHA-256 hashes match the manifest and curated list, with no
JSON companions. Nonzero saved modified
Pyre base attack and unsupported tower room modifiers remain guarded. Gold
range quantities still require captured Setup-time amount metadata. Broader
card/relic combinations, all Bosses and the optimal solver remain unfinished.

```powershell
dotnet run --project src/ModelChecks -c Release -- --gold-pyre-pure
dotnet run --project src/ModelChecks -c Release -- --pyre-resource-refresh-only tests/fixtures/full-battle-pyre-resource-refresh.mt2f
& ./scripts/Check-Models.ps1 -Fixture full-battle-pyre-resource-refresh.mt2f
```

### Original floor capacity relics

`full-battle-original-floor-capacity-relics.mt2f` validates unchanged original
`PurgeChampionReward`, `BossCapacity`, `BossCapacity2`, `BossCapacityDH` and
`SilverIncreasedCapacity`. All 14 original effects have positive native
witnesses: five +2 floor-capacity contributions, PurgeChampionReward's normal
and deployment energy and draw, and the three Boss relics' indexed Pyre health
and attack. The recorded room reaches capacity 15 with native size 6/6/2/1
units; removing any one capacity relic yields 13 in a newly initialized room.
The 6+6 deployment fits, while the next size-2 unit is rejected; an existing
room retains its original maximum after relic removal. Native mutation order,
registration, RNG state, resource queries and room identities are preserved by
the independent checks. This encounter has no Pyre attack exchange, so the
Boss attack contributions are verified through native acquisition and Pyre
birth stats.

The schema-119 Instant battle wins in 76 seconds with 26 card actions, five
EndTurns and 51 matched combat stages. It has zero capture failures,
mismatches, unsupported transitions or pending records. Whole-battle and
mid-battle policy replays each pass 16 immutable parallel branches. The focused
capacity regression also passes the previous room-capacity, lethal-capacity,
physical-spawn, physical-spawn-without-cards, typed-draw and Pyre-resource
fixtures. The full 223-archive replay remains deferred to a milestone.

The binary has no JSON source: 32,376 bytes, 6,658 unique nodes, SHA-256
`7286230896e2253fe22db88f74dc5d61d51b36e6f9609b81ace026194022e09b`. Native
sampling used the final model and strict acceptance replays the archived state.
The inventory is 223 archives (192 battles, 31 calibrations); accepted original
relic coverage is 55/224 (24.55%) across 15 evidence archives. Broader
card/relic combinations, unsupported tower room modifiers, all Bosses and the
optimal solver remain unfinished. Mageblade now has pure model checks for its
cached per-unit coefficient and captured room-power attack bonus. The new
`full-battle-mageblade.mt2f` archive records 39 status/cache/room-power
observations and passes the complete native battle, action replay, mid-battle
continuation and 16 parallel branches. Its +15 room magic power is a controlled
probe input, so it validates Mageblade's cache and attack consumer rather than
the natural source of room power. Mageblade is not an original relic and does
not change the 55/224 relic-owner count.

### Mixed cardless relic effect order (schema 121)

`full-battle-spawn-status-relics-mixed-cardless-order.mt2f` places the original
`SpawnWithArmor` spawn-status payload before `PyreHeartSon`'s cardless temporary
upgrade on one captured relic. The full-battle probe matches the native relic
birth states and callback queues, observes Armor followed by the upgrade on five
non-clone cardless births, and verifies that five cardless clones skip the
upgrade. The battle has 42 combat stages, nine card cycles, seven births, five
EndTurns, and finishes at Pyre 80 with zero capture failures, mismatches,
unsupported transitions, or pending records. The independent strict replay
passed all 42 stages and 16 immutable branches; a full 224-archive replay
remains deferred.

This combines two existing effect payloads for an ordering test. It does not add
an original relic owner to the 55/224 evidence count or establish every
temporary-upgrade lifecycle and interaction.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-floor-capacity-pure
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-floor-capacity-relics.mt2f
```

### Temporary relic Consume routing

`full-battle-relic-card-exhaust-upgrades.mt2f` validates a temporary
`CardTraitExhaustState` supplied through the native `RelicEffectAddTempUpgrade`
manager path. The probe substitutes this trait into the `ReduceStarterCost`
upgrade payload and targets eligible spells, so this is mechanism coverage, not
evidence for another unchanged original relic owner. Two upgraded cards are
actually played and reach Exhausted with their private trait state, double
exhaustion statistic, and standby closure all matching native capture.

The schema-122 muted Instant battle wins after 51 matched stages, 28 card
actions and six EndTurns. Native capture failures, mismatches, unsupported
transitions and pending records are all zero. Independent replay passes the
whole battle, mid-battle continuation and 16 parallel branches. The archive is
29,854 bytes with 5,068 unique nodes and SHA-256
`1a620b7384b8d700f48d4c074c88f9a39721b2f17d86156a59963081a3e805b0`; it has
no JSON companion. Original relic coverage remains 55/224 (24.55%). Accepted
inventory is now 225 archives: 194 battles and 31 component calibrations. A
full 225-archive replay remains deferred to a milestone.

```powershell
& ./scripts/Check-Models.ps1 -Fixture full-battle-relic-card-exhaust-upgrades.mt2f
```

### Permanent hand-upgrade Consume installation

`full-battle-permanent-consume-hand-upgrades.mt2f` records native
`CardEffectAddPermanentCardUpgradeToCardsInHand` application of
`CardTraitExhaustState`. It verifies that adding the same permanent upgrade
does not duplicate an already-visible base Consume, but installs a new trait
when SelfPurge hides the existing one. A played card with the permanent upgrade
then reaches Exhausted. This verifies the mechanism, not another unchanged
original relic owner.

The schema-123 native battle wins at Pyre 80 after 26 card plays, six EndTurns
and 50 recorded combat stages. Corrected-model replay passes all 26 policy
actions, the permanent-Consume exhaustion assertion, and 570 card-mask decision
contexts with 9,255 exact native card-view queries, 3,345 upgraded-card views
and 32 immutable branches. Pure Consume checks pass as well. The 29,840-byte
binary has 4,894 unique nodes and SHA-256
`b0cdbe1fddef6c959facbd58fdfd4e35bc1d9b758a133883e4d6173b20d5b1c2`; there is
no JSON companion. Inventory is 226 archives: 195 battles and 31 calibrations.
Original relic coverage remains 55/224 (24.55%). Full-inventory replay remains
deferred to a completed milestone.

```powershell
& ./scripts/Check-Models.ps1 -Fixture full-battle-permanent-consume-hand-upgrades.mt2f
```

### Permanent Consume over temporary Consume

`full-battle-permanent-consume-over-temporary.mt2f` records a hand receiving
temporary `CardTraitExhaustState` and then permanent `CardTraitExhaustState`.
The native state keeps the active Consume in `ExhaustState.UpgradeTrait` and
leaves `MaskDescriptor.BaseTraits` empty. The permanent upgrade continues to
provide Consume after the temporary source is removed by the model. Five played
cards reach Exhausted.

The schema-125 muted Instant battle wins at Pyre 80 after 26 card plays, six
EndTurns and 50 combat stages. Strict capture has zero failures, mismatches,
unsupported transitions or pending records. Offline replay matches all 26
actions, eight hand-upgrade spell plays, five Consume exhaustions and 570
card-mask decision contexts. The 29,737-byte archive has 4,863 unique nodes and
SHA-256 `60a748a680f39e0f7185c5c21499dca9741c13b08bb761a668b1312e57686e93`;
it has no JSON companion. Inventory is 228 archives: 197 battles and 31
component calibrations. Original relic evidence remains 55/224 (24.55%).
Full-inventory replay remains deferred to a milestone.

```powershell
& ./scripts/Check-Models.ps1 -Fixture @('full-battle-permanent-consume-hand-upgrades.mt2f','full-battle-permanent-consume-over-temporary.mt2f','full-battle-generation-permanent-consume.mt2f')
```

### Generation copies retain permanent Consume

`full-battle-generation-permanent-consume.mt2f` records the native
`CardEffectAddBattleCard` copy path for permanent Consume upgrades. All five pile
destinations match, including spell and unit-trigger generation. The checker
matches 91 generation effects and 112 created cards; 60 copied cards retain the
base Consume trait with fresh, unexhausted private state. Three unit-trigger
paths, 39 modifier-copy cases, 12 refusals, 12 empty pools, one pending-upgrade
consumption and 38 fresh effect states are covered.

The schema-124 muted Instant battle wins at Pyre 80 after 21 card plays, six
EndTurns and 49 room stages. Capture failures, mismatches, unsupported
transitions and pending records are all zero. The complete and mid-battle
policies pass 16 parallel branches. The archive is 60,191 bytes with 12,240
unique nodes, and has no JSON companion. Original relic evidence remains
55/224 (24.55%); inventory is 227 archives: 196 battles and 31 calibrations.
Full-inventory replay remains deferred to a completed milestone.

```powershell
pwsh -NoProfile -Command "& './scripts/Check-Models.ps1' -Fixture @('full-battle-generation-permanent-consume.mt2f','full-battle-generation.mt2f','full-battle-permanent-consume-hand-upgrades.mt2f')"
```

### Original Sting relic owners

`full-battle-original-sting-relics.mt2f` acquires the unchanged
`StingBuffPower` and `StingBuffPiercing` relics, adds a native Sting, and plays
it three times. The complete battle has 48 matched combat stages, 19 actions,
six EndTurns and wins at Pyre 79. All 53 relic manager calls match the exact
upgrade, filter, notification and reset states; the plays retain +30 damage and
IgnoreArmor. The archive also passes mid-battle replay and 16 immutable policy
branches. Its 30,258 bytes contain 4,886 unique nodes, SHA-256
`dbe26dd88f9410ecd2a2b75f441be4ba206beff9c565aea2470de36775aece0a`, and no
JSON source. The two relic owners bring native full-battle evidence to 57/224
(25.45%); inventory is 229 archives: 198 battles and 31 component calibrations.
Full-inventory replay remains deferred to a completed milestone.

```powershell
pwsh -NoProfile -Command "& './scripts/Check-Models.ps1' -Fixture @('full-battle-original-sting-relics.mt2f','full-battle-relic-card-piercing-upgrades.mt2f','full-battle-original-relic-batch.mt2f')"
```

### Original deployment-phase relic

`full-battle-original-deployable-relic.mt2f` acquires the unchanged
`NonBannerUnitsHaveDeployable` relic before the native first-battle draw. Its
original `RelicEffectAddTempUpgrade` adds `CardTraitDrawInDeploymentPhase` to
eligible non-Banner units. Native setup places two such units in the first
player-decision hand, and the independent policy plays both into rooms. The
native manager's non-unique repeated-upgrade behavior is retained.

The muted Instant battle wins at Pyre 73 after 23 card plays, seven EndTurns
and 62 matched room states. All 48 relic-manager calls, 48 ordered filter
results and six notifications match; the model also passes 560 decision
contexts, 9,375 exact card-mask queries, 1,120 upgraded-card observations and
32 immutable query branches. Sixteen policy branches pass the complete replay.
The schema-125 archive is 34,673 bytes with 5,346 unique nodes and SHA-256
`8496a6ac544baba57535f5b0fafd4a855004ef854c19da1c9e314a5698188178`; it has
no JSON companion. Inventory is now 230 archives: 199 battles and 31 component
calibrations. Original relic evidence is 58/224 (25.89%). Full-inventory replay
remains deferred to a milestone.

```powershell
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-deployable-relic.mt2f
```

### Spawn-or-status skill effect

`full-battle-spawn-or-status.mt2f` exercises both branches of native
`CardEffectSpawnMonsterOrAddStatusEffect`: the first skill activation creates a
cardless Funguy with Armor, and the next activation applies statuses to the
existing unit. Both activations and all 17 card actions match native state; the
battle wins after 44 stages with zero capture failures, mismatches, unsupported
transitions or pending records. The muted Instant capture took 59.60 seconds.
The 35,554-byte archive has 5,850 unique nodes, SHA-256
`efe52d33f38dbd9b15059f826cd5620948205db866e0d5ae5a0b2beb320d5975`, and no
JSON companion. Inventory is 231 archives: 200 battles and 31 component
calibrations. Original relic evidence remains 58/224 (25.89%).

```powershell
& ./scripts/Check-Models.ps1 -Fixture @('full-battle-spawn-or-status.mt2f','full-battle-ability-activation.mt2f','full-battle-spawn-status-relics.mt2f')
```

### Relic modifier definitions in ordinary capture

`full-battle-spawn-status-relics-clones-ungated-capture.mt2f` records the
cardless `PyreHeartSon` upgrade with `RelicCardModifiersEnabled=false`. Ordinary
relic capture still includes its upgrade definition, applies it to five regular
cardless births and skips five cardless clones. The muted Instant native battle
wins at Pyre 80 with five EndTurns, 13 clone operations and zero capture
failures, mismatches, unsupported or pending records; the original save files
remain unchanged. Independent replay matches the complete chain and 16 parallel
policy branches. The schema-111 archive is 56,902 bytes, has 7,868 unique nodes
and SHA-256
`11240d0691ac6745affcd86f771cb203d51902e9164dc62bc090eaf9a80d0a97`; it has no
JSON companion. The result does not increase original relic-owner coverage.

```powershell
& ./scripts/Check-Models.ps1 -Fixture full-battle-spawn-status-relics-clones-ungated-capture.mt2f
```

### Endless unit death return

`full-battle-original-endless-return.mt2f` verifies the unchanged
`MoreRares_Relic` / `AddEndlessToNonMorsels` effect on a non-Morsel carrier.
After one actual death, native state places the spawner in Draw regardless of
free hand space, retains its temporary Endless upgrade, and leaves
`TimesExhausted` unchanged. The Instant battle wins after 23 card plays and
seven EndTurns; all 62 room stages, 23 actions and 16 parallel policy branches
match. The 34,872-byte archive has 5,643 unique nodes and SHA-256
`8c26f779b64ce8c4bc4f91bb83691d94f2ebabdc274995107478a50049822bae`.

```powershell
& ./scripts/Check-Models.ps1 -Fixture full-battle-original-endless-return.mt2f
```

### Buff equipment spell

`full-battle-equipment-buff.mt2f` records the `BuffEquipment` spell on a host
with three attached equipment cards. The muted Instant battle wins at Pyre 80
after 16 policy plays and five EndTurns. All 40 room stages, nine card cycles,
nine train phases, seven spawns, 16 actions, five EndTurns and five equipment
operations match native state, with zero mismatches, unsupported transitions,
capture failures or pending records. Sampling took 41.76 seconds. The schema-103
archive is 23,820 bytes with 3,860 unique nodes and SHA-256
`9f561a27aa020aa7e8f33285d659668c047d406da37ed92f230313098ec8963d`; it has no
JSON companion. The inventory is 236 archives: 205 battles and 31 component
calibrations. Accepted original relic evidence remains 59/224 (26.34%). Full
inventory replay remains deferred.

```powershell
dotnet run --project src/ModelChecks -c Release -- --equipment-native-only tests/fixtures/full-battle-equipment-buff.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-equipment-buff.mt2f
```

### Persistent enchantment Boss preview restoration

Full-battle preview restoration now clears pending `PreviewTargets.NextAction`
for Boss identities, including Bosses referenced by preview-born retained aura
sources. This matches all eight existing persistent-enchantment EndTurn
snapshots. The two random-pool summon archives exposed one later preview edge:
model death cleanup recomputed all auras after terminal Boss removal, beyond the
native preview stop boundary. `RoomCombatModel.Death` now skips that final global
update while preserving the live battle-won path. Both archives pass all 41 room
stages, nine card cycles, nine train phases, seven spawns, five EndTurns, 15
actions and 16 parallel policy branches, with zero unsupported operations. The
six other persistent-enchantment archives also retain their targeted pass.
Three adjacent terminal-death fixtures pass 46 more native action checks with no
unsupported operations. Inventory remains 236 archives and accepted original
relic evidence remains 59/224 (26.34%); full-inventory replay is still deferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-persistent-enchantment-random-summons.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-persistent-enchantment-random-summons-fresh.mt2f
```

### Covenant Boss character filters

`full-battle-covenant-character-filter.mt2f` activates the complete native
`Covenant04_StrongerBosses` definition in an isolated run. Its 11 wave-spawn
decisions and eight Covenant birth callbacks match exactly. The character filter
installs the `OnKill` Rage trigger on the single `Boss_Level1_Basic` match; the
seven observed nonmatching births retain their original statuses and triggers.
The 2026-10-12 muted Instant native battle at
`.probe-runs/full-battle-steward-once-20261012-163709-f52d5f60` wins after 55
room stages, 13 card cycles, 14 train phases, 11 spawns, seven EndTurns and one
card play. Capture failures, mismatches, unsupported transitions and pending
records are zero. Offline replay matches all 55 rooms, 13 card cycles, 14 train
phases, 11 spawns and seven turns; the policy chain also matches seven EndTurns
and final Pyre 49 across 16 parallel branches. This fixture does not include
`Boss_Level2_BigStats`, whose Covenant effect adds Titan Skin; that status still
stops combat modeling explicitly because damage soak and stack consumption are
not implemented. The schema-103 archive is 37,647 bytes with 4,648 unique nodes
and SHA-256
`ced33a85db5f5525883c0d389c60adebc0f43620cf6eaf5cc9d8d89870df87a8`; it has no
JSON companion. Inventory is 240 archives: 209 battles and 31 component
calibrations. Original relic evidence is 60/224 (26.79%).

```powershell
dotnet run --project src/ModelChecks -c Release -- --covenant-character-filter-only tests/fixtures/full-battle-covenant-character-filter.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-covenant-character-filter.mt2f
```

### Titan Skin damage soak

The room model applies passive Titan Skin soak as
`(ParamInt + relic magnitude) * min(incoming damage, stacks)` on each positive,
non-piercing hit. Damage Shield runs before the soak and Armor after it; unit
Piercing, card `CardTraitIgnoreArmor`, and the global
`RelicEffectAllDamageIsPiercing` bypass it. The focused pure checks cover soak,
cap, ordering, piercing sources, and relic magnitude.

`full-battle-titan-skin.mt2f` adds two Titan Skin stacks to the in-memory
`Boss_Level1_Basic` definition in an isolated probe. The 2026-10-12 muted
Instant native battle at
`.probe-runs/full-battle-steward-once-20261012-164254-f34755f5` wins at Pyre 49
with 55 room stages, 13 card cycles, 14 train phases, 11 spawns, seven EndTurns
and one card play. Independent replay matches all 55 rooms and seven turns; the
Boss carries Titan Skin in 11 stages, takes damage in four and retains the
stacks. Capture failures, mismatches, unsupported stages and pending records are
zero. The schema-103 archive is 20,714 bytes with 3,059 unique nodes and SHA-256
`94941bcd2435b3d7b79b3b47982281b5077a866ec4a9a7039c0906d51660bc62`; it has no
JSON companion.

This validates the native damage handler, not the natural
`Boss_Level2_BigStats` spawn path. No original relic owner was added. Inventory
is 240 archives: 209 battles and 31 component calibrations. Original relic
evidence is 60/224 (26.79%).

```powershell
dotnet run --project src/ModelChecks -c Release -- --titan-skin-pure
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -TitanSkinCombat
dotnet run --project src/ModelChecks -c Release -- --room-combat-only tests/fixtures/full-battle-titan-skin.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-covenant-character-filter.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-titan-skin.mt2f
```

### AllUnitsMultistrike battle mutator

`AllUnitsMultistrike` is captured from the unchanged native mutator definition.
Its unique `Multistrike1` upgrade reaches an eligible owned card, which is then
played and damages enemies in three combat exchanges; all seven observed enemy
births also receive Multistrike. Four native relic-manager calls match the
ordered model, including the original effect filters and notifications.

The full battle wins after 55 stages with 13 card cycles, 14 train phases, 11
spawns, seven EndTurns and 21 card plays. Capture failures, mismatches,
unsupported transitions and pending records are zero. The schema-103 archive is
31,317 bytes with 4,937 unique nodes and SHA-256
`b41dfa4dbed15f85676b29fe52eb04560daab6437fcf8e1e9f91048125406f54`; it has no
JSON companion. This is mutator evidence, not a collectible relic owner, so
original relic coverage remains 59/224 (26.34%). Inventory is 240 archives:
209 battles and 31 component calibrations.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -AllUnitsMultistrikeMutator
dotnet run --project src/ModelChecks -c Release -- --relic-card-modifiers-only tests/fixtures/full-battle-all-units-multistrike-mutator.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-all-units-multistrike-mutator.mt2f
```

### Refreshed units-and-junk full battle (schema 103)

The 2026-10-12 native `full-battle-units-and-junk.mt2f` capture wins after 61
room stages, 13 card cycles, 14 train phases, 11 spawns, seven EndTurns and 11
card plays. Capture failures, mismatches, unsupported transitions and pending
records are zero. Offline checks match all 61 rooms, 13 card cycles, 14 train
phases, 11 spawns, seven turns and 11 actions; the policy chain also matches all
card plays, EndTurns and final Pyre 60. The archive is 26,708 bytes with 4,301
unique nodes and SHA-256
`6d2f8fa146f94746ad54d999cd8a33c37be48f98e47b8a685828f7c33273b185`; it has
no JSON companion. This validates the captured seven-turn path, not exhaustive
card/relic coverage or completion of the full decision search.

### Refreshed units, spells and junk full battle (schema 103)

The 2026-10-12 native capture at
`.probe-runs/full-battle-units-spells-and-junk-20261012-161054-b4a12ee9`
wins after 60 room stages, 13 card cycles, 14 train phases, 11 spawns, seven
EndTurns and 21 card plays. Capture failures, mismatches, unsupported
transitions and pending records are zero. Offline checks match all 60 rooms,
13 card cycles, 14 train phases, 11 spawns, seven turns and 21 actions; the
policy chain matches all card plays, EndTurns and final Pyre 73. The archive is
30,256 bytes with 5,002 unique nodes and SHA-256
`c775daf43ea3c1c93e80dd2f221e20d6d8df829fbc33faa49276db6d6be2292a`; it has
no JSON companion. This validates the captured seven-turn path, not exhaustive
card/relic coverage or completion of the full decision search.

### Native Mageblade magic-power source

The refreshed schema-127 `full-battle-mageblade.mt2f` gives the isolated battle
a synthetic relic definition with `RelicEffectModifyCardMagicalPower`. Native
game logic supplies 15 room magic power and applies trait-scaled temporary
damage/heal offsets to an eligible spell. This replaces the earlier controlled
room-power input. Mageblade's cached attack coefficient matches in 33
observations; all six native relic-manager calls, 12 ordered effect results,
10 notifications and 32 immutable branches match as well.

The muted Instant battle wins at Pyre 80 after 37 room stages, nine card cycles,
nine train phases, seven spawns, five EndTurns and six actions. Capture failures,
mismatches, unsupported transitions and pending records are zero. The binary is
20,617 bytes with 2,952 unique nodes and SHA-256
`e42cd42f722d2e2e6ccda5808cd8762e9a468210881368e1cf45015916c72aba`; it has no
JSON companion. The synthetic owner does not add to the 59/224 original relic
evidence or 239-archive inventory. Conditional magic-power effects and preview
copy lifecycle remain open.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-magic-power-pure
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Mageblade
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-mageblade.mt2f
```

### Native original card-play magic-power relic

The schema-128 `full-battle-original-card-play-magic-power.mt2f` captures the
unchanged original `MagicPowerWhenCardConsumed` relic (ID
`a4c87eb7-1c4e-4f63-af08-79f910e5a2e8`, power 1, no conditions, required
`CardTraitExhaustState`). The isolated test adds that trait to its TrainSteward
carrier so the native relic's original filter can run; the relic definition is
not changed. Independent replay matches two eligible carrier plays and all 110
spell offset updates, while also checking the rejected nonmatching play. The
native battle wins after 61 stages, 13 card cycles, 14 train phases, 11 spawns,
seven EndTurns and 11 card actions. Capture failures, mismatches, unsupported
transitions and pending records are zero.

The binary is 28,250 bytes with 4,465 unique nodes and SHA-256
`5f60fa073c94fbcc9a7648fe02897624ec96a3db54803e9e40d49e31137e91de`; it has no
JSON companion. Original relic evidence is now 60/224 (26.79%), and the
inventory has 240 archives: 209 battles and 31 component calibrations. Full
inventory replay, other card-play magic-power owners, broader cross-effect
coverage and the decision solver remain unfinished.

```powershell
dotnet run --project src/ModelChecks -c Release -- --relic-card-play-magic-power-pure
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -OriginalCardPlayMagicPower
dotnet run --project src/ModelChecks -c Release -- --original-card-play-magic-power-only tests/fixtures/full-battle-original-card-play-magic-power.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-card-play-magic-power.mt2f
```

### Selected-room and current-cost statistic queries

The statistic query calibration stores 18,000 observations across six native
batches and 42 query kinds. Its 288 `CapacityOfTargetRoom` samples cover four
`Team.Type` cases and verify that only the exact `Heroes` flag selects enemy
capacity; `None`, `Monsters` and the combined flag select player capacity.
Selected-room state follows explicit selection and card play, as well as native
room combat, post-combat triggers, enemy movement, wave spawning and treasure
placement. The current-cost query matches 108 samples when ordered permanent
and temporary card modifiers explain cost. Trait, hand, room and other
effect-driven cost changes remain explicitly unsupported by this query model.

The updated binary is 19,383 bytes with 4,091 unique nodes and SHA-256
`677373e3f4099da3fe60e499578e52e8eef6e9e7c069554a838c8a1aa58bdb74`; it has
no tracked JSON dependency. The full inventory remains 240 archives: 209 battles
and 31 component calibrations. The muted Instant full-battle sample matched all
55 room stages, 14 train phases, 11 spawns, seven EndTurns and one card action
with zero capture failures, mismatches, unsupported operations or pending work.

```powershell
dotnet run --project src/ModelChecks -c Release -- tests/fixtures/statistic-query-calibration.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture statistic-query-calibration.mt2f
```

### Native unit ability teleport

`full-battle-unit-ability-teleport.mt2f` records a native
`CardEffectUnitAbilityTeleport` ability. Independent policy replay matches two
same-room moves and one cross-room move to the chosen spawn point. The muted
Instant battle wins after 55 room stages, 11 card cycles, 11 train phases, nine
spawns, six EndTurns and 20 card actions; all 20 actions match, with zero
capture failures, mismatches, unsupported transitions or pending records.
The schema-103 archive is 40,329 bytes with 6,786 unique nodes and SHA-256
`88625d8afbc2e730af31b70406e63468a10c86527baf8191c6d9fb905be7244d`; it has no
JSON companion. Original relic evidence stays at 60/224 (26.79%). Inventory is
242 archives: 211 battles and 31 component calibrations.

```powershell
dotnet run --project src/ModelChecks -c Release -- --unit-teleport-native-only tests/fixtures/full-battle-unit-ability-teleport.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-unit-ability-teleport.mt2f
```

### DamagePerHp last-attacker spell

`full-battle-damage-per-hp-last-attacker.mt2f` records the native behavior of an
ordinary spell with `DamagePerHpLastAttacker`: the selected friendly unit has a
captured `LastAttackerId`, but the spell has no `selfTarget`, so the two effect
plays do not damage any unit. The native battle won with 13 actions, seven
EndTurns and 60 room stages, with zero capture failures, mismatches, unsupported
transitions or pending records. Independent replay matches all 13 actions and
60 room stages. The schema-103 binary is 27,479 bytes with 4,402 unique nodes
and SHA-256
`ecdc252773a79a9a785e90612bf5c7bf42b08c7918356f68fbb97b571f9514cf`; it has no
JSON companion. Pure model checks additionally cover positive last-attacker
redirection from same-room and cross-room ability actors, lethal remote OnKill
dispatch, optional Armor and missing-reference rejection. The curated inventory
is now 244 archives: 213 battles and 31 component calibrations.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-damage-per-hp-last-attacker.mt2f
dotnet run --project src/ModelChecks -c Release -- --damage-per-hp-pure
```

### DamagePerHp unit ability

`full-battle-damage-per-hp-ability.mt2f` records two native unit-ability plays
whose `DamagePerHpLastAttacker` effects lower their enemy attackers' health.
The muted Instant battle won with 17 actions, five EndTurns and 48 room stages;
there were zero capture failures, mismatches, unsupported transitions or
pending records. Independent policy replay covers the post-input suffix: 12
card actions and four EndTurns, including 16 parallel policy branches. A separate
actionwise check matches all 17 native card actions.
The test seeds one live `LastAttackerId` as a controlled combat input. Actions
before that input remain individually compared against native state; independent
policy replay starts from the first decision state containing it. The schema-103
binary is 31,941 bytes with 5,072 unique nodes and SHA-256
`7b9726b80ffde51e36672e1d9e7b6d77e3325422cdc708794976a8b3309e47ac`; it has no
JSON companion. At that capture milestone, the curated inventory had 246
archives: 215 battles and 31 component calibrations.

`full-battle-original-room-entry-status-relics.mt2f` records the unchanged
`RootBeforePyre`, `SpellWeaknessBelowPyre` and `PyregelOnEnterTopFloor` effects
on Heroes entering room 2. The Instant battle won with 21 card actions, seven
EndTurns and 55 matched stages; all three original status payloads have native
unit witnesses, with zero capture failures, mismatches, unsupported transitions
or pending records. The schema-132 binary is 31,851 bytes with 5,147 unique
nodes and SHA-256
`0a9d0ccc7ce83234efc50cc3221aa16c4c14bb7df69b8cfb62245f11506f12d3`; it has no
JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-room-entry-status-relics.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-room-entry-status-relics-only tests/fixtures/full-battle-original-room-entry-status-relics.mt2f
```

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-damage-per-hp-ability.mt2f
dotnet run --project src/ModelChecks -c Release -- --unit-ability-pure
dotnet run --project src/ModelChecks -c Release -- --damage-per-hp-pure
```

### Native DamageOverflow spell

`full-battle-damage-overflow.mt2f` records a zero-cost 20-damage spell carrying
the native `CardTraitDamageOverflow` FriendlyTeam trait. The schema-103 battle
won with 17 card actions and five EndTurns. Capture failures, mismatches,
unsupported transitions and pending records are zero. Independent replay
matches all actions, the policy chain and 16 parallel branches; two played
overflow spells damage same-team siblings. Pure checks cover all four team
modes, spill amount/order, exact lethal, armor and recursion suppression. The
24,704-byte archive has 3,927 unique nodes and SHA-256
`c0a1c9cde31b0f43d7a1971d1fa4d6c314c3470c7d8caade1257d18312c71ce9`; it has no
JSON companion. Original relic evidence remains 63/224 (28.13%); this fixture
adds battle-mechanism coverage only. Inventory is 247 archives: 216 battles and
31 component calibrations. Full-inventory replay remains deferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-damage-overflow.mt2f
```

`full-battle-original-unit-upgrade-trigger-relic.mt2f` captures the unchanged
`AddAttackToFunguy` relic on authored Funguy carriers with two Horde stacks.
Four native additions each apply +4 attack (2 → 6); four removals include one
observed -2 attack change (5 → 3). The muted Instant battle won with 23 card
actions, seven EndTurns and 57 stages, with zero capture failures, mismatches,
unsupported transitions or pending records. All actions and the 16-branch
policy replay match. The schema-133 archive is 32,734 bytes with 5,445 unique
nodes and SHA-256
`d7a1f6071199f506c53754f109140d5249d56b8e092b00204e77727e0c6743e2`; it has no
JSON companion. This brings accepted original relic evidence to 64/224
(28.57%) and the inventory to 248 archives: 217 battles and 31 calibrations.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-unit-upgrade-trigger-relic.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-unit-upgrade-triggers-pure
```

### Relic-granted Ephemeral routing

The schema-135 `full-battle-relic-card-ephemeral-upgrades.mt2f` confirms that a
temporary relic card upgrade adds `CardTraitEphemeral` to spells. One upgraded
spell is played and purged; another remains in hand and is purged by EndTurn.
The muted Instant battle won with 27 card actions and six EndTurns. Capture
failures, mismatches, unsupported transitions and pending records are zero.
Native checks verify both purge routes, 8 relic manager calls, 8 ordered
effect/filter results and four exact notifications; the 16-branch policy replay
matches. This fixture authors an Ephemeral payload on the original
`ReduceStarterCost` relic for lifecycle coverage, so it adds no original relic
owner to the 65/224 count.

The binary is 31,416 bytes with 5,278 unique nodes and SHA-256
`e4508851abacb5833012db73253152016365f290a88e81d0f84d55a9015230c4`; it has no
JSON companion. The inventory is now 249 archives: 218 battles and 31 component
calibrations. Full-inventory replay remains deferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-relic-card-ephemeral-upgrades.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-card-modifiers-only tests/fixtures/full-battle-relic-card-ephemeral-upgrades.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-cycle-only tests/fixtures/full-battle-relic-card-ephemeral-upgrades.mt2f
```

### Original ExtraToughTombs relic

`full-battle-relic-extra-tough-tombs.mt2f` records the unchanged original
`ExtraToughTombs` relic. Its native `OnlyTombs` filter accepts the pre-battle
Tomb carrier, applies +30 health and the unique `AddBurnoutToTombs` temporary
upgrade, and the battle plays that carrier. The summoned Train Steward is
55/55 with one `ephemeral` stack; native `OnPostCombatPoison` consumes it,
fires `OnBurnout`, then sacrifices the unit. The muted Instant battle won with
22 card actions, seven EndTurns and 59 stages. Capture failures, mismatches,
unsupported transitions and pending records are zero. Independent replay
matches all 59 room stages and 22 actions; battle search matches 22 decisions,
128 states and serial/parallel results.

This accepts `ExtraToughTombs`, bringing original relic evidence to 68/224
(30.36%). The schema-137 archive is 33,356 bytes with 5,388 unique nodes and
SHA-256
`1551a614cb42645e926c6637629d2d4a33320cdc7afdf52ea10b6920bb2a8ad2`; it has no
JSON companion. The inventory is now 251 archives: 220 battles and 31 component
calibrations. Full-inventory replay remains deferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-relic-extra-tough-tombs.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-card-modifiers-only tests/fixtures/full-battle-relic-extra-tough-tombs.mt2f
dotnet run --project src/ModelChecks -c Release -- --room-combat-only tests/fixtures/full-battle-relic-extra-tough-tombs.mt2f
```

### Random hand discard (schema 138)

`full-battle-random-discard.mt2f` records a targetless spell that randomly
discards one eligible card from hand. The model advances battle RNG by randomly
removing the cards that stay in hand, then routes the remaining candidates
through native discard callbacks. It excludes the resolving spell and cards
whose captured branch masks make them Infusion or Persistent. The muted Instant
battle won with 21 card actions, seven EndTurns and 63 stages. All seven native
applications discarded one card and matched the complete hand, pile and battle
RNG state. Capture failures, mismatches, unsupported transitions and pending
records are zero. Independent replay matches all 21 actions; search matches 21
decisions and 128 states, with serial and parallel results equal.

The binary archive is 34,323 bytes with 5,558 unique nodes and SHA-256
`5df2094729c61edfd97c47cb9b80d92ef0b24f5308e8cef4c3201a0eb49c58e8`; it has no
JSON companion. The inventory is now 253 archives: 222 battles and 31 component
calibrations. Full-inventory replay remains deferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-random-discard.mt2f
dotnet run --project src/ModelChecks -c Release -- --native-actions-only tests/fixtures/full-battle-random-discard.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-random-discard.mt2f
```

### Pure room capacity attachment (schema 139)

The binary archive has 21,259 bytes, 3,052 unique nodes and SHA-256
`ab9df0a218eb253025b46384faed92bc6e5a4cd3e1351d21a5fd0c8ed3b36f96`. It has no
JSON source or companion. Native evidence confirms the capacity change from 5 to
9, one active attachment with the expected card ID, and successful full-battle
completion. Independent replay matches all 52 room stages, 13 card cycles, 14
train phases, 11 spawns, seven EndTurns and the single card action.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-room-capacity-attachment.mt2f
```

### Original StealthWaxers relic (schema 140)

`full-battle-original-stealth-waxers.mt2f` captures the unchanged original
`StealthWaxers` relic. Its `SubtypeInDeck >= 1` condition grants one Endless
stack on the first eligible monster birth each turn. Four births produce
positive status and condition-counter witnesses. The muted Instant battle wins
with 52 stages, 18 card actions and six EndTurns; native capture has zero
failures, mismatches, unsupported transitions or pending records. Independent
model replay matches all 13 relic birth phases across 32 isolated branches.

This raises accepted original relic evidence to 69/224 (30.80%). The schema-140
archive is 36,271 bytes with 5,528 unique nodes and SHA-256
`70ad72b86200398a787b573e4b607b0ad95026de2fd2078de056fa656466536b`; it has no
JSON companion. The inventory is now 254 archives: 223 battles and 31 component
calibrations. Full-inventory replay remains deferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-stealth-waxers.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-spawn-status-only tests/fixtures/full-battle-original-stealth-waxers.mt2f
```

### Original IncreaseBurnoutStacks relic (schema 142)

`full-battle-original-burnout-stacks.mt2f` exercises the unchanged original
`IncreaseBurnoutStacks` definition. Its Monsters-team positive-stack rule adds
3 Ephemeral to a real player application; owner ablation proves 1 → 4 with the
relic and 1 without it. The 73.42-second muted Instant battle wins at Pyre 80
with 41 matched room stages, 26 card actions and five EndTurns. Capture failures,
mismatches, unsupported transitions and pending records are zero. Independent
replay matches the policy chain, a mid-battle root and 16 parallel branches.
The status-stack batch checks 35 actual and 336 boundary queries, including the
Ephemeral team, overflow, exclusion and permanent/deferred cases.

The binary archive is 35,966 bytes with 6,271 unique nodes and SHA-256
`591ea715e5ff5d7774941e17261e4fb02195e17c7a830806c6e49db8d933c79a`; it has no
JSON companion. The inventory is now 255 archives: 224 battles and 31 component
calibrations. Original relic evidence is 70/224 (31.25%). Full-inventory replay
remains deferred.

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -OriginalStatusRelicBatchBurnout -GameSpeed Instant
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-original-burnout-stacks.mt2f
dotnet run --project src/ModelChecks -c Release -- --original-status-relics-only tests/fixtures/full-battle-original-burnout-stacks.mt2f
```

### Recursion from Discard to Hand (schema 154)

`full-battle-card-recursion-discard-choice.mt2f` captures native
`WurmkinEtchings` with six Spell candidates in Discard and `ChooseToHand`. The
selected card leaves Discard and returns to Hand. The muted Instant battle wins
with 49 matched stages and 19 card actions; capture failures, mismatches,
unsupported transitions and pending records are zero. All 465 branch decision
contexts were captured. Search emits six distinct choices and checks 128 states
with identical serial and parallel results.

The game does not restore a Discard card through `ChooseToDeck`; that path calls
the Exhaust/Eaten restoration API, so the model explicitly rejects it. This
fixture adds no original relic owner. The accepted inventory is now 271 archives:
240 battles and 31 component calibrations, while original relic evidence remains
74/224 (33.04%).

The archive is 31,848 bytes with 5,331 unique nodes and SHA-256
`881ff052f7f22304759a8736bbe392cc4412d27d1c32490cdb85671efaf0d8ef`; it has no
JSON companion.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-card-recursion-discard-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --card-recursion-discard-only tests/fixtures/full-battle-card-recursion-discard-choice.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-card-recursion-discard-choice.mt2f
```

### TunedGear Burst relic (schema 155)

`full-battle-spawn-status-relics-tuned-gear.mt2f` records the unchanged original
`TunedGear` granting Burst to five player births; seven enemy births remain
unaffected. All 12 native birth phases and 32 focused branches match. The
independent full battle wins with 51 matched stages, 18 actions and six EndTurns,
with zero capture failures, mismatches, unsupported transitions or pending
records. Burst adds attacks after ordinary Multistrike and consumes one stack
before each such attack. Original relic evidence is now 75/224 (33.48%).

The 35,926-byte archive contains 5,518 unique nodes and has SHA-256
`1b067d579d9786ebb44cb1473b922c46633f94700a0a86297cca52947431b6a9`; no JSON
companion is tracked.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-spawn-status-relics-tuned-gear.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-spawn-status-only tests/fixtures/full-battle-spawn-status-relics-tuned-gear.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-spawn-status-relics-tuned-gear.mt2f
```

### BalatroEventChicot Mute relic (schema 156)

`full-battle-spawn-status-relics-boss-mute.mt2f` records the unchanged original
`BalatroEventChicot` relic applying 3 Muted to an enemy BossAll birth. The native
replay matches all 52 battle stages, 18 actions and six EndTurns, with zero
capture failures, mismatches, unsupported transitions or pending records. The
focused relic check verifies the exact owner/filter/payload, one BossAll witness,
and 32 isolated birth branches. The model's trigger checks preserve
IgnoreSilence callbacks and ordinary attacks while suppressing other triggers on
Muted units. Original relic evidence is now 76/224 (33.93%).

The schema-156 binary is 36,240 bytes with 5,566 unique nodes and SHA-256
`9bc308cd45b8a6984544b01bfd5a44567c66accf0e54c01c078007d99b197d0d`; no JSON
companion is tracked. The inventory is now 273 archives: 242 accepted battles
and 31 component calibrations.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-spawn-status-relics-boss-mute.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-spawn-status-only tests/fixtures/full-battle-spawn-status-relics-boss-mute.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-spawn-status-relics-boss-mute.mt2f
```

### Relic card OnCast effects (schema 157)

`full-battle-relic-card-cast-trigger-upgrades.mt2f` uses a synthetic
`RelicEffectAddTempUpgrade` payload that installs an ordered `CardEffectDamage`
OnCast effect. The native check confirms five ordered relic-manager results,
two trigger notifications and two plays of the upgraded card. The native battle
wins with 49 matched stages and 10 actions; capture failures, mismatches,
unsupported transitions and pending records are zero. Search checks 128 states
with equal serial and parallel results and no unsupported actions.

This establishes the OnCast card-effect path for temporary relic upgrades. It
does not establish an original relic owner or cover other trigger and room
ability payloads. The 26,109-byte binary has 3,990 unique nodes and SHA-256
`f83469ddd3c598088eaf28bcadf2fee99bae7f9be22d4ad50d8993fe43ebeb7a`; it has no
JSON companion. Inventory is now 274 archives: 243 battles and 31 component
calibrations. Original relic evidence remains 76/224 (33.93%).

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-relic-card-cast-trigger-upgrades.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-card-modifiers-only tests/fixtures/full-battle-relic-card-cast-trigger-upgrades.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-relic-card-cast-trigger-upgrades.mt2f
```

### Relic card OnCast effects on Monster cards (schema 158)

`full-battle-relic-card-cast-trigger-unit-upgrades.mt2f` exercises the same
temporary `RelicEffectAddTempUpgrade` OnCast damage on a playable Monster card.
The muted Instant native battle wins with 57 matched stages and nine card
actions. Capture failures, mismatches, unsupported transitions and pending
records are zero; all 355 branch decision contexts are captured. The focused
check confirms enemy-front damage, and battle search checks 128 states with
equal serial and parallel results.

This extends supported OnCast handling to Monster cards; it is synthetic
mechanism evidence and adds no original relic owner. The 28,581-byte binary has
4,389 unique nodes and SHA-256
`43ff4ed402996d5b94050423a4101a794e3ab11a7052336fb932f71b1bfa183c`; no JSON
companion is tracked. Inventory is now 275 archives: 244 accepted battles and
31 component calibrations. Original relic evidence remains 76/224 (33.93%).

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture full-battle-relic-card-cast-trigger-unit-upgrades.mt2f
dotnet run --project src/ModelChecks -c Release -- --relic-card-modifiers-only tests/fixtures/full-battle-relic-card-cast-trigger-unit-upgrades.mt2f
dotnet run --project src/ModelChecks -c Release -- --battle-search-only tests/fixtures/full-battle-relic-card-cast-trigger-unit-upgrades.mt2f
```
