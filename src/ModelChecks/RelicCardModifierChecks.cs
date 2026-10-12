using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardModifierChecks
{
    internal static void Run()
    {
        var definition = new CardMaskDefinition("monster", "Monster", "Common", true, true, true,
            [], [], [], [], null, false, 1, 0, false, false, false);
        var descriptor = new CardMaskDescriptor(definition, 1, [], []);
        CardUpgradeMaskMetadata Metadata(IReadOnlyList<UpgradeMaskStatus>? statuses = null) =>
            new CardUpgradeMaskMetadata(statuses ?? [], false, false, false, false);
        CardUpgradeModifier Modifier(string id, CardLifecycleUpgrade lifecycle, IReadOnlyList<CombatStatus>? statuses = null,
            IReadOnlyList<UpgradeMaskStatus>? maskStatuses = null, IReadOnlyList<string>? interactions = null, string? assetKey = null) => new CardUpgradeModifier(id, assetKey ?? id,
                lifecycle.Values.Stats, statuses ?? [], false, false, false, 0, 0, interactions ?? [],
                maskMetadata: Metadata(maskStatuses), lifecycle: lifecycle);

        var permanentLifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("permanent-remove-piercing",
            new CardStatModifier(), [], false, false, false, false), "permanent-remove-piercing", false, false,
            [], ["CardTraitIgnoreArmor"], false, [], [], []);
        var permanent = Modifier("permanent-remove-piercing", permanentLifecycle);
        var firstLifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("temporary-add-piercing",
            new CardStatModifier(), [], false, false, false, false), "temporary-add-piercing", false, false,
            [new CardTraitValue("CardTraitIgnoreArmor", "CardTraitIgnoreArmor", 0, 0, true, 0)], [], false, [], [], []);
        var firstUpgrade = Modifier("temporary-add-piercing", firstLifecycle);
        var armor = new UpgradeMaskStatus("armor", 2);
        var secondLifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("temporary-armor",
            new CardStatModifier(), [armor], false, false, false, false), "temporary-armor", false, false,
            [], [], false, [], [], []);
        var secondUpgrade = Modifier("temporary-armor", secondLifecycle, [new CombatStatus("armor", 2)], [armor]);
        var traitFilter = new CardUpgradeMaskRule(cardType: "Monster",
            traits: new UpgradeMaskContent<string>(["CardTraitIgnoreArmor"]));
        var relic = new CombatRelicState("test-relic", "TestRelic",
            [RelicModel.AddTempUpgrade, RelicModel.AddTempUpgrade], cardModifiers:
            [new RelicCardModifier(0, new RelicCardUpgradeRule("temporary-add-piercing", true, firstLifecycle, []), firstUpgrade, false),
             new RelicCardModifier(1, new RelicCardUpgradeRule("temporary-armor", true, secondLifecycle, [traitFilter]), secondUpgrade, false)]);
        var card = new CardInstanceState(1, definition.DataId,
            new CardModifiers(new CardStatModifier(), [permanent], 0, []), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: descriptor);
        RelicCardModifierResult result = RelicCardModifierModel.Apply(card, [relic], resetTemporary: true);
        Require(result.Supported, "Permanent trait removals could not be replayed through relic reset: " + result.UnsupportedReason);
        Require(result.Dispatches.Count == 2 && result.Dispatches[0].UpgradeAdded &&
            result.Dispatches[1].Filters.Single().Accepted == false && !result.Dispatches[1].UpgradeAdded &&
            result.Card!.Temporary.Upgrades.Count == 1 && result.Card.Temporary.Upgrades[0].DataId == "temporary-add-piercing",
            "Relic reset lost a permanent removed-trait rule before a later effect filter.");
        var resetAgain = RelicCardModifierModel.Apply(result.Card!, [relic], resetTemporary: true);
        Require(resetAgain.Supported && JsonSerializer.Serialize(resetAgain.Card) == JsonSerializer.Serialize(result.Card) &&
            result.Card!.Permanent.Upgrades[0].Lifecycle?.RemovedRuntimeTypes.SequenceEqual(["CardTraitIgnoreArmor"]) == true,
            "Repeated relic resets must retain permanent upgrade lifecycle metadata and ordered filter behavior.");
        Console.WriteLine("RELIC-CARD-RESET-CHECKS PASS: permanent removed traits survive reset and constrain later ordered effect filters.");

        var abilityDefinition = new UnitAbilityDefinition("steward-sacrifice", true, 1, 1, null);
        var ability = new AbilityChangeRule(abilityDefinition, false, false,
            [new CombatTrigger("OnOwnAbilityActivated", false, false, true, 1, [], skipDuringDeployment: false)]);
        var abilityValues = new CardMaskUpgrade("steward-sacrifice-upgrade", new CardStatModifier(), [], false, false, false, true);
        var abilityLifecycle = new CardLifecycleUpgrade(abilityValues, "StewardSacrificeDamage_Upgrade", true, false,
            [], [], false, [], [], []);
        var abilityUpgrade = new CardUpgradeModifier("steward-sacrifice-upgrade", "StewardSacrificeDamage_Upgrade",
            new CardStatModifier(), [], false, true, false, 0, 0, [], abilityUpgrade: ability,
            maskMetadata: Metadata(), lifecycle: abilityLifecycle);
        var abilityCard = new CardInstanceState(2, definition.DataId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: descriptor);
        var abilityModifier = new RelicCardModifier(0,
            new RelicCardUpgradeRule("steward-sacrifice-upgrade", true, abilityLifecycle, []), abilityUpgrade, false);
        var abilityRelic = new CombatRelicState("ability-relic", "AbilityRelic", [RelicModel.AddTempUpgrade], cardModifiers: [abilityModifier]);
        RelicCardModifierResult abilityResult = RelicCardModifierModel.Apply(abilityCard, [abilityRelic], resetTemporary: false);
        Require(abilityResult.Supported && abilityResult.Card!.Temporary.Upgrades.Single().AbilityUpgrade?.Definition?.DataId ==
            "steward-sacrifice", "A supported relic unit-ability upgrade was rejected or lost during application: " + abilityResult.UnsupportedReason);
        Console.WriteLine("RELIC-CARD-ABILITY-CHECKS PASS: unit ability upgrade payload survives native relic application.");

        var priorUpgradeLifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("prior-upgrade", new CardStatModifier(damage: 20), [],
            false, false, false, false), "prior-upgrade-asset", false, false, [], [], false, [], [], []);
        var priorUpgrade = Modifier("prior-upgrade", priorUpgradeLifecycle, assetKey: "prior-upgrade-asset");
        var replacementLifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("replacement-upgrade", new CardStatModifier(damage: 5), [],
            false, false, false, false), "replacement-upgrade-asset", false, false, [], [], false, [], ["prior-upgrade-asset"], []);
        var replacementUpgrade = Modifier("replacement-upgrade", replacementLifecycle, interactions: ["Upgrade replacements"],
            assetKey: "replacement-upgrade-asset");
        var replacementRelic = new CombatRelicState("replacement-relic", "ReplacementRelic", [RelicModel.AddTempUpgrade], cardModifiers:
            [new RelicCardModifier(0, new RelicCardUpgradeRule("replacement-upgrade-asset", true, replacementLifecycle, []), replacementUpgrade, false)]);
        var replacementCard = new CardInstanceState(3, definition.DataId, CardModifiers.Empty(),
            new CardModifiers(new CardStatModifier(), [priorUpgrade], 0, []), 0, 0, 0, [], maskDescriptor: descriptor);
        RelicCardModifierResult replacementResult = RelicCardModifierModel.Apply(replacementCard, [replacementRelic], resetTemporary: false);
        Require(replacementResult.Supported, "Relic replacement was rejected: " + replacementResult.UnsupportedReason);
        Require(replacementResult.Card!.Temporary.Upgrades.Count == 1 &&
            replacementResult.Card.Temporary.Upgrades[0].DataId == "replacement-upgrade",
            "Relic replacement did not synchronize the prior upgrade: " + JsonSerializer.Serialize(replacementResult.Card.Temporary.Upgrades));
        Require(replacementResult.Dispatches.Single().Returned && !replacementResult.Dispatches.Single().UpgradeAdded,
            "Replacement dispatch did not preserve the native returned-versus-list-growth distinction.");
        string? replacementError = CardModifierModel.UnsupportedReason(replacementResult.Card);
        Require(replacementError == null, "Installed replacement metadata remained unsupported: " + replacementError);
        CardPlayRule replacementPlay = CardModifierModel.Resolve(new CardPlayRule(definition.DataId, "Monster", 0, "Unit", "Standby",
            null, []), replacementResult.Card!);
        Require(replacementPlay.ExternalInteractions.Count == 0 && CardModifierModel.UpgradedStat(10, "Damage", true,
            replacementResult.Card.Permanent, replacementResult.Card.Temporary) == 15,
            "Installed replacement metadata remained unsupported or the removed upgrade still affected card play.");
        Console.WriteLine("RELIC-CARD-REPLACEMENT-CHECKS PASS: replacement lifecycle and battle card modifiers stay synchronized.");
    }

    internal static void Native(FixtureValue root, bool required = false, bool requireScenarioCoverage = true)
    {
        if (!root.TryGetProperty("RelicCardModifiersEnabled", out var enabled) || !enabled.GetBoolean())
        { Require(!required, "Fixture lacks enabled native relic manager card modifiers."); return; }
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Native relic manager capture is incomplete or from another game build.");
        var rows = root.GetProperty("RelicCardModifiers").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CardInstanceState>()!,
            ContextBefore: row.TryGetProperty("ContextBefore", out var beforeContext) ? beforeContext.Deserialize<CombatContext>() : null,
            ContextAfter: row.TryGetProperty("ContextAfter", out var afterContext) ? afterContext.Deserialize<CombatContext>() : null,
            Relics: row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
            Reset: row.GetProperty("ResetTemporary").GetBoolean(),
            Actual: row.GetProperty("Actual").Deserialize<CardInstanceState>()!,
            Dispatches: row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!,
            Notifications: row.GetProperty("Notifications").EnumerateArray().Select(item =>
                (Relic: item.GetProperty("RelicIndex").GetInt32(), Effect: item.GetProperty("EffectIndex").GetInt32())).ToArray())).ToArray();
        bool statusUpgradeScenario = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "relic-card-status-upgrades";
        bool piercingUpgradeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "relic-card-piercing-upgrades";
        bool selfPurgeUpgradeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "relic-card-self-purge-upgrades";
        bool exhaustUpgradeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "relic-card-exhaust-upgrades";
        bool abilityUpgradeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "relic-card-ability-upgrades";
        bool allUnitsMultistrikeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "all-units-multistrike-mutator";
        bool triggerUpgradeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "relic-card-trigger-upgrades";
        bool replacementUpgradeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "relic-card-replacement-upgrades";
        bool deployableRelicScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "original-deployable-relic";
        bool magebladeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "mageblade";
        bool sawExpectedRejection = statusUpgradeScenario || piercingUpgradeScenario || abilityUpgradeScenario || triggerUpgradeScenario ||
            replacementUpgradeScenario || deployableRelicScenario
            ? rows.Any(row => row.Dispatches.Any(effect => !effect.Returned && !effect.UpgradeAdded))
            : rows.Any(row => !row.Reset && row.Dispatches.Any(effect => effect.Returned && !effect.UpgradeAdded));
        Require(rows.Length > 0, "Native relic manager capture contains no calls.");
        // Composite draw fixtures acquire the already-calibrated starter relic
        // before their trace begins and need not generate an eligible card.
        // Compare every call; dedicated relic fixtures also require their
        // reset, rejection and successful-generation coverage witnesses.
        if (requireScenarioCoverage) Require(sawExpectedRejection &&
            rows.Any(row => row.Reset && row.Dispatches.Any(effect => effect.UpgradeAdded)) &&
            rows.Any(row => row.Dispatches.Any(effect => !effect.Returned)), statusUpgradeScenario
                ? "Native manager lacks reset and failed-eligibility rejection."
                : "Native manager lacks reset/repeat/unique rejection and failed eligibility.");
        string expectedOwner = magebladeScenario ? "PojuMagebladeMagicPowerRelic" :
            root.TryGetProperty("ModifierScenario", out var ownerScenario) && ownerScenario.GetString() == "original-deployable-relic"
                ? "NonBannerUnitsHaveDeployable" : allUnitsMultistrikeScenario ? "AllUnitsMultistrike" : "ReduceStarterCost";
        Require(rows.Any(row => row.Relics.Any(relic => relic.AssetKey == expectedOwner &&
            (magebladeScenario
                ? relic.EffectTypes.SequenceEqual(new[] { RelicModel.ModifyCardMagicalPower }) &&
                    relic.MagicPowerModifiers?.Count == 1
                : allUnitsMultistrikeScenario
                    ? relic.EffectTypes.SequenceEqual(new[] { "RelicEffectAddTempUpgrade", "RelicEffectAddStatusEffectOnSpawn" })
                    : relic.EffectTypes.SequenceEqual(new[] { "RelicEffectAddTempUpgrade" })))),
            "Expected original relic owner/effect order is missing.");
        if (magebladeScenario)
        {
            bool nativeSpellModifier = rows.Any(row => row.Reset && row.Before.MaskDescriptor?.Definition.CanScaleWithMagicPower == true &&
                row.Relics.Any(relic => relic.AssetKey == expectedOwner && relic.MagicPowerModifiers?.Count == 1) &&
                row.Actual.Temporary.Offsets.Damage == unchecked(row.Relics.Single(relic => relic.AssetKey == expectedOwner)
                    .MagicPowerModifiers!.Single().PowerModifier * CardMagicPowerModel.Multiplier(row.Before)) &&
                row.Actual.Temporary.Offsets.Heal == unchecked(row.Relics.Single(relic => relic.AssetKey == expectedOwner)
                    .MagicPowerModifiers!.Single().PowerModifier * CardMagicPowerModel.Multiplier(row.Before)));
            Require(nativeSpellModifier, "Native magic-power relic did not match spell eligibility and trait-scaled temporary offsets.");
        }
        string parent = JsonSerializer.Serialize(rows);
        void Check()
        {
            foreach (var row in rows)
            {
                var predicted = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset, row.ContextBefore);
                Require(predicted.Supported, "Native relic manager cannot be simulated: " + predicted.UnsupportedReason);
                Equal(predicted.Card, row.Actual, "complete native manager card state");
                Equal(predicted.Dispatches, row.Dispatches, "ordered native manager effects, filter outcomes and unique return/add flags");
                if (row.ContextAfter != null)
                {
                    Equal(predicted.Context!.Relics, row.ContextAfter.Relics, "native relic condition state");
                    Equal(predicted.Context.Statistics, row.ContextAfter.Statistics, "native relic condition statistic cache");
                }
                var notifications = predicted.Dispatches.Where(effect => effect.Returned).Select(effect => (effect.RelicIndex, effect.EffectIndex)).ToArray();
                Require(notifications.SequenceEqual(row.Notifications), "Native manager notified a different effect/order, including unique rejection.");
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows) == parent, "Relic manager replay mutated its source cards or relics.");
        if (root.TryGetProperty("ModifierScenario", out var modifierScenario) &&
            modifierScenario.GetString() == "conditional-relic-card-upgrades")
        {
            bool sawCondition = false, sawSpentGate = false, sawTurnReset = false;
            foreach (var row in rows)
            {
                var relics = row.ContextBefore?.Relics;
                if (relics == null) continue;
                for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
                foreach (var effect in relics[relicIndex].CardModifiers ?? Array.Empty<RelicCardModifier>())
                {
                    if (effect.Conditions.Count == 0) continue;
                    sawCondition = true;
                    bool spent = effect.Conditions.Any(condition => condition.TrackTriggerCount &&
                        condition.DurationTriggerCount - condition.ValueAtLastTrigger >= condition.Value);
                    bool dispatched = row.Dispatches.Any(dispatch => dispatch.RelicIndex == relicIndex && dispatch.EffectIndex == effect.EffectIndex);
                    if (spent && !dispatched) sawSpentGate = true;
                }
            }
            bool previouslyTriggered = false;
            foreach (var row in rows)
            {
                var relics = row.ContextBefore?.Relics;
                if (relics == null) continue;
                foreach (var relic in relics)
                {
                    if (relic.AssetKey != "ReduceStarterCost") continue;
                    foreach (var effect in relic.CardModifiers ?? Array.Empty<RelicCardModifier>())
                    foreach (var condition in effect.Conditions)
                    {
                        if (!condition.TrackTriggerCount || condition.Query.Duration != "ThisTurn") continue;
                        if (condition.DurationTriggerCount > 0) previouslyTriggered = true;
                        else if (previouslyTriggered && !condition.Triggered) sawTurnReset = true;
                    }
                }
            }
            Require(sawCondition && sawSpentGate && sawTurnReset,
                "Conditional relic capture lacks a native trigger limit, spent-condition skip or ThisTurn reset.");
        }
        if (statusUpgradeScenario)
        {
            bool statusReachedSummonedUnit = root.GetProperty("Actions").EnumerateArray().Any(action =>
            {
                int cardId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CardInstanceState? card = after.Spawn.Train.Context!.FindCard(cardId);
                return card?.Temporary.Upgrades.Any(upgrade => upgrade.Statuses.Any(status => status.Id == "armor")) == true &&
                    after.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == cardId &&
                        unit.Status("armor")?.Stacks > 0);
            });
            Require(statusReachedSummonedUnit,
                "A native card-side relic armor upgrade did not reach the summoned unit in the independent policy.");
        }
        if (piercingUpgradeScenario)
        {
            bool modifierContainsTrait = rows.Any(row => row.Actual.Temporary.Upgrades.Any(upgrade =>
                upgrade.Lifecycle?.AddedTraits.Any(trait => trait.DeclaredName == "CardTraitIgnoreArmor") == true));
            bool playedCardContainsTrait = root.GetProperty("Actions").EnumerateArray().Any(action =>
            {
                int cardId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CardInstanceState? card = after.Spawn.Train.Context!.FindCard(cardId);
                return card?.MaskDescriptor != null && CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitIgnoreArmor");
            });
            Require(modifierContainsTrait && playedCardContainsTrait,
                "Native relic piercing trait was not retained on an independently played card.");
        }
        if (exhaustUpgradeScenario)
        {
            int exhaustedPlays = 0;
            foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                CardInstanceState? card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
                if (card?.Temporary.Upgrades.Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait =>
                    trait.RuntimeType == "CardTraitExhaustState") == true) != true) continue;

                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatContext context = actual.Spawn.Train.Context!;
                CardInstanceState? played = context.FindCard(action.CardInstanceId);
                Require(played?.ExhaustState?.UpgradeTrait?.IsExhausted == true &&
                    context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == action.CardInstanceId) &&
                    !context.OtherPiles.Single(pile => pile.Name == "Standby").Cards.Any(item => item.InstanceId == action.CardInstanceId) &&
                    context.Statistics!.Value(action.CardInstanceId, "TimesExhausted") == 2,
                    "A played relic-upgraded Consume spell did not reach Exhausted with native private state and statistics.");
                exhaustedPlays++;
            }
            Require(exhaustedPlays > 0, "The relic Consume scenario did not play a card carrying its temporary trait.");
            Console.WriteLine($"NATIVE-RELIC-CARD-EXHAUST-CHECKS PASS: {exhaustedPlays} relic-upgraded cards reached Exhausted with private trait state.");
        }
        if (abilityUpgradeScenario)
        {
            int abilityUpgradedSummons = 0;
            foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                CardInstanceState? card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
                AbilityChangeRule? ability = card?.Temporary.Upgrades.Select(upgrade => upgrade.AbilityUpgrade)
                    .FirstOrDefault(candidate => candidate?.Definition != null);
                if (ability?.Definition == null) continue;
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? spawned = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .FirstOrDefault(unit => unit.SpawnerCardId == action.CardInstanceId);
                Require(spawned?.Ability?.DataId == ability.Definition.DataId,
                    "A relic-upgraded card did not give its summoned unit the captured native ability.");
                abilityUpgradedSummons++;
            }
            Require(abilityUpgradedSummons > 0,
                "The native relic ability scenario did not play and summon from an ability-upgraded card.");
            Console.WriteLine($"NATIVE-RELIC-CARD-ABILITY-CHECKS PASS: {abilityUpgradedSummons} relic-upgraded summons received their native unit abilities.");
        }
        if (triggerUpgradeScenario)
        {
            int triggerUpgradedSummons = 0;
            foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                CardInstanceState? card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
                CardUpgradeModifier? upgrade = card?.Temporary.Upgrades.FirstOrDefault(candidate =>
                    candidate.TriggerUpgrades?.Any(trigger => trigger.Kind == "OnKill") == true);
                if (upgrade == null) continue;
                CombatTrigger expected = upgrade.TriggerUpgrades!.Single(trigger => trigger.Kind == "OnKill");
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? spawned = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .FirstOrDefault(unit => unit.SpawnerCardId == action.CardInstanceId);
                CombatTrigger? applied = spawned?.Triggers.FirstOrDefault(trigger => trigger.Kind == "OnKill");
                Require(applied != null && applied.Once == expected.Once &&
                    applied.IgnoreSilence == expected.IgnoreSilence && applied.FireCount == expected.FireCount &&
                    applied.TriggerAtThreshold == expected.TriggerAtThreshold,
                    "A relic-upgraded card did not give its summoned unit the native OnKill trigger.");
                Equal(expected.Effects, applied!.Effects, "relic OnKill trigger effects");
                triggerUpgradedSummons++;
            }
            Require(triggerUpgradedSummons > 0,
                "The native relic trigger scenario did not play and summon from an OnKill-upgraded card.");
            Console.WriteLine($"NATIVE-RELIC-CARD-TRIGGER-CHECKS PASS: {triggerUpgradedSummons} relic-upgraded summons received their native OnKill triggers.");
        }
        if (replacementUpgradeScenario)
        {
            bool replacementObserved = rows.Any(row =>
            {
                RelicCardModifier? effect = row.Relics.Where(relic => relic.AssetKey == "ReduceStarterCost")
                    .SelectMany(relic => relic.CardModifiers ?? Array.Empty<RelicCardModifier>())
                    .FirstOrDefault(candidate => candidate.Rule.Upgrade?.ReplacedAssets.Count > 0);
                if (effect?.Rule.Upgrade == null) return false;
                var replacedAssets = effect.Rule.Upgrade.ReplacedAssets;
                return !row.Reset && row.Before.Temporary.Upgrades.Any(upgrade => replacedAssets.Contains(upgrade.AssetKey)) &&
                    row.Actual.Temporary.Upgrades.All(upgrade => !replacedAssets.Contains(upgrade.AssetKey)) &&
                    row.Actual.Temporary.Upgrades.Any(upgrade => upgrade.DataId == effect.Rule.Upgrade.Values.DataId) &&
                    row.Dispatches.Any(dispatch => dispatch.Returned && !dispatch.UpgradeAdded);
            });
            Require(replacementObserved,
                "Native relic modifier replay did not replace the targeted temporary armor upgrade while preserving list-growth flags.");
            Console.WriteLine("NATIVE-RELIC-CARD-REPLACEMENT-CHECKS PASS: native replacement target removed and replacement card upgrade retained.");
        }
        if (allUnitsMultistrikeScenario)
        {
            var upgradedCardIds = rows.Where(row => row.Relics.Any(relic => relic.AssetKey == "AllUnitsMultistrike"))
                .Where(row => row.Dispatches.Any(dispatch => dispatch.UpgradeAdded))
                .Select(row => row.Before.InstanceId).ToHashSet();
            Require(upgradedCardIds.Count > 0 && rows.Where(row => row.Relics.Any(relic => relic.AssetKey == "AllUnitsMultistrike"))
                .Where(row => row.Dispatches.Any(dispatch => dispatch.UpgradeAdded)).All(row => row.Actual!.Temporary.Upgrades.Any(upgrade =>
                    upgrade.AssetKey == "Multistrike1" && upgrade.Statuses.Any(status => status.Id == "multistrike" && status.Stacks == 1))),
                "The AllUnitsMultistrike mutator did not install its exact unique status upgrade.");

            var playedCarriers = new HashSet<int>();
            foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                if (!upgradedCardIds.Contains(action.CardInstanceId)) continue;
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                if (actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                    unit.SpawnerCardId == action.CardInstanceId && unit.Status("multistrike")?.Stacks == 1))
                    playedCarriers.Add(action.CardInstanceId);
            }
            Require(playedCarriers.Count > 0, "No mutator-upgraded card summoned a Multistrike carrier.");

            int combatHits = 0;
            foreach (FixtureValue entry in root.GetProperty("Stages").EnumerateArray())
            {
                if (entry.GetProperty("Kind").GetString() != "Exchange") continue;
                RoomCombatState before = entry.GetProperty("Before").Deserialize<RoomCombatState>()!;
                RoomCombatState actual = entry.GetProperty("Actual").Deserialize<RoomCombatState>()!;
                var afterById = actual.Units.ToDictionary(unit => unit.Id);
                bool hit = before.Units.Any(unit => unit.Team == CombatTeam.Player && playedCarriers.Contains(unit.SpawnerCardId) &&
                    unit.Attacks > 1 && before.Units.Where(enemy => enemy.Team == CombatTeam.Enemy).Any(enemy =>
                        !afterById.TryGetValue(enemy.Id, out CombatUnit? after) || after.Health < enemy.Health));
                if (hit) combatHits++;
            }
            Require(combatHits > 0, "No native combat exchange shows an AllUnitsMultistrike carrier damaging an enemy.");

            int enemyBirths = 0;
            foreach (FixtureValue entry in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
            {
                if (entry.GetProperty("OnlyCovenants").GetBoolean()) continue;
                RoomCombatState before = entry.GetProperty("Before").Deserialize<RoomCombatState>()!;
                RoomCombatState after = entry.GetProperty("After").Deserialize<RoomCombatState>()!;
                int unitId = entry.GetProperty("UnitId").GetInt32();
                CombatUnit? initial = before.Units.FirstOrDefault(unit => unit.Id == unitId);
                CombatUnit? spawned = after.Units.FirstOrDefault(unit => unit.Id == unitId);
                if (initial?.Team == CombatTeam.Enemy && initial.Status("multistrike") == null && spawned?.Status("multistrike")?.Stacks == 1)
                    enemyBirths++;
            }
            Require(enemyBirths > 0, "The original Heroes-side Multistrike birth effect had no native enemy witness.");
            Console.WriteLine($"NATIVE-ALL-UNITS-MULTISTRIKE-CHECKS PASS: {upgradedCardIds.Count} upgraded cards, {playedCarriers.Count} played carriers, {combatHits} damaging exchanges, {enemyBirths} enemy births.");
        }
        var generated = root.GetProperty("CardGenerations").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CombatContext>()!,
            Actual: row.GetProperty("Actual").Deserialize<CombatContext>()!)).ToArray();
        var births = generated.SelectMany(row => row.Actual.CardInstances!.Where(card => card.InstanceId >= row.Before.NextCardId)).ToArray();
        if (requireScenarioCoverage && !deployableRelicScenario && !statusUpgradeScenario && !piercingUpgradeScenario && !selfPurgeUpgradeScenario &&
            !exhaustUpgradeScenario && !abilityUpgradeScenario && !triggerUpgradeScenario && !replacementUpgradeScenario &&
            !allUnitsMultistrikeScenario && !magebladeScenario)
            Require(births.Any(card => card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "ReduceStarterCost" ||
                rows.SelectMany(row => row.Relics).SelectMany(relic => relic.CardModifiers ?? []).Any(effect => effect.Upgrade?.DataId == upgrade.DataId))),
                "No battle-generated eligible card received an original relic upgrade.");
        string coverage = !requireScenarioCoverage ? "all recorded composite eligibility results" :
            deployableRelicScenario ? "eligible deployment-phase units are independently played" :
            statusUpgradeScenario ? "card status upgrades reached summoned units" :
            piercingUpgradeScenario ? "IgnoreArmor relic traits retained on played cards" :
            selfPurgeUpgradeScenario ? "SelfPurge relic traits retained on played cards" :
            exhaustUpgradeScenario ? "Consume relic traits routed played cards to Exhausted" :
            abilityUpgradeScenario ? "unit ability upgrades reached summoned units" :
            triggerUpgradeScenario ? "OnKill character triggers reached summoned units" :
            replacementUpgradeScenario ? "native temporary card upgrade replacement" :
            allUnitsMultistrikeScenario ? "Multistrike applied to owned and spawned cards" :
            magebladeScenario ? "native magic-power spell offsets" : "eligible battle-generated cards";
        Console.WriteLine($"NATIVE-RELIC-CARD-MODIFIER-CHECKS PASS: {rows.Length} original manager calls, {rows.Sum(row => row.Dispatches.Length)} ordered effect/filter results, " +
            $"{rows.Sum(row => row.Notifications.Length)} exact trigger notifications, {coverage} and 32 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
