using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardModifierChecks
{
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
        bool sawExpectedRejection = statusUpgradeScenario || piercingUpgradeScenario
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
        var generated = root.GetProperty("CardGenerations").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CombatContext>()!,
            Actual: row.GetProperty("Actual").Deserialize<CombatContext>()!)).ToArray();
        var births = generated.SelectMany(row => row.Actual.CardInstances!.Where(card => card.InstanceId >= row.Before.NextCardId)).ToArray();
        if (!statusUpgradeScenario && !piercingUpgradeScenario)
            Require(births.Any(card => card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "ReduceStarterCost" ||
                rows.SelectMany(row => row.Relics).SelectMany(relic => relic.CardModifiers ?? []).Any(effect => effect.Upgrade?.DataId == upgrade.DataId))),
                "No battle-generated eligible card received an original relic upgrade.");
        string coverage = statusUpgradeScenario ? "card status upgrades reached summoned units" :
            piercingUpgradeScenario ? "IgnoreArmor relic traits retained on played cards" : "eligible battle-generated cards";
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
