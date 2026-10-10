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
            IReadOnlyList<UpgradeMaskStatus>? maskStatuses = null) => new CardUpgradeModifier(id, id,
                new CardStatModifier(), statuses ?? [], false, false, false, 0, 0, [],
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
    }

    internal static void Native(FixtureValue root, bool required = false)
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
        bool abilityUpgradeScenario = root.TryGetProperty("ModifierScenario", out scenario) &&
            scenario.GetString() == "relic-card-ability-upgrades";
        bool sawExpectedRejection = statusUpgradeScenario || piercingUpgradeScenario || abilityUpgradeScenario
            ? rows.Any(row => row.Dispatches.Any(effect => !effect.Returned && !effect.UpgradeAdded))
            : rows.Any(row => !row.Reset && row.Dispatches.Any(effect => effect.Returned && !effect.UpgradeAdded));
        Require(rows.Length > 0 && sawExpectedRejection &&
            rows.Any(row => row.Reset && row.Dispatches.Any(effect => effect.UpgradeAdded)) &&
            rows.Any(row => row.Dispatches.Any(effect => !effect.Returned)), statusUpgradeScenario
                ? "Native manager lacks reset and failed-eligibility rejection."
                : "Native manager lacks reset/repeat/unique rejection and failed eligibility.");
        Require(rows.Any(row => row.Relics.Any(relic => relic.AssetKey == "ReduceStarterCost" &&
            relic.EffectTypes.SequenceEqual(new[] { "RelicEffectAddTempUpgrade" }))), "Original starter-cost relic is missing.");
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
        var generated = root.GetProperty("CardGenerations").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CombatContext>()!,
            Actual: row.GetProperty("Actual").Deserialize<CombatContext>()!)).ToArray();
        var births = generated.SelectMany(row => row.Actual.CardInstances!.Where(card => card.InstanceId >= row.Before.NextCardId)).ToArray();
        if (!statusUpgradeScenario && !piercingUpgradeScenario && !selfPurgeUpgradeScenario && !abilityUpgradeScenario)
            Require(births.Any(card => card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "ReduceStarterCost" ||
                rows.SelectMany(row => row.Relics).SelectMany(relic => relic.CardModifiers ?? []).Any(effect => effect.Upgrade?.DataId == upgrade.DataId))),
                "No battle-generated eligible card received an original relic upgrade.");
        string coverage = statusUpgradeScenario ? "card status upgrades reached summoned units" :
            piercingUpgradeScenario ? "IgnoreArmor relic traits retained on played cards" :
            selfPurgeUpgradeScenario ? "SelfPurge relic traits retained on played cards" :
            abilityUpgradeScenario ? "unit ability upgrades reached summoned units" : "eligible battle-generated cards";
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
