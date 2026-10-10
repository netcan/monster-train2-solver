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
            Relics: row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
            Reset: row.GetProperty("ResetTemporary").GetBoolean(),
            Actual: row.GetProperty("Actual").Deserialize<CardInstanceState>()!,
            Dispatches: row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!,
            Notifications: row.GetProperty("Notifications").EnumerateArray().Select(item =>
                (Relic: item.GetProperty("RelicIndex").GetInt32(), Effect: item.GetProperty("EffectIndex").GetInt32())).ToArray())).ToArray();
        Require(rows.Length > 0 && rows.Any(row => !row.Reset && row.Dispatches.Any(effect => effect.Returned && !effect.UpgradeAdded)) &&
            rows.Any(row => row.Reset && row.Dispatches.Any(effect => effect.UpgradeAdded)) &&
            rows.Any(row => row.Dispatches.Any(effect => !effect.Returned)), "Native manager lacks reset/repeat/unique rejection and failed eligibility.");
        Require(rows.Any(row => row.Relics.Any(relic => relic.AssetKey == "ReduceStarterCost" &&
            relic.EffectTypes.SequenceEqual(new[] { "RelicEffectAddTempUpgrade" }))), "Original starter-cost relic is missing.");
        string parent = JsonSerializer.Serialize(rows);
        void Check()
        {
            foreach (var row in rows)
            {
                var predicted = RelicCardModifierModel.Apply(row.Before, row.Relics, row.Reset);
                Require(predicted.Supported, "Native relic manager cannot be simulated: " + predicted.UnsupportedReason);
                Equal(predicted.Card, row.Actual, "complete native manager card state");
                Equal(predicted.Dispatches, row.Dispatches, "ordered native manager effects, filter outcomes and unique return/add flags");
                var notifications = predicted.Dispatches.Where(effect => effect.Returned).Select(effect => (effect.RelicIndex, effect.EffectIndex)).ToArray();
                Require(notifications.SequenceEqual(row.Notifications), "Native manager notified a different effect/order, including unique rejection.");
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows) == parent, "Relic manager replay mutated its source cards or relics.");
        var generated = root.GetProperty("CardGenerations").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<CombatContext>()!,
            Actual: row.GetProperty("Actual").Deserialize<CombatContext>()!)).ToArray();
        var births = generated.SelectMany(row => row.Actual.CardInstances!.Where(card => card.InstanceId >= row.Before.NextCardId)).ToArray();
        Require(births.Any(card => card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "ReduceStarterCost" ||
            rows.SelectMany(row => row.Relics).SelectMany(relic => relic.CardModifiers ?? []).Any(effect => effect.Upgrade?.DataId == upgrade.DataId))),
            "No battle-generated eligible card received an original relic upgrade.");
        Console.WriteLine($"NATIVE-RELIC-CARD-MODIFIER-CHECKS PASS: {rows.Length} original manager calls, {rows.Sum(row => row.Dispatches.Length)} ordered effect/filter results, " +
            $"{rows.Sum(row => row.Notifications.Length)} exact trigger notifications, eligible battle-generated cards and 32 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
